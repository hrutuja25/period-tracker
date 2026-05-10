using PeriodTracker.Api.Models;

namespace PeriodTracker.Api.Repositories;

public sealed class DisabledPredictionCacheRepository : IPredictionCacheRepository
{
    public Task<PredictionResponse?> GetAsync(Guid userId) => Task.FromResult<PredictionResponse?>(null);

    public Task SetAsync(Guid userId, PredictionResponse prediction) => Task.CompletedTask;

    public Task RemoveAsync(Guid userId) => Task.CompletedTask;
}
