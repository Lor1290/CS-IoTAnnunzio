USE CS_IOT;

-- ----------------------------------------
--              USERS
--    admin@iot.local - Admin1234!
--    mario@iot.local - Mario1234!
--    giulia@iot.local - Giulia1234!
-- ----------------------------------------

INSERT INTO USERS (username, email, password_hash, full_name, role, is_verified) VALUES
(
    'admin',
    'admin@iot.local',
    '$2b$11$zO1OVlrDuLc67EtcFapOxO960XVs2fIUR/..wLnacQhPdb3baECnG',
    'Admin IoT',
    'admin',
    1
);
SET @admin_user_id = LAST_INSERT_ID();

INSERT INTO USERS (username, email, password_hash, full_name, role, is_verified) VALUES
(
    'mario',
    'mario@iot.local',
    '$2b$11$b.LKhRK8UtLEReLNUOmpI.T1HQMUgMEeEnBdckBISiSUC7j26ND/S',
    'Mario Rossi',
    'viewer',
    1
);
SET @mario_user_id = LAST_INSERT_ID();

INSERT INTO USERS (username, email, password_hash, full_name, role, is_verified) VALUES
(
    'giulia',
    'giulia@iot.local',
    '$2b$11$c.FQdGnkHbs/vAYMGCc56.RupVzKLNvD.GYrGALDAl.WyjShcGIUm',
    'Giulia Bianchi',
    'viewer',
    1
);
SET @giulia_user_id = LAST_INSERT_ID();


-- --------------------------
-- DEVICES  (one per user) --
-- --------------------------
INSERT INTO DEVICES (user_id, name, location, esp32_serial_id, status) VALUES
(@admin_user_id, 'ESP32-Admin', 'Server Room', 'ESP32-001', 'online'),
(@mario_user_id,  'ESP32-Mario',  'Home', 'ESP32-002', 'online'),
(@giulia_user_id, 'ESP32-Giulia', 'Home', 'ESP32-003', 'online');


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

SELECT id INTO @admin_device_id FROM DEVICES WHERE user_id = @admin_user_id;
SELECT id INTO @mario_device_id FROM DEVICES WHERE user_id = @mario_user_id;
SELECT id INTO @giulia_device_id FROM DEVICES WHERE user_id = @giulia_user_id;

CALL AddStandardSensors(@admin_device_id);  -- admin's ESP32
CALL AddStandardSensors(@mario_device_id);  -- mario's  ESP32
CALL AddStandardSensors(@giulia_device_id);  -- giulia's ESP32


-- ─────────────────────────────────────────
--  HOW TO ADD A NEW USER + DEVICE IN FUTURE
--  Run these 3 statements in order:
--
--  INSERT INTO USERS (username, email, password_hash, full_name, role, is_verified)
--  VALUES ('newuser', 'new@iot.local', '<bcrypt_hash>', 'New User', 'viewer', 1);
--  SET @new_user_id = LAST_INSERT_ID();
--
--  INSERT INTO DEVICES (user_id, name, location, esp32_serial_id, status)
--  VALUES (@new_user_id, 'ESP32-New', 'Room X', 'WOKWI-ESP32-004', 'offline');
--  SET @new_device_id = LAST_INSERT_ID();
--
--  CALL AddStandardSensors(@new_device_id);
-- ─────────────────────────────────────────