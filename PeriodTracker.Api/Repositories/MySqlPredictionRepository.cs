using MySqlConnector;
using PeriodTracker.Api.Models;

namespace PeriodTracker.Api.Repositories;

public sealed class MySqlPredictionRepository(string connectionString) : IPredictionRepository
{
    public async Task<PredictionResponse?> GetLatestAsync(Guid userId)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("get_latest_prediction", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.Parameters.AddWithValue("p_user_id", userId.ToString());
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new PredictionResponse(
            ReadString(reader, 0),
            ReadInt(reader, 1),
            ReadInt(reader, 2),
            ReadDate(reader, 3),
            ReadDate(reader, 4),
            ReadInt(reader, 5),
            new DateRange(ReadDate(reader, 6), ReadDate(reader, 7)),
            ReadDouble(reader, 8),
            ReadBoolean(reader, 9),
            ReadNullableInt(reader, 10),
            ReadDouble(reader, 11),
            "database",
            "Predictions are estimates from logged history and are not medical or contraceptive advice.");
    }

    private sealed record PredictionEntity(
        string PredictionType,
        int AverageCycleLength,
        int AveragePeriodDuration,
        DateOnly PredictedPeriodDate,
        DateOnly OvulationDate,
        int FollicularPhaseLength,
        DateOnly FertileWindowStart,
        DateOnly FertileWindowEnd,
        double CycleVariation,
        bool IsIrregularCycle,
        int? DaysSinceLastPeriod,
        double ConfidenceScore);

    public async Task SaveAsync(Guid userId, PredictionResponse prediction)
    {
        var record = new PredictionEntity(
            prediction.PredictionType,
            prediction.AverageCycleLength,
            prediction.AveragePeriodDuration,
            prediction.PredictedNextPeriod,
            prediction.OvulationDate,
            prediction.FollicularPhaseLength,
            prediction.FertileWindow.StartDate,
            prediction.FertileWindow.EndDate,
            prediction.CycleVariation,
            prediction.IsIrregularCycle,
            prediction.DaysSinceLastPeriod,
            prediction.ConfidenceScore);

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("save_prediction", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.Parameters.AddWithValue("p_user_id", userId.ToString());
        command.Parameters.AddWithValue("p_prediction_type", record.PredictionType);
        command.Parameters.AddWithValue("p_average_cycle_length", record.AverageCycleLength);
        command.Parameters.AddWithValue("p_average_period_duration", record.AveragePeriodDuration);
        command.Parameters.AddWithValue("p_predicted_period_date", record.PredictedPeriodDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("p_ovulation_date", record.OvulationDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("p_follicular_phase_length", record.FollicularPhaseLength);
        command.Parameters.AddWithValue("p_fertile_window_start", record.FertileWindowStart.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("p_fertile_window_end", record.FertileWindowEnd.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("p_cycle_variation", record.CycleVariation);
        command.Parameters.AddWithValue("p_is_irregular_cycle", record.IsIrregularCycle);
        command.Parameters.AddWithValue("p_days_since_last_period", record.DaysSinceLastPeriod.HasValue
            ? record.DaysSinceLastPeriod.Value
            : DBNull.Value);
        command.Parameters.AddWithValue("p_confidence_score", record.ConfidenceScore);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveAsync(Guid userId)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("remove_predictions", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.Parameters.AddWithValue("p_user_id", userId.ToString());
        await command.ExecuteNonQueryAsync();
    }

    private static DateOnly ReadDate(MySqlDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);

        return value switch
        {
            DateOnly date => date,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            string text => DateOnly.Parse(text),
            _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
        };
    }

    private static string ReadString(MySqlDataReader reader, int ordinal) =>
        Convert.ToString(reader.GetValue(ordinal)) ?? "";

    private static int ReadInt(MySqlDataReader reader, int ordinal) =>
        Convert.ToInt32(reader.GetValue(ordinal));

    private static int? ReadNullableInt(MySqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : ReadInt(reader, ordinal);

    private static double ReadDouble(MySqlDataReader reader, int ordinal) =>
        Convert.ToDouble(reader.GetValue(ordinal));

    private static bool ReadBoolean(MySqlDataReader reader, int ordinal) =>
        Convert.ToBoolean(reader.GetValue(ordinal));
}
