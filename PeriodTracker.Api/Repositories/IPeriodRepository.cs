using PeriodTracker.Api.Models;

namespace PeriodTracker.Api.Repositories;

public interface IPeriodRepository
{
    Task<IReadOnlyList<PeriodLog>> ListPeriodsAsync(Guid userId, int limit = 12);

    Task<PeriodLog> CreatePeriodAsync(Guid userId, CreatePeriodRequest request);

    Task<UserCycleProfile> GetProfileAsync(Guid userId);
}
