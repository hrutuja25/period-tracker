# Period Tracker

Small full-stack implementation and architecture notes for a Flo-like period tracker.

## Stack

- Backend: .NET 8 Minimal API
- Frontend: Vue 3 + TypeScript + Vite
- Database target: MySQL
- Cache target: Redis for prediction responses
- Local default: MySQL repository and disabled cache

## Run Locally

Before starting the backend, update `ConnectionStrings:MySql` in `PeriodTracker.Api/appsettings.json` with your MySQL username and password.

Backend:

```powershell
cd period-tracker
dotnet run --project PeriodTracker.Api\PeriodTracker.Api.csproj --urls http://127.0.0.1:5090
```

Frontend:

```powershell
cd period-tracker\PeriodTracker.Web
npm install
npm run dev
```

Open `http://127.0.0.1:5173`.

## MySQL Setup

In MySQL Workbench, open and run these scripts in order:

1. `database/schema.sql`
2. `database/seed.sql`

The schema script creates the `period_tracker` database if it does not exist. The seed script adds demo user `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` and sample period entries.

Use MySQL storage in `PeriodTracker.Api/appsettings.Development.json`:

```json
{
  "Storage": {
    "Provider": "MySql"
  },
  "Cache": {
    "Enabled": false
  }
}
```

Update `ConnectionStrings:MySql` in `appsettings.json` for your machine:

```json
{
  "ConnectionStrings": {
    "MySql": "Server=localhost;Port=3306;Database=period_tracker;User ID=root;Password=your_password;Allow User Variables=true"
  }
}
```

## Redis Setup

Redis is intentionally disabled locally. To enable it later:

```json
{
  "Cache": {
    "Enabled": true
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

Prediction reads use this order: Redis cache, latest non-deleted MySQL `predictions` row, then fresh calculation. Fresh calculations are saved to MySQL first and then cached. Creating a new period entry invalidates both stored predictions and cache entries for that user.

Prediction cache keys use `period-tracker:prediction:{userId}`.

## Architecture

```text
Vue + TypeScript UI
        |
        | HTTPS/JSON
        v
.NET Minimal API
        |
        +--> PredictionService
        |       |
        |       +--> optional Redis cache
        |
        +--> IPeriodRepository
                |
                +--> MySQL repository in production/local setup
                +--> In-memory repository for local demo
```

### Key Backend Modules

- API endpoints in `Program.cs` expose period logging and prediction retrieval.
- `PredictionService` owns cycle and ovulation calculation.
- `IPeriodRepository` keeps storage swappable.
- `IPredictionCache` keeps Redis optional.

## Database Design

```text
flow_intensity_enum
- id
- name

pain_enum
- id
- name

mood_enum
- id
- name

users
- id
- name
- email
- password_hash
- dob
- created_at
- is_deleted

period_entries
- id
- user_id
- start_date
- end_date
- notes
- created_at
- is_deleted

period_daily_logs
- id
- period_entry_id
- log_date
- flow_intensity_id
- pain_level_id
- mood_id
- notes
- created_at
- is_deleted

symptoms
- id
- user_id
- period_daily_log_id
- symptom_type
- cramp_fatigue
- severity
- notes

predictions
- id
- user_id
- prediction_type
- average_cycle_length
- average_period_duration
- predicted_period_date
- ovulation_date
- follicular_phase_length
- fertile_window_start
- fertile_window_end
- cycle_variation
- is_irregular_cycle
- days_since_last_period
- confidence_score
- created_at
- is_deleted
```

Important constraints:

- `period_entries.user_id` references `users.id`.
- `period_daily_logs.period_entry_id` references `period_entries.id`.
- `symptoms` links both the user and a daily period log.
- `(user_id, start_date)` is unique to prevent duplicate period entries.
- `(period_entry_id, log_date)` is unique to prevent duplicate daily logs.
- date checks reject impossible period ranges.

## APIs

### `GET /api/users/{userId}/periods`

Returns recent period logs in descending start-date order.

Demo user:

```text
aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
```

### `POST /api/users/{userId}/periods`

Request:

```json
{
  "startDate": "2026-05-06",
  "endDate": "2026-05-10",
  "flow": "medium",
  "notes": "Normal cycle"
}
```

Creates a log, validates dates, and invalidates stored prediction data for that user.

### `GET /api/users/{userId}/predictions`

Returns:

```json
{
  "predictionType": "weighted_history",
  "averageCycleLength": 28,
  "averagePeriodDuration": 5,
  "predictedNextPeriod": "2026-05-06",
  "ovulationDate": "2026-04-22",
  "follicularPhaseLength": 14,
  "fertileWindow": {
    "startDate": "2026-04-17",
    "endDate": "2026-04-23"
  },
  "cycleVariation": 0.82,
  "isIrregularCycle": false,
  "daysSinceLastPeriod": 32,
  "confidenceScore": 0.9,
  "source": "computed",
  "disclaimer": "Predictions are estimates from logged history and are not medical or contraceptive advice."
}
```

## Prediction Logic

1. Fetch the latest 12 period entries and sort by start date.
2. If no periods exist, use medical defaults: 28-day cycle and 5-day period duration.
3. Compute cycle lengths as `currentPeriodStart - previousPeriodStart`.
4. Remove outlier cycles longer than 60 days.
5. Use weighted averages for valid cycles: most recent `0.5`, previous `0.3`, older `0.2`.
6. Predict next period as `lastPeriodStart + averageCycleLength`.
7. Predict ovulation as `predictedNextPeriod - 14 days`.
8. Calculate follicular phase as `ovulationDate - lastPeriodStart`.
9. Calculate fertile window as 5 days before ovulation through 1 day after.
10. Detect long gaps when the last period is more than 90 days ago.
11. Score confidence from cycle variation, limited data, and irregular-cycle status.

## Product Features To Improve UX

- Symptom and mood tracking with tags.
- Reminder notifications before predicted period and fertile window.
- Calendar view with editable predictions.
- Irregular-cycle detection and gentle suggestions to consult a clinician.
- Privacy controls, data export, and account deletion.
- Partner/device sync only with explicit consent.

## Scaling And Implementation Challenges

- Health data is sensitive, so encryption, audit trails, least-privilege access, and clear deletion workflows matter.
- Predictions are probabilistic. The UI should communicate uncertainty and avoid medical overclaiming.
- Irregular cycles, pregnancy, medication, stress, PCOS, and postpartum changes can make simple averages inaccurate.
- MySQL indexes should be shaped around `(user_id, start_date)` because most queries are per-user timelines.
- Redis can reduce repeated prediction reads, but cache invalidation must happen on every cycle-affecting write.
- For many users, split read-heavy prediction workloads from write paths with background recomputation or event-driven cache warming.
- Internationalization matters for dates, units, privacy laws, and culturally sensitive content.

## Verified

- `dotnet build PeriodTracker.Api\PeriodTracker.Api.csproj`
- `npm run build`
- Smoke-tested `GET /api/users/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/periods`
- Smoke-tested `GET /api/users/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/predictions`
