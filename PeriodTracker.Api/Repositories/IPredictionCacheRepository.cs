using PeriodTracker.Api.Models;

namespace PeriodTracker.Api.Repositories;

public interface IPredictionCacheRepository
{
    Task<PredictionResponse?> GetAsync(Guid userId);

    Task SetAsync(Guid userId, PredictionResponse prediction);

    Task RemoveAsync(Guid userId);
}
