USE period_tracker;

DELIMITER //

-- Stored Procedure for listing periods
CREATE PROCEDURE get_periods(
    IN p_user_id CHAR(36),
    IN p_limit INT
)
BEGIN
    SELECT
        pe.id,
        pe.user_id,
        pe.start_date,
        pe.end_date,
        COALESCE(MAX(fie.name), 'notSet') AS flow,
        pe.notes
    FROM period_entries pe
    LEFT JOIN period_daily_logs pdl ON pdl.period_entry_id = pe.id AND pdl.is_deleted = FALSE
    LEFT JOIN flow_intensity_enum fie ON fie.id = pdl.flow_intensity_id
    WHERE pe.user_id = p_user_id AND pe.is_deleted = FALSE
    GROUP BY pe.id, pe.user_id, pe.start_date, pe.end_date, pe.notes
    ORDER BY pe.start_date DESC
    LIMIT p_limit;
END //

-- Stored Procedure for creating a period
CREATE PROCEDURE create_period(
    IN p_user_id CHAR(36),
    IN p_start_date DATE,
    IN p_end_date DATE,
    IN p_flow VARCHAR(20),
    IN p_notes TEXT
)
BEGIN
    DECLARE v_period_id CHAR(36);
    DECLARE v_flow_id INT;
    DECLARE v_current_date DATE;

    -- Map flow string to ID
    SET v_flow_id = CASE LOWER(TRIM(p_flow))
        WHEN 'spotting' THEN 1
        WHEN 'light' THEN 2
        WHEN 'medium' THEN 3
        WHEN 'heavy' THEN 4
        ELSE 0
    END;

    -- Generate new ID
    SET v_period_id = UUID();

    -- Insert period entry
    INSERT INTO period_entries (id, user_id, start_date, end_date, notes)
    VALUES (v_period_id, p_user_id, p_start_date, p_end_date, NULLIF(TRIM(p_notes), ''));

    -- Insert daily logs
    SET v_current_date = p_start_date;
    WHILE v_current_date <= p_end_date DO
        INSERT INTO period_daily_logs
            (id, period_entry_id, log_date, flow_intensity_id, pain_level_id, mood_id, notes)
        VALUES
            (UUID(), v_period_id, v_current_date, v_flow_id, 0, 0, '');
        SET v_current_date = DATE_ADD(v_current_date, INTERVAL 1 DAY);
    END WHILE;

    -- Return the created period ID
    SELECT v_period_id AS period_id;
END //

-- Stored Procedure for getting user profile
CREATE PROCEDURE get_user_profile(
    IN p_user_id CHAR(36)
)
BEGIN
    SELECT id, 28 AS average_cycle_length, 5 AS average_period_length
    FROM users
    WHERE id = p_user_id AND is_deleted = FALSE;
END //

-- Stored Procedure for getting latest prediction
CREATE PROCEDURE get_latest_prediction(
    IN p_user_id CHAR(36)
)
BEGIN
    SELECT
        prediction_type,
        average_cycle_length,
        average_period_duration,
        predicted_period_date,
        ovulation_date,
        follicular_phase_length,
        fertile_window_start,
        fertile_window_end,
        cycle_variation,
        is_irregular_cycle,
        days_since_last_period,
        confidence_score
    FROM predictions
    WHERE user_id = p_user_id AND is_deleted = FALSE
    ORDER BY created_at DESC
    LIMIT 1;
END //

-- Stored Procedure for saving prediction
CREATE PROCEDURE save_prediction(
    IN p_user_id CHAR(36),
    IN p_prediction_type VARCHAR(40),
    IN p_average_cycle_length INT,
    IN p_average_period_duration INT,
    IN p_predicted_period_date DATE,
    IN p_ovulation_date DATE,
    IN p_follicular_phase_length INT,
    IN p_fertile_window_start DATE,
    IN p_fertile_window_end DATE,
    IN p_cycle_variation FLOAT,
    IN p_is_irregular_cycle BOOLEAN,
    IN p_days_since_last_period INT,
    IN p_confidence_score FLOAT
)
BEGIN
    INSERT INTO predictions (
        id,
        user_id,
        prediction_type,
        average_cycle_length,
        average_period_duration,
        predicted_period_date,
        ovulation_date,
        follicular_phase_length,
        fertile_window_start,
        fertile_window_end,
        cycle_variation,
        is_irregular_cycle,
        days_since_last_period,
        confidence_score
    )
    VALUES (
        UUID(),
        p_user_id,
        p_prediction_type,
        p_average_cycle_length,
        p_average_period_duration,
        p_predicted_period_date,
        p_ovulation_date,
        p_follicular_phase_length,
        p_fertile_window_start,
        p_fertile_window_end,
        p_cycle_variation,
        p_is_irregular_cycle,
        p_days_since_last_period,
        p_confidence_score
    );
END //

-- Stored Procedure for removing predictions
CREATE PROCEDURE remove_predictions(
    IN p_user_id CHAR(36)
)
BEGIN
    UPDATE predictions
    SET is_deleted = TRUE
    WHERE user_id = p_user_id AND is_deleted = FALSE;
END //

DELIMITER ;