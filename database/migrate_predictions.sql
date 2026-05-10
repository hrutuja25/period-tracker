USE period_tracker;

DROP PROCEDURE IF EXISTS add_prediction_column_if_missing;

DELIMITER //

CREATE PROCEDURE add_prediction_column_if_missing(
    IN column_name_to_add VARCHAR(64),
    IN column_definition TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'predictions'
          AND column_name = column_name_to_add
    ) THEN
        SET @sql = CONCAT('ALTER TABLE predictions ADD COLUMN ', column_definition);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END//

DELIMITER ;

CALL add_prediction_column_if_missing('prediction_type', 'prediction_type VARCHAR(40) NOT NULL DEFAULT ''weighted_history''');
CALL add_prediction_column_if_missing('average_cycle_length', 'average_cycle_length INT NOT NULL DEFAULT 28');
CALL add_prediction_column_if_missing('average_period_duration', 'average_period_duration INT NOT NULL DEFAULT 5');
CALL add_prediction_column_if_missing('follicular_phase_length', 'follicular_phase_length INT NOT NULL DEFAULT 14');
CALL add_prediction_column_if_missing('cycle_variation', 'cycle_variation FLOAT NOT NULL DEFAULT 0');
CALL add_prediction_column_if_missing('is_irregular_cycle', 'is_irregular_cycle BOOLEAN NOT NULL DEFAULT FALSE');
CALL add_prediction_column_if_missing('days_since_last_period', 'days_since_last_period INT NULL');

DROP PROCEDURE add_prediction_column_if_missing;
