CREATE DATABASE IF NOT EXISTS CS_IOT;
USE CS_IOT;

CREATE TABLE USERS (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    username      VARCHAR(100) NOT NULL UNIQUE,
    email         VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name     VARCHAR(255) NOT NULL,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE DEVICES (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    user_id         INT          NOT NULL UNIQUE,
    name            VARCHAR(255) NOT NULL,
    location        VARCHAR(255),
    status          VARCHAR(10)  NOT NULL DEFAULT 'offline',
    last_seen       DATETIME,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (user_id) REFERENCES USERS(id)
);

CREATE TABLE SENSORS (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    device_id     INT          NOT NULL,
    type          VARCHAR(20)  NOT NULL,
    label         VARCHAR(255) NOT NULL,
    unit          VARCHAR(50),
    min_threshold DECIMAL(10,2),
    max_threshold DECIMAL(10,2),
    FOREIGN KEY (device_id) REFERENCES DEVICES(id)
);

CREATE TABLE SENSORSREADING (
    id        INT AUTO_INCREMENT PRIMARY KEY,
    sensor_id INT           NOT NULL,
    value     DECIMAL(10,2) NOT NULL,
    timestamp DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (sensor_id) REFERENCES SENSORS(id)
);

CREATE TABLE ALERTS (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    sensor_id       INT         NOT NULL,
    acknowledged_by INT,
    triggered_at    DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_at     DATETIME,
    severity        VARCHAR(10) NOT NULL,
    message         TEXT        NOT NULL,
    FOREIGN KEY (sensor_id)       REFERENCES SENSORS(id),
    FOREIGN KEY (acknowledged_by) REFERENCES USERS(id)
);

CREATE TABLE AUDITLOG (
    id         INT AUTO_INCREMENT PRIMARY KEY,
    user_id    INT,
    action     TEXT        NOT NULL,
    ip_address VARCHAR(45),
    created_at DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES USERS(id)
);

CREATE TABLE SESSIONS (
    id         INT AUTO_INCREMENT PRIMARY KEY,
    user_id    INT          NOT NULL,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at DATETIME     NOT NULL,
    created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES USERS(id)
);