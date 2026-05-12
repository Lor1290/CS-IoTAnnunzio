USE railway;

-- ----------------------------------------
--              USERS
--    admin@iot.local - Admin1234!
--    mario@iot.local - Mario1234!
--    giulia@iot.local - Giulia1234!
-- ----------------------------------------

INSERT INTO USERS (email, password_hash, full_name, role, is_verified) VALUES
(
    'admin@iot.local',
    '$2b$11$zO1OVlrDuLc67EtcFapOxO960XVs2fIUR/..wLnacQhPdb3baECnG',
    'Admin IoT',
    'admin',
    1
),
(
    'mario@iot.local',
    '$2b$11$b.LKhRK8UtLEReLNUOmpI.T1HQMUgMEeEnBdckBISiSUC7j26ND/S',
    'Mario Rossi',
    'viewer',
    1
),
(
    'giulia@iot.local',
    '$2b$11$c.FQdGnkHbs/vAYMGCc56.RupVzKLNvD.GYrGALDAl.WyjShcGIUm',
    'Giulia Bianchi',
    'viewer',
    1
);


-- --------------------------
-- DEVICES  (one per user) --
-- --------------------------
INSERT INTO DEVICES (name, location, esp32_serial_id, status) VALUES
('ESP32-Admin', 'Server Room', 'ESP32-001', 'online'),
('ESP32-mario@iot.local',  'Home', 'ESP32-002', 'online'),
('ESP32-giulia@iot.local', 'Home', 'ESP32-003', 'online');


-- ─────────────────────────────────────────
--              STORED PROCEDURE
--  Inserts the full standard sensor set for a given device_id.
--  Reuse this whenever a new device is added.
-- ─────────────────────────────────────────
DELIMITER $$

CREATE PROCEDURE AddStandardSensors(IN p_device_id INT)
BEGIN
    INSERT INTO SENSORS (device_id, type, label, unit, min_threshold, max_threshold) VALUES
    (p_device_id, 'temperature', 'DHT22 Temperatura',  '°C',  -10.00,    50.00),
    (p_device_id, 'humidity',    'DHT22 Umidità',       '%',     0.00,   100.00),
    (p_device_id, 'temperature', 'BMP180 Temperatura', '°C',  -10.00,    50.00),
    (p_device_id, 'pressure',    'BMP180 Pressione',   'Pa', 90000.00, 110000.00),
    (p_device_id, 'temperature', 'NTC Temperatura',    '°C',  -10.00,    50.00),
    (p_device_id, 'light',       'Luminosità',         'lux',   0.00, 10000.00),
    (p_device_id, 'gas',         'Gas',                '',      0.00,     0.60),
    (p_device_id, 'wind',        'Vento',              '',      0.00,     1.00),
    (p_device_id, 'water',       'Acqua',              '',      0.00,     1.00);
END$$

DELIMITER ;

CALL AddStandardSensors(1);  -- admin's ESP32
CALL AddStandardSensors(2);  -- mario's  ESP32
CALL AddStandardSensors(3);  -- giulia's ESP32


-- ─────────────────────────────────────────
--  HOW TO ADD A NEW USER + DEVICE IN FUTURE
--  Run these 3 statements in order:
--
--  INSERT INTO USERS (email, password_hash, full_name, role, is_verified)
--  VALUES ('new@iot.local', '<bcrypt_hash>', 'New User', 'viewer', 1);
--
--  INSERT INTO DEVICES (user_id, name, location, esp32_serial_id, status)
--  VALUES (LAST_INSERT_ID(), 'ESP32-New', 'Room X', 'WOKWI-ESP32-004', 'offline');
--
--  CALL AddStandardSensors(LAST_INSERT_ID());
-- ─────────────────────────────────────────