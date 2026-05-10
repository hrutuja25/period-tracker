using PeriodTracker.Api.Models;

namespace PeriodTracker.Api.Repositories;

public sealed class InMemoryPredictionRepository : IPredictionRepository
{
    private readonly object syncRoot = new();
    private readonly Dictionary<Guid, PredictionResponse> predictions = [];

    public Task<PredictionResponse?> GetLatestAsync(Guid userId)
    {
        lock (syncRoot)
        {
            return Task.FromResult(predictions.GetValueOrDefault(userId));
        }
    }

    public Task SaveAsync(Guid userId, PredictionResponse prediction)
    {
        lock (syncRoot)
        {
            predictions[userId] = prediction;
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(Guid userId)
    {
        lock (syncRoot)
        {
            predictions.Remove(userId);
            return Task.CompletedTask;
        }
    }
}
