namespace PeriodTracker.Api.Models;

public sealed record PredictionResponse(
    string PredictionType,
    int AverageCycleLength,
    int AveragePeriodDuration,
    DateOnly PredictedNextPeriod,
    DateOnly OvulationDate,
    int FollicularPhaseLength,
    DateRange FertileWindow,
    double CycleVariation,
    bool IsIrregularCycle,
    int? DaysSinceLastPeriod,
    double ConfidenceScore,
    string Source,
    string Disclaimer)
{
    public string Status => PredictionType;

    public string Basis => PredictionType switch
    {
        "first_time_user" => "medical defaults because no period history has been logged",
        "limited_history" => "hybrid of defaults and limited period history",
        "irregular_cycle" => "default cycle length because recent history is irregular",
        "long_gap" => "last logged period is older than the no-period threshold",
        _ => "weighted average of recent cycle lengths"
    };

    public double Confidence => ConfidenceScore;

    public DateOnly? LatestPeriodStart => DaysSinceLastPeriod is null
        ? null
        : PredictedNextPeriod.AddDays(-AverageCycleLength);

    public int? PredictedCycleLength => AverageCycleLength;

    public int? PredictedPeriodLength => AveragePeriodDuration;

    public DateRange NextPeriod => new(PredictedNextPeriod, PredictedNextPeriod.AddDays(AveragePeriodDuration - 1));

    public OvulationWindow Ovulation => new(OvulationDate, FertileWindow.StartDate, FertileWindow.EndDate);
}

public sealed record DateRange(DateOnly StartDate, DateOnly EndDate);

public sealed record OvulationWindow(DateOnly Date, DateOnly FertileWindowStart, DateOnly FertileWindowEnd);
