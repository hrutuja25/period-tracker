using PeriodTracker.Api.Models;

namespace PeriodTracker.Api.Repositories;

public interface IPredictionRepository
{
    Task<PredictionResponse?> GetLatestAsync(Guid userId);

    Task SaveAsync(Guid userId, PredictionResponse prediction);

    Task RemoveAsync(Guid userId);
}
