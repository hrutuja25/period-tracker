using PeriodTracker.Api.Models;

namespace PeriodTracker.Api.Repositories;

public sealed class InMemoryPeriodRepository : IPeriodRepository
{
    private static readonly Guid DemoUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly object syncRoot = new();
    private readonly List<PeriodLog> logs =
    [
        new(Guid.NewGuid(), DemoUserId, new DateOnly(2025, 10, 20), new DateOnly(2025, 10, 24), "medium", "Mild cramps on day one"),
        new(Guid.NewGuid(), DemoUserId, new DateOnly(2025, 11, 17), new DateOnly(2025, 11, 21), "medium", "Normal cycle"),
        new(Guid.NewGuid(), DemoUserId, new DateOnly(2025, 12, 16), new DateOnly(2025, 12, 20), "heavy", "Heavier first two days"),
        new(Guid.NewGuid(), DemoUserId, new DateOnly(2026, 1, 13), new DateOnly(2026, 1, 17), "medium", "Normal cycle"),
        new(Guid.NewGuid(), DemoUserId, new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 14), "light", "Lighter flow"),
        new(Guid.NewGuid(), DemoUserId, new DateOnly(2026, 3, 11), new DateOnly(2026, 3, 15), "medium", "Normal cycle"),
        new(Guid.NewGuid(), DemoUserId, new DateOnly(2026, 4, 8), new DateOnly(2026, 4, 12), "medium", "Normal cycle")
    ];

    public Task<IReadOnlyList<PeriodLog>> ListPeriodsAsync(Guid userId, int limit = 12)
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<PeriodLog>>(
                logs
                    .Where(log => log.UserId == userId)
                    .OrderByDescending(log => log.StartDate)
                    .Take(limit)
                    .ToList());
        }
    }

    public Task<PeriodLog> CreatePeriodAsync(Guid userId, CreatePeriodRequest request)
    {
        lock (syncRoot)
        {
            if (logs.Any(log => log.UserId == userId && log.StartDate == request.StartDate))
            {
                throw new InvalidOperationException("a period with this start date already exists");
            }

            var period = new PeriodLog(Guid.NewGuid(), userId, request.StartDate, request.EndDate, request.Flow, request.Notes);
            logs.Add(period);
            return Task.FromResult(period);
        }
    }

    public Task<UserCycleProfile> GetProfileAsync(Guid userId) =>
        Task.FromResult(new UserCycleProfile(userId, 28, 5));
}
