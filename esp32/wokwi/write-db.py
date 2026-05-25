#!/usr/bin/env python3

import sys
import os
import re
import time
import logging

import mysql.connector              # type: ignore
from mysql.connector import Error   # type: ignore


# ----------------- #
# --- CONSTANTS --- #
# ----------------- #

MAX_READINGS = 50
DEVICE_NAME = os.environ["ESP32_DEVICE_NAME"]

DB_CONFIG = {
    "autocommit": True,
    "connection_timeout": 10,
    "user": os.environ["MYSQLUSER"],
    "host": os.environ["MYSQLHOST"],
    "port": int(os.environ["MYSQLPORT"]),
    "database": os.environ["MYSQLDATABASE"],
    "password": os.environ["MYSQLPASSWORD"],
}

SENSOR_MAP = {
    "LUCE": ("light", "Sensore Luce", "lux", None),
    "VENTO": ("wind", "Sensore Vento", "ratio", None),
    "ACQUA": ("water", "Sensore Acqua", "bool", "Allerta Allagamento"),
    "GAS": ("gas", "Sensore Gas", "ratio", "Allerta Concentrazione Gas"),
    "TEMP1_T": ("temperature", "TEMP1 - Temperatura", "°C", None),
    "TEMP1_H": ("humidity", "TEMP1 - Umidità", "%", None),
    "TEMP2_T": ("temperature", "TEMP2 - Temperatura", "°C", None),
    "TEMP2_P": ("pressure", "TEMP2 - Pressione", "Pa", None),
    "TEMP3_T": ("temperature", "TEMP3 - Temperatura", "°C", None),
}

logging.basicConfig(
    level=logging.INFO,
    format="{asctime} - {levelname} - {message}",
    style="{",
    datefmt="%H:%M:%S"
)

log = logging.getLogger()


# ------------- #
# --- REGEX --- #
# ------------- #

RE_DATA  = re.compile(r"^DATA:(.+)$")
RE_PAIR  = re.compile(r"([A-Z0-9_]+):([\d.]+)")
RE_ALERT = re.compile(r"^ALERT:([A-Z0-9_]+):(low|high|critical)$")


# ----------------- #
# --- FUNCTIONS --- #
# ----------------- #
def connect(retries=5, delay=3):
    for attempt in range(1, retries + 1):
        try:
            conn = mysql.connector.connect(**DB_CONFIG)
            log.info(f"Connected to MySQL ({DB_CONFIG['host']}:{DB_CONFIG['port']})")
            return conn
        except Error as e:
            log.warning(f"DB connect attempt {attempt}/{retries} failed: {e}")
            time.sleep(delay)
    raise RuntimeError("Could not connect to MySQL!")


def get_device(cur):
    cur.execute("SELECT id FROM devices WHERE name = %s", (DEVICE_NAME,))
    row = cur.fetchone()
    if row is None:
        raise RuntimeError(
            f"Device '{DEVICE_NAME}' non trovato nel database. "
            f"Crealo prima dall'admin panel."
        )
    cur.execute(
        "UPDATE devices SET status='online', last_seen=NOW() WHERE id = %s",
        (row[0],)
    )
    log.info(f"Device '{DEVICE_NAME}' trovato, id={row[0]}")
    return row[0]


def get_or_create_sensor(cur, device_id, key):
    sensor_type, label, unit, _ = SENSOR_MAP[key]
    cur.execute(
        "SELECT id FROM sensors WHERE device_id = %s AND label = %s",
        (device_id, label)
    )
    row = cur.fetchone()
    if row:
        return row[0]
    cur.execute(
        "INSERT INTO sensors (device_id, type, label, unit) VALUES (%s, %s, %s, %s)",
        (device_id, sensor_type, label, unit)
    )
    log.info(f"Created sensor '{label}' id={cur.lastrowid}")
    return cur.lastrowid


def insert_reading(cur, sensor_id, value):
    cur.execute(
        "SELECT COUNT(*) FROM sensor_readings WHERE sensor_id = %s",
        (sensor_id,)
    )
    count = cur.fetchone()[0]

    if count < MAX_READINGS:
        cur.execute(
            "INSERT INTO sensor_readings (sensor_id, value, timestamp) VALUES (%s, %s, NOW())",
            (sensor_id, value)
        )
    else:
        cur.execute(
            """
            UPDATE sensor_readings SET value=%s, timestamp=NOW()
            WHERE sensor_id=%s AND id=(
                SELECT id FROM (
                    SELECT id FROM sensor_readings
                    WHERE sensor_id=%s ORDER BY timestamp ASC LIMIT 1
                ) AS oldest
            )
            """,
            (value, sensor_id, sensor_id)
        )


def insert_alert(cur, sensor_id, message, severity):
    cur.execute(
        "SELECT id FROM alerts WHERE sensor_id=%s AND resolved_at IS NULL LIMIT 1",
        (sensor_id,)
    )
    if cur.fetchone():
        return
    cur.execute(
        "INSERT INTO alerts (sensor_id, severity, message) VALUES (%s, %s, %s)",
        (sensor_id, severity, message)
    )
    log.warning(f"ALERT: {message} [{severity}]")


def main():
    conn = connect()
    cur = conn.cursor()

    device_id = get_device(cur)
    sensor_cache = {}

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

        if time.time() - last_seen_update > 30:
            cur.execute(
                "UPDATE devices SET status='online', last_seen=NOW() WHERE id=%s",
                (device_id,)
            )
            last_seen_update = time.time()

        m = RE_DATA.match(line)
        if m:
            for key, val in RE_PAIR.findall(m.group(1)):
                if key not in SENSOR_MAP:
                    continue
                insert_reading(cur, sid(key), float(val))
                if key == "ACQUA" and float(val) == 1.0:
                    insert_alert(cur, sid(key), "Allerta Allagamento", "critical")
            lines_processed += 1
            continue

        m = RE_ALERT.match(line)
        if m:
            key, severity = m.group(1), m.group(2)
            if key in SENSOR_MAP:
                _, _, _, alert_msg = SENSOR_MAP[key]
                insert_alert(cur, sid(key), alert_msg or f"Allerta {key}", severity)
            continue

    cur.execute("UPDATE devices SET status='offline' WHERE id=%s", (device_id,))
    cur.close()
    conn.close()
    log.info(f"Done. Lines processed: {lines_processed}")


if __name__ == "__main__":
    main()