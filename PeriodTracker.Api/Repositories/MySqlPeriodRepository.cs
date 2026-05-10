using MySqlConnector;
using PeriodTracker.Api.Models;

namespace PeriodTracker.Api.Repositories;

public sealed class MySqlPeriodRepository(string connectionString) : IPeriodRepository
{
    public async Task<IReadOnlyList<PeriodLog>> ListPeriodsAsync(Guid userId, int limit = 12)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("get_periods", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.Parameters.AddWithValue("p_user_id", userId.ToString());
        command.Parameters.AddWithValue("p_limit", limit);
        await using var reader = await command.ExecuteReaderAsync();

        var periods = new List<PeriodLog>();
        while (await reader.ReadAsync())
        {
            periods.Add(new PeriodLog(
                ReadGuid(reader, 0),
                ReadGuid(reader, 1),
                ReadDate(reader, 2),
                ReadDate(reader, 3),
                FlowNameToApiValue(ReadString(reader, 4)),
                ReadNullableString(reader, 5) ?? ""));
        }

        return periods;
    }

    public async Task<PeriodLog> CreatePeriodAsync(Guid userId, CreatePeriodRequest request)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("create_period", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.Parameters.AddWithValue("p_user_id", userId.ToString());
        command.Parameters.AddWithValue("p_start_date", request.StartDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("p_end_date", request.EndDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("p_flow", request.Flow);
        command.Parameters.AddWithValue("p_notes", string.IsNullOrWhiteSpace(request.Notes)
            ? DBNull.Value
            : request.Notes);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("Failed to create period");
        }

        var periodId = ReadGuid(reader, 0);

        return new PeriodLog(periodId, userId, request.StartDate, request.EndDate, request.Flow, request.Notes);
    }

    public async Task<UserCycleProfile> GetProfileAsync(Guid userId)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("get_user_profile", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.Parameters.AddWithValue("p_user_id", userId.ToString());
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException("user not found");
        }

        return new UserCycleProfile(ReadGuid(reader, 0), ReadInt(reader, 1), ReadInt(reader, 2));
    }

    private static Guid ReadGuid(MySqlDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);

        return value switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text),
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => Guid.Parse(Convert.ToString(value)
                ?? throw new InvalidOperationException($"Column {ordinal} cannot be read as a Guid."))
        };
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

    private static string? ReadNullableString(MySqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : ReadString(reader, ordinal);

    private static string FlowNameToApiValue(string flow) =>
        flow.Trim().ToLowerInvariant() switch
        {
            "spotting" => "spotting",
            "light" => "light",
            "medium" => "medium",
            "heavy" => "heavy",
            _ => "notSet"
        };
}
