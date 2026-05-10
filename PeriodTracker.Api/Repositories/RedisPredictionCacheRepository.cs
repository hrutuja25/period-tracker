using System.Text.Json;
using PeriodTracker.Api.Models;
using StackExchange.Redis;

namespace PeriodTracker.Api.Repositories;

public sealed class RedisPredictionCacheRepository(IConnectionMultiplexer redis) : IPredictionCacheRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase database = redis.GetDatabase();

    public async Task<PredictionResponse?> GetAsync(Guid userId)
    {
        var value = await database.StringGetAsync(Key(userId));
        return value.HasValue
            ? JsonSerializer.Deserialize<PredictionResponse>(value!, JsonOptions)
            : null;
    }

    public Task SetAsync(Guid userId, PredictionResponse prediction)
    {
        var value = JsonSerializer.Serialize(prediction, JsonOptions);
        return database.StringSetAsync(Key(userId), value, TimeSpan.FromHours(12));
    }

    public Task RemoveAsync(Guid userId) => database.KeyDeleteAsync(Key(userId));

    private static string Key(Guid userId) => $"period-tracker:prediction:{userId:N}";
}
