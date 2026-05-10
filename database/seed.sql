USE period_tracker;

INSERT INTO flow_intensity_enum (id, name)
VALUES
    (0, 'notSet'),
    (1, 'spotting'),
    (2, 'light'),
    (3, 'medium'),
    (4, 'heavy')
ON DUPLICATE KEY UPDATE name = VALUES(name);

INSERT INTO pain_enum (id, name)
VALUES
    (0, 'notSet'),
    (1, 'light'),
    (2, 'moderate'),
    (4, 'severe')
ON DUPLICATE KEY UPDATE name = VALUES(name);

INSERT INTO mood_enum (id, name)
VALUES
    (0, 'notSet'),
    (1, 'happy'),
    (2, 'neutral'),
    (4, 'frustrated')
ON DUPLICATE KEY UPDATE name = VALUES(name);

INSERT INTO users (id, name, email, password_hash, dob)
VALUES (
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'Demo User',
    'demo@example.com',
    '$2a$12$demo.hash.placeholder.replace.in.real.auth.flow',
    '1998-01-01'
)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    email = VALUES(email),
    password_hash = VALUES(password_hash),
    dob = VALUES(dob),
    is_deleted = FALSE;

INSERT INTO period_entries (id, user_id, start_date, end_date, notes)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2025-10-20', '2025-10-24', 'Mild cramps on day one'),
    ('22222222-2222-2222-2222-222222222222', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2025-11-17', '2025-11-21', 'Normal cycle'),
    ('33333333-3333-3333-3333-333333333333', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2025-12-16', '2025-12-20', 'Heavier first two days'),
    ('44444444-4444-4444-4444-444444444444', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-01-13', '2026-01-17', 'Normal cycle'),
    ('55555555-5555-5555-5555-555555555555', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-02-10', '2026-02-14', 'Lighter flow'),
    ('66666666-6666-6666-6666-666666666666', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-03-11', '2026-03-15', 'Normal cycle'),
    ('77777777-7777-7777-7777-777777777777', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '2026-04-08', '2026-04-12', 'Normal cycle')
ON DUPLICATE KEY UPDATE
    end_date = VALUES(end_date),
    notes = VALUES(notes),
    is_deleted = FALSE;

INSERT INTO period_daily_logs (id, period_entry_id, log_date, flow_intensity_id, pain_level_id, mood_id, notes)
VALUES
    ('11111111-1111-1111-1111-111111111120', '11111111-1111-1111-1111-111111111111', '2025-10-20', 3, 2, 2, 'Mild cramps'),
    ('22222222-2222-2222-2222-222222222217', '22222222-2222-2222-2222-222222222222', '2025-11-17', 3, 1, 2, ''),
    ('33333333-3333-3333-3333-333333333316', '33333333-3333-3333-3333-333333333333', '2025-12-16', 4, 2, 2, 'Heavier first two days'),
    ('44444444-4444-4444-4444-444444444413', '44444444-4444-4444-4444-444444444444', '2026-01-13', 3, 1, 2, ''),
    ('55555555-5555-5555-5555-555555555510', '55555555-5555-5555-5555-555555555555', '2026-02-10', 2, 1, 1, 'Lighter flow'),
    ('66666666-6666-6666-6666-666666666611', '66666666-6666-6666-6666-666666666666', '2026-03-11', 3, 1, 2, ''),
    ('77777777-7777-7777-7777-777777777708', '77777777-7777-7777-7777-777777777777', '2026-04-08', 3, 1, 2, '')
ON DUPLICATE KEY UPDATE
    flow_intensity_id = VALUES(flow_intensity_id),
    pain_level_id = VALUES(pain_level_id),
    mood_id = VALUES(mood_id),
    notes = VALUES(notes),
    is_deleted = FALSE;

-- Test users for prediction logic
INSERT INTO users (id, name, email, password_hash, dob)
VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Regular Cycle User', 'regular@example.com', '$2a$12$demo.hash.placeholder', '1995-05-05'),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'Variable Length User', 'variable@example.com', '$2a$12$demo.hash.placeholder', '1994-04-04'),
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'Irregular Cycle User', 'irregular@example.com', '$2a$12$demo.hash.placeholder', '1993-03-03'),
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'Long Gap User', 'longgap@example.com', '$2a$12$demo.hash.placeholder', '1992-02-02')
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    email = VALUES(email),
    password_hash = VALUES(password_hash),
    dob = VALUES(dob),
    is_deleted = FALSE;

-- Regular period history: 28-day cycle length
INSERT INTO period_entries (id, user_id, start_date, end_date, notes)
VALUES
    ('bbbbbbbb-1111-1111-1111-111111111111', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-01', '2026-01-05', 'Regular cycle'),
    ('bbbbbbbb-2222-2222-2222-222222222222', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-01-29', '2026-02-02', 'Regular cycle'),
    ('bbbbbbbb-3333-3333-3333-333333333333', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-02-26', '2026-03-02', 'Regular cycle'),
    ('bbbbbbbb-4444-4444-4444-444444444444', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-03-26', '2026-03-30', 'Regular cycle'),
    ('bbbbbbbb-5555-5555-5555-555555555555', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-04-23', '2026-04-27', 'Regular cycle')
ON DUPLICATE KEY UPDATE
    end_date = VALUES(end_date),
    notes = VALUES(notes),
    is_deleted = FALSE;

INSERT INTO period_daily_logs (id, period_entry_id, log_date, flow_intensity_id, pain_level_id, mood_id, notes)
VALUES
    ('bbbbbbbb-1111-1111-1111-111111111110', 'bbbbbbbb-1111-1111-1111-111111111111', '2026-01-01', 3, 1, 2, 'Started normally'),
    ('bbbbbbbb-2222-2222-2222-222222222220', 'bbbbbbbb-2222-2222-2222-222222222222', '2026-01-29', 3, 1, 2, 'Normal'),
    ('bbbbbbbb-3333-3333-3333-333333333330', 'bbbbbbbb-3333-3333-3333-333333333333', '2026-02-26', 3, 1, 2, 'Normal'),
    ('bbbbbbbb-4444-4444-4444-444444444440', 'bbbbbbbb-4444-4444-4444-444444444444', '2026-03-26', 3, 1, 2, 'Normal'),
    ('bbbbbbbb-5555-5555-5555-555555555550', 'bbbbbbbb-5555-5555-5555-555555555555', '2026-04-23', 3, 1, 2, 'Normal')
ON DUPLICATE KEY UPDATE
    flow_intensity_id = VALUES(flow_intensity_id),
    pain_level_id = VALUES(pain_level_id),
    mood_id = VALUES(mood_id),
    notes = VALUES(notes),
    is_deleted = FALSE;

-- Variable cycle lengths: varying period start spacing and duration
INSERT INTO period_entries (id, user_id, start_date, end_date, notes)
VALUES
    ('cccccccc-1111-1111-1111-111111111111', 'cccccccc-cccc-cccc-cccc-cccccccccccc', '2026-01-02', '2026-01-05', 'Shorter period'),
    ('cccccccc-2222-2222-2222-222222222222', 'cccccccc-cccc-cccc-cccc-cccccccccccc', '2026-01-31', '2026-02-03', 'Longer cycle'),
    ('cccccccc-3333-3333-3333-333333333333', 'cccccccc-cccc-cccc-cccc-cccccccccccc', '2026-02-27', '2026-03-02', 'Short recovery'),
    ('cccccccc-4444-4444-4444-444444444444', 'cccccccc-cccc-cccc-cccc-cccccccccccc', '2026-03-25', '2026-03-29', 'Slightly longer duration'),
    ('cccccccc-5555-5555-5555-555555555555', 'cccccccc-cccc-cccc-cccc-cccccccccccc', '2026-04-20', '2026-04-23', 'Shortened cycle')
ON DUPLICATE KEY UPDATE
    end_date = VALUES(end_date),
    notes = VALUES(notes),
    is_deleted = FALSE;

INSERT INTO period_daily_logs (id, period_entry_id, log_date, flow_intensity_id, pain_level_id, mood_id, notes)
VALUES
    ('cccccccc-1111-1111-1111-111111111110', 'cccccccc-1111-1111-1111-111111111111', '2026-01-02', 3, 1, 2, 'Started'),
    ('cccccccc-2222-2222-2222-222222222220', 'cccccccc-2222-2222-2222-222222222222', '2026-01-31', 3, 1, 2, 'Started'),
    ('cccccccc-3333-3333-3333-333333333330', 'cccccccc-3333-3333-3333-333333333333', '2026-02-27', 3, 1, 2, 'Started'),
    ('cccccccc-4444-4444-4444-444444444440', 'cccccccc-4444-4444-4444-444444444444', '2026-03-25', 3, 1, 2, 'Started'),
    ('cccccccc-5555-5555-5555-555555555550', 'cccccccc-5555-5555-5555-555555555555', '2026-04-20', 3, 1, 2, 'Started')
ON DUPLICATE KEY UPDATE
    flow_intensity_id = VALUES(flow_intensity_id),
    pain_level_id = VALUES(pain_level_id),
    mood_id = VALUES(mood_id),
    notes = VALUES(notes),
    is_deleted = FALSE;

-- Cycles demonstrating an outlier above IRREGULAR_THRESHOLD (60 days)
INSERT INTO period_entries (id, user_id, start_date, end_date, notes)
VALUES
    ('dddddddd-1111-1111-1111-111111111111', 'dddddddd-dddd-dddd-dddd-dddddddddddd', '2025-10-01', '2025-10-05', 'Normal start'),
    ('dddddddd-2222-2222-2222-222222222222', 'dddddddd-dddd-dddd-dddd-dddddddddddd', '2025-11-30', '2025-12-04', '61-day gap from previous'),
    ('dddddddd-3333-3333-3333-333333333333', 'dddddddd-dddd-dddd-dddd-dddddddddddd', '2026-01-27', '2026-01-31', '65-day gap from previous')
ON DUPLICATE KEY UPDATE
    end_date = VALUES(end_date),
    notes = VALUES(notes),
    is_deleted = FALSE;

INSERT INTO period_daily_logs (id, period_entry_id, log_date, flow_intensity_id, pain_level_id, mood_id, notes)
VALUES
    ('dddddddd-1111-1111-1111-111111111110', 'dddddddd-1111-1111-1111-111111111111', '2025-10-01', 3, 1, 2, 'Started'),
    ('dddddddd-2222-2222-2222-222222222220', 'dddddddd-2222-2222-2222-222222222222', '2025-11-30', 3, 1, 2, 'Outlier gap'),
    ('dddddddd-3333-3333-3333-333333333330', 'dddddddd-3333-3333-3333-333333333333', '2026-01-27', 3, 1, 2, 'Another outlier gap')
ON DUPLICATE KEY UPDATE
    flow_intensity_id = VALUES(flow_intensity_id),
    pain_level_id = VALUES(pain_level_id),
    mood_id = VALUES(mood_id),
    notes = VALUES(notes),
    is_deleted = FALSE;

-- Long gap case: last period is older than NO_PERIOD_THRESHOLD (90 days)
INSERT INTO period_entries (id, user_id, start_date, end_date, notes)
VALUES
    ('eeeeeeee-1111-1111-1111-111111111111', 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', '2025-10-01', '2025-10-05', 'Historical cycle'),
    ('eeeeeeee-2222-2222-2222-222222222222', 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', '2025-11-01', '2025-11-05', 'Second historical cycle')
ON DUPLICATE KEY UPDATE
    end_date = VALUES(end_date),
    notes = VALUES(notes),
    is_deleted = FALSE;

INSERT INTO period_daily_logs (id, period_entry_id, log_date, flow_intensity_id, pain_level_id, mood_id, notes)
VALUES
    ('eeeeeeee-1111-1111-1111-111111111110', 'eeeeeeee-1111-1111-1111-111111111111', '2025-10-01', 3, 1, 2, 'Old cycle'),
    ('eeeeeeee-2222-2222-2222-222222222220', 'eeeeeeee-2222-2222-2222-222222222222', '2025-11-01', 3, 1, 2, 'Old cycle')
ON DUPLICATE KEY UPDATE
    flow_intensity_id = VALUES(flow_intensity_id),
    pain_level_id = VALUES(pain_level_id),
    mood_id = VALUES(mood_id),
    notes = VALUES(notes),
    is_deleted = FALSE;

