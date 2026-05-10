#!/usr/bin/env python3
import sys
import os
import re
import time
import logging

import mysql.connector              # type: ignore
from mysql.connector import Error   # type: ignore

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger(__name__)

DB_CONFIG = {
    "host":               os.environ["MYSQLHOST"],
    "port":               int(os.environ.get("MYSQLPORT", 3306)),
    "database":           os.environ["MYSQLDATABASE"],
    "user":               os.environ["MYSQLUSER"],
    "password":           os.environ["MYSQLPASSWORD"],
    "autocommit":         True,
    "connection_timeout": 10,
}

ESP32_SERIAL_ID = os.environ.get("ESP32_SERIAL_ID", "wokwi-esp32-dev-01")

MAX_READINGS = 50

RE_TEMP1 = re.compile(r"\[TEMP1\]\s+Temperatura:\s*([\d.]+)\s*C\s*\|\s*Umidita:\s*([\d.]+)\s*%")
RE_TEMP2 = re.compile(r"\[TEMP2\]\s+Temperatura:\s*([\d.]+)\s*C\s*\|\s*Pressione:\s*([\d.]+)\s*Pa")
RE_TEMP3       = re.compile(r"\[TEMP3\]\s+Temperatura:\s*([\d.]+)\s*C")

RE_LUCE        = re.compile(r"\[LUCE\]\s+Raw:\s*\d+\s*\|\s*([\d.]+)\s*lux")
RE_GAS         = re.compile(r"\[GAS\]\s+Raw:\s*\d+\s*\|\s*Livello:\s*([\d.]+)")
RE_VENTO       = re.compile(r"\[VENTO\]\s+Raw:\s*\d+\s*\|\s*Livello:\s*([\d.]+)")

RE_GAS_ALERT   = re.compile(r"\[!\]\s*GAS RILEVATO")
RE_FLOOD_ALERT = re.compile(r"\[!\]\s*(ALLUVIONE|FLOOD|ACQUA)", re.IGNORECASE)

RE_ACQUA_FLOOD = re.compile(r"\[ACQUA\]\s+ALLAGAMENTO")
RE_ACQUA_SAFE  = re.compile(r"\[ACQUA\]\s+LIVELLO SICURO")

SENSOR_MAP = {
    "TEMP1_umidita": ("humidity", "TEMP1 - Umidità", "%"),

    "TEMP2_pressione": ("pressure", "TEMP2 - Pressione", "Pa"),

    "TEMP1_temperatura": ("temperature", "TEMP1 - Temperatura", "°C"),
    "TEMP2_temperatura": ("temperature", "TEMP2 - Temperatura", "°C"),
    "TEMP3_temperatura": ("temperature", "TEMP3 - Temperatura", "°C"),

    "GAS": ("gas", "Sensore Gas", "ratio"),
    "LUCE": ("light", "Sensore Luce", "lux"),
    "VENTO": ("wind", "Sensore Vento", "ratio"),
    "ACQUA": ("water", "Sensore Acqua", "boolean"),
}

def connect(retries=5, delay=3):
    for attempt in range(1, retries + 1):
        try:
            conn = mysql.connector.connect(**DB_CONFIG)
            log.info("Connected to MySQL (%s:%s)", DB_CONFIG["host"], DB_CONFIG["port"])
            return conn
        except Error as e:
            log.warning("DB connect attempt %d/%d failed: %s", attempt, retries, e)
            time.sleep(delay)
    raise RuntimeError("Could not connect to MySQL after multiple retries.")

def get_or_create_device(cur):
    cur.execute(
        "SELECT id FROM DEVICES WHERE esp32_serial_id = %s",
        (ESP32_SERIAL_ID,)
    )
    row = cur.fetchone()
    if row:
        return row[0]
    cur.execute(
        """INSERT INTO DEVICES (name, location, esp32_serial_id, status, last_seen)
           VALUES (%s, %s, %s, 'online', NOW())""",
        ("Wokwi ESP32", "Simulazione CI", ESP32_SERIAL_ID)
    )
    log.info("Created DEVICE id=%d", cur.lastrowid)
    return cur.lastrowid

def get_or_create_sensor(cur, device_id, key):
    sensor_type, label, unit = SENSOR_MAP[key]
    cur.execute(
        "SELECT id FROM SENSORS WHERE device_id = %s AND label = %s",
        (device_id, label)
    )
    row = cur.fetchone()
    if row:
        return row[0]
    cur.execute(
        """INSERT INTO SENSORS (device_id, type, label, unit)
           VALUES (%s, %s, %s, %s)""",
        (device_id, sensor_type, label, unit)
    )
    log.info("Created SENSOR '%s' id=%d", label, cur.lastrowid)
    return cur.lastrowid

def insert_reading(cur, sensor_id, value):
    cur.execute(
        "SELECT COUNT(*) FROM SENSORSREADING WHERE sensor_id = %s",
        (sensor_id,)
    )
    count = cur.fetchone()[0]

    if count < MAX_READINGS:
        cur.execute(
            """INSERT INTO SENSORSREADING (sensor_id, value, timestamp)
               VALUES (%s, %s, NOW())""",
            (sensor_id, value)
        )
    else:
        cur.execute(
            """UPDATE SENSORSREADING
               SET value = %s, timestamp = NOW()
               WHERE sensor_id = %s
               AND id = (
                   SELECT id FROM (
                       SELECT id FROM SENSORSREADING
                       WHERE sensor_id = %s
                       ORDER BY timestamp ASC
                       LIMIT 1
                   ) AS oldest
               )""",
            (value, sensor_id, sensor_id)
        )

def insert_alert(cur, sensor_id, message, severity="high"):
    cur.execute(
        """SELECT id FROM ALERTS
           WHERE sensor_id = %s AND resolved_at IS NULL
           ORDER BY triggered_at DESC LIMIT 1""",
        (sensor_id,)
    )
    if cur.fetchone():
        return  
    cur.execute(
        """INSERT INTO ALERTS (sensor_id, severity, message)
           VALUES (%s, %s, %s)""",
        (sensor_id, severity, message)
    )
    log.warning("ALERT inserted: %s", message)

def update_device_lastseen(cur, device_id):
    cur.execute(
        "UPDATE DEVICES SET status='online', last_seen=NOW() WHERE id=%s",
        (device_id,)
    )


def main():
    conn = connect()
    cur  = conn.cursor()

    device_id    = get_or_create_device(cur)
    sensor_cache = {}  # key → sensor_id

    def sid(key):
        if key not in sensor_cache:
            sensor_cache[key] = get_or_create_sensor(cur, device_id, key)
        return sensor_cache[key]

    lines_processed  = 0
    last_seen_update = time.time()

    log.info("Parser started, reading stdin…")

    for raw_line in sys.stdin:
        line = raw_line.strip()
        if not line:
            continue

        # Aggiorna last_seen ogni 30 secondi
        if time.time() - last_seen_update > 30:
            update_device_lastseen(cur, device_id)
            last_seen_update = time.time()

        m = RE_TEMP1.search(line)
        if m:
            insert_reading(cur, sid("TEMP1_temperatura"), float(m.group(1)))
            insert_reading(cur, sid("TEMP1_umidita"),     float(m.group(2)))
            lines_processed += 1
            continue
        
        m = RE_TEMP2.search(line)
        if m:
            insert_reading(cur, sid("TEMP2_temperatura"), float(m.group(1)))
            insert_reading(cur, sid("TEMP2_pressione"),   float(m.group(2)))
            lines_processed += 1
            continue

        m = RE_TEMP3.search(line)
        if m:
            insert_reading(cur, sid("TEMP3_temperatura"), float(m.group(1)))
            lines_processed += 1
            continue

        m = RE_LUCE.search(line)
        if m:
            insert_reading(cur, sid("LUCE"), float(m.group(1)))
            lines_processed += 1
            continue
        
        m = RE_GAS.search(line)
        if m:
            insert_reading(cur, sid("GAS"), float(m.group(1)))
            lines_processed += 1
            continue

        m = RE_VENTO.search(line)
        if m:
            insert_reading(cur, sid("VENTO"), float(m.group(1)))
            lines_processed += 1
            continue

        if RE_GAS_ALERT.search(line):
            insert_alert(cur, sid("GAS"), "Gas pericoloso rilevato dal sensore", "high")
            continue

        if RE_FLOOD_ALERT.search(line):
            insert_alert(cur, sid("VENTO"), "Allerta alluvione rilevata", "critical")
            continue
        
        if RE_ACQUA_FLOOD.search(line):
            insert_reading(cur, sid("ACQUA"), 1.0)
            insert_alert(cur, sid("ACQUA"), "Allagamento rilevato dal sensore acqua", "critical")
            continue

        if RE_ACQUA_SAFE.search(line):
            insert_reading(cur, sid("ACQUA"), 0.0)
            continue

    update_device_lastseen(cur, device_id)
    cur.close()
    conn.close()
    log.info("Done. Lines processed: %d", lines_processed)


if __name__ == "__main__":
    main()