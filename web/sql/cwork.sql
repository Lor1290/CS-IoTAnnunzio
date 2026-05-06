CREATE DATABASE IF NOT EXISTS iot_db;
USE iot_db;

CREATE TABLE Users (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    email         VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name     VARCHAR(255) NOT NULL,
    role          VARCHAR(10)  NOT NULL DEFAULT 'viewer',
    totp_secret   VARCHAR(255),
    is_verified   TINYINT(1)   NOT NULL DEFAULT 0,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Devices (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    name            VARCHAR(255) NOT NULL,
    location        VARCHAR(255),
    esp32_serial_id VARCHAR(255) NOT NULL UNIQUE,
    status          VARCHAR(10)  NOT NULL DEFAULT 'offline',
    last_seen       DATETIME,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Sensors (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    device_id     INT          NOT NULL,
    type          VARCHAR(20)  NOT NULL,
    label         VARCHAR(255) NOT NULL,
    unit          VARCHAR(50),
    min_threshold DECIMAL(10,2),
    max_threshold DECIMAL(10,2),
    FOREIGN KEY (device_id) REFERENCES Devices(id)
);

CREATE TABLE SensorReadings (
    id        INT AUTO_INCREMENT PRIMARY KEY,
    sensor_id INT          NOT NULL,
    value     DECIMAL(10,2) NOT NULL,
    timestamp DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (sensor_id) REFERENCES Sensors(id)
);

CREATE TABLE Alerts (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    sensor_id       INT          NOT NULL,
    triggered_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_at     DATETIME,
    severity        VARCHAR(10)  NOT NULL,
    message         TEXT         NOT NULL,
    acknowledged_by INT,
    FOREIGN KEY (sensor_id)       REFERENCES Sensors(id),
    FOREIGN KEY (acknowledged_by) REFERENCES Users(id)
);

CREATE TABLE AuditLog (
    id         INT AUTO_INCREMENT PRIMARY KEY,
    user_id    INT,
    action     TEXT         NOT NULL,
    ip_address VARCHAR(45),
    created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

CREATE TABLE Sessions (
    id         INT AUTO_INCREMENT PRIMARY KEY,
    user_id    INT          NOT NULL,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at DATETIME     NOT NULL,
    created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(id)
);