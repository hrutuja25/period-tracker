namespace PeriodTracker.Api.Models;

public sealed record CreatePeriodRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    string Flow = "medium",
    string Notes = "");

public sealed record PeriodResponse(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    int Length,
    string Flow,
    string Notes)
{
    public static PeriodResponse FromDomain(PeriodLog period) =>
        new(
            period.Id,
            period.StartDate,
            period.EndDate,
            period.EndDate.DayNumber - period.StartDate.DayNumber + 1,
            period.Flow,
            period.Notes);
}

public sealed record UserCycleProfile(Guid UserId, int AverageCycleLength, int AveragePeriodLength);

public sealed record PeriodLog(
    Guid Id,
    Guid UserId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Flow,
    string Notes);

public sealed record ApiError(string Error);
