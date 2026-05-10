CREATE DATABASE IF NOT EXISTS period_tracker;
USE period_tracker;

CREATE TABLE IF NOT EXISTS flow_intensity_enum (
    id INT NOT NULL PRIMARY KEY,
    name VARCHAR(32) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS pain_enum (
    id INT NOT NULL PRIMARY KEY,
    name VARCHAR(32) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS mood_enum (
    id INT NOT NULL PRIMARY KEY,
    name VARCHAR(32) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS users (
    id CHAR(36) NOT NULL PRIMARY KEY,
    name VARCHAR(45) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    dob DATE NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS period_entries (
    id CHAR(36) NOT NULL PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    notes TEXT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT period_entries_dates_valid CHECK (end_date >= start_date),
    CONSTRAINT period_entries_length_reasonable CHECK (DATEDIFF(end_date, start_date) BETWEEN 0 AND 14),
    CONSTRAINT one_period_entry_per_start_date UNIQUE (user_id, start_date),
    CONSTRAINT fk_period_entries_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_period_entries_user_start_date (user_id, start_date)
);

CREATE TABLE IF NOT EXISTS period_daily_logs (
    id CHAR(36) NOT NULL PRIMARY KEY,
    period_entry_id CHAR(36) NOT NULL,
    log_date DATE NOT NULL,
    flow_intensity_id INT NOT NULL DEFAULT 0,
    pain_level_id INT NOT NULL DEFAULT 0,
    mood_id INT NOT NULL DEFAULT 0,
    notes TEXT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT one_daily_log_per_period_date UNIQUE (period_entry_id, log_date),
    CONSTRAINT fk_daily_logs_period_entry FOREIGN KEY (period_entry_id) REFERENCES period_entries(id) ON DELETE CASCADE,
    CONSTRAINT fk_daily_logs_flow FOREIGN KEY (flow_intensity_id) REFERENCES flow_intensity_enum(id),
    CONSTRAINT fk_daily_logs_pain FOREIGN KEY (pain_level_id) REFERENCES pain_enum(id),
    CONSTRAINT fk_daily_logs_mood FOREIGN KEY (mood_id) REFERENCES mood_enum(id),
    INDEX idx_daily_logs_period_entry_log_date (period_entry_id, log_date),
    INDEX idx_daily_logs_log_date (log_date)
);

CREATE TABLE IF NOT EXISTS symptoms (
    id CHAR(36) NOT NULL PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    period_daily_log_id CHAR(36) NOT NULL,
    symptom_type VARCHAR(80) NOT NULL,
    severity INT NOT NULL DEFAULT 0 CHECK (severity BETWEEN 0 AND 10),
    notes TEXT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT fk_symptoms_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_symptoms_daily_log FOREIGN KEY (period_daily_log_id) REFERENCES period_daily_logs(id) ON DELETE CASCADE,
    INDEX idx_symptoms_user (user_id),
    INDEX idx_symptoms_period_daily_log (period_daily_log_id)
);

CREATE TABLE IF NOT EXISTS predictions (
    id CHAR(36) NOT NULL PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    prediction_type VARCHAR(40) NOT NULL,
    average_cycle_length INT NOT NULL,
    average_period_duration INT NOT NULL,
    predicted_period_date DATE NOT NULL,
    ovulation_date DATE NOT NULL,
    follicular_phase_length INT NOT NULL,
    fertile_window_start DATE NOT NULL,
    fertile_window_end DATE NOT NULL,
    cycle_variation FLOAT NOT NULL DEFAULT 0,
    is_irregular_cycle BOOLEAN NOT NULL DEFAULT FALSE,
    days_since_last_period INT NULL,
    confidence_score FLOAT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT fk_predictions_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_predictions_user_created_at (user_id, created_at)
);
