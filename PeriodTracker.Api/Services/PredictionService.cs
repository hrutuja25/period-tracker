using System.Text.Json;
using PeriodTracker.Api.Models;
using PeriodTracker.Api.Repositories;

namespace PeriodTracker.Api.Services;

public sealed class PredictionService(
    IPeriodRepository periodRepository,
    IPredictionCacheRepository predictionCacheRepository,
    IPredictionRepository predictionRepository)
{
    // Move these to a config
    private const int DEFAULT_CYCLE_LENGTH = 28;
    private const int DEFAULT_PERIOD_DURATION = 5;
    private const int LUTEAL_PHASE = 14;
    private const int IRREGULAR_THRESHOLD = 60;
    private const int NO_PERIOD_THRESHOLD = 90;

    public async Task<PredictionResponse> PredictAsync(Guid userId)
    {
        // First check cache, then stored prediction, then compute new prediction if needed
        var cached = await predictionCacheRepository.GetAsync(userId);
        if (cached is not null)
        {
            return cached with { Source = "cache" };
        }

        var stored = await predictionRepository.GetLatestAsync(userId);
        if (stored is not null)
        {
            await predictionCacheRepository.SetAsync(userId, stored);
            return stored;
        }

        var prediction = await ComputePredictionAsync(userId);

        // Cache the prediction and save to database
        await predictionRepository.SaveAsync(userId, prediction);
        await predictionCacheRepository.SetAsync(userId, prediction);
        return prediction;
    }

    private async Task<PredictionResponse> ComputePredictionAsync(Guid userId)
    {
        var periods = SortPeriods(await periodRepository.ListPeriodsAsync(userId, 12));
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (periods.Count == 0)
        {
            return CreateFirstTimePrediction(today);
        }

        var latestPeriod = periods[^1];
        var cycleLengths = CalculateCycleLengths(periods);
        var validCycles = RemoveOutlierCycles(cycleLengths);
        var averageCycleLength = CalculateAverageCycleLength(validCycles, periods.Count);
        var averagePeriodDuration = CalculateAveragePeriodDuration(periods);
        var predictedNextPeriod = PredictNextPeriod(latestPeriod.StartDate, averageCycleLength);
        var ovulationDate = PredictOvulationDate(predictedNextPeriod);
        var follicularPhaseLength = CalculateFollicularPhaseLength(latestPeriod.StartDate, ovulationDate);
        var fertileWindow = CalculateFertileWindow(ovulationDate);
        var cycleVariation = CalculateCycleVariation(validCycles);
        var daysSinceLastPeriod = CalculateDaysSinceLastPeriod(today, latestPeriod.StartDate);
        var hasNoValidCycles = cycleLengths.Count > 0 && validCycles.Count == 0;
        var hasLongGap = daysSinceLastPeriod > NO_PERIOD_THRESHOLD;
        var isIrregular = hasNoValidCycles || hasLongGap;
        var predictionType = GetPredictionType(periods.Count, hasNoValidCycles, hasLongGap);
        var confidence = CalculateConfidence(periods.Count, cycleVariation, isIrregular, hasNoValidCycles);

        return CreatePrediction(
            predictionType,
            averageCycleLength,
            averagePeriodDuration,
            predictedNextPeriod,
            ovulationDate,
            follicularPhaseLength,
            fertileWindow,
            cycleVariation,
            isIrregular,
            daysSinceLastPeriod,
            confidence);
    }

    private static IReadOnlyList<PeriodLog> SortPeriods(IReadOnlyList<PeriodLog> periods) =>
        periods.OrderBy(period => period.StartDate).ToList();

    private static List<int> CalculateCycleLengths(IReadOnlyList<PeriodLog> periods) =>
        periods
            .Zip(periods.Skip(1), (previous, current) => DaysBetween(current.StartDate, previous.StartDate))
            .ToList();

    private static List<int> RemoveOutlierCycles(IReadOnlyList<int> cycleLengths) =>
        cycleLengths
            .Where(cycle => cycle <= IRREGULAR_THRESHOLD)
            .ToList();

    private static int CalculateAverageCycleLength(IReadOnlyList<int> validCycles, int periodCount)
    {
        if (validCycles.Count == 0)
        {
            return DEFAULT_CYCLE_LENGTH;
        }

        var weightedAverage = CalculateWeightedAverage(validCycles, GenerateWeights(validCycles.Count));
        var hybridAverage = periodCount < 2
            ? (weightedAverage + DEFAULT_CYCLE_LENGTH) / 2
            : weightedAverage;

        return (int)Math.Round(hybridAverage);
    }

    private static double CalculateWeightedAverage(IReadOnlyList<int> cycles, IReadOnlyList<double> weights)
    {
        var weightedSum = cycles.Zip(weights, (cycle, weight) => cycle * weight).Sum();
        var totalWeight = weights.Sum();

        return weightedSum / totalWeight;
    }

    private static List<double> GenerateWeights(int cycleCount)
    {
        if (cycleCount == 1)
        {
            return [0.5];
        }

        if (cycleCount == 2)
        {
            return [0.3, 0.5];
        }

        // Most recent cycle gets highest weight, second most recent gets medium weight, and all others get lower weight
        // Will have to modify it in the future depending on the test data
        var weights = Enumerable.Repeat(0.2, cycleCount).ToList();
        weights[^2] = 0.3;
        weights[^1] = 0.5;
        return weights;
    }

    private static int CalculateAveragePeriodDuration(IReadOnlyList<PeriodLog> periods)
    {
        if (periods.Count == 0)
        {
            return DEFAULT_PERIOD_DURATION;
        }

        var averageDuration = periods
            .TakeLast(6)
            .Average(period => DaysBetween(period.EndDate, period.StartDate) + 1);

        return Math.Clamp((int)Math.Round(averageDuration), 2, 10);
    }

    private static DateOnly PredictNextPeriod(DateOnly lastPeriodStart, int averageCycleLength) =>
        lastPeriodStart.AddDays(averageCycleLength);

    private static DateOnly PredictOvulationDate(DateOnly predictedNextPeriod) =>
        predictedNextPeriod.AddDays(-LUTEAL_PHASE);

    private static int CalculateFollicularPhaseLength(DateOnly lastPeriodStart, DateOnly ovulationDate) =>
        DaysBetween(ovulationDate, lastPeriodStart);

    // Fertile Window: 5 days before ovulation, Ovulation day, 1 day after
    private static DateRange CalculateFertileWindow(DateOnly ovulationDate) =>
        new(ovulationDate.AddDays(-5), ovulationDate.AddDays(1));

    private static double CalculateCycleVariation(IReadOnlyList<int> validCycles)
    {
        if (validCycles.Count < 2)
        {
            return 0;
        }

        var average = validCycles.Average();
        var variance = validCycles.Average(cycle => Math.Pow(cycle - average, 2));
        return Math.Round(Math.Sqrt(variance), 2);
    }

    private static int CalculateDaysSinceLastPeriod(DateOnly today, DateOnly lastPeriodStart) =>
        DaysBetween(today, lastPeriodStart);

    private static string GetPredictionType(int periodCount, bool hasNoValidCycles, bool hasLongGap)
    {
        if (hasLongGap)
        {
            return "long_gap";
        }

        if (hasNoValidCycles)
        {
            return "irregular_cycle";
        }

        return periodCount < 2
            ? "limited_history"
            : "weighted_history";
    }

    private static double CalculateConfidence(
        int periodCount,
        double cycleVariation,
        bool isIrregular,
        bool hasNoValidCycles)
    {
        if (periodCount == 0)
        {
            return 0.30;
        }

        if (hasNoValidCycles)
        {
            return 0.40;
        }

        var confidence = cycleVariation switch
        {
            <= 2 => 0.90,
            <= 5 => 0.75,
            _ => 0.50
        };

        if (periodCount < 2)
        {
            confidence = Math.Min(confidence, 0.55);
        }

        if (isIrregular)
        {
            confidence -= 0.20;
        }

        return Math.Round(Math.Max(0.10, confidence), 2);
    }

    // For users with no history, return a prediction based on medical defaults
    private static PredictionResponse CreateFirstTimePrediction(DateOnly today)
    {
        var predictedNextPeriod = today.AddDays(DEFAULT_CYCLE_LENGTH);
        var ovulationDate = PredictOvulationDate(predictedNextPeriod);
        var fertileWindow = CalculateFertileWindow(ovulationDate);

        return CreatePrediction(
            "first_time_user",
            DEFAULT_CYCLE_LENGTH,
            DEFAULT_PERIOD_DURATION,
            predictedNextPeriod,
            ovulationDate,
            DEFAULT_CYCLE_LENGTH - LUTEAL_PHASE,
            fertileWindow,
            0,
            false,
            null,
            0.30);
    }

    private static PredictionResponse CreatePrediction(
        string predictionType,
        int averageCycleLength,
        int averagePeriodDuration,
        DateOnly predictedNextPeriod,
        DateOnly ovulationDate,
        int follicularPhaseLength,
        DateRange fertileWindow,
        double cycleVariation,
        bool isIrregularCycle,
        int? daysSinceLastPeriod,
        double confidenceScore) =>
        new(
            predictionType,
            averageCycleLength,
            averagePeriodDuration,
            predictedNextPeriod,
            ovulationDate,
            follicularPhaseLength,
            fertileWindow,
            cycleVariation,
            isIrregularCycle,
            daysSinceLastPeriod,
            confidenceScore,
            "computed",
            "Predictions are estimates from logged history and are not medical or contraceptive advice.");

    private static int DaysBetween(DateOnly laterDate, DateOnly earlierDate) =>
        laterDate.DayNumber - earlierDate.DayNumber;
}
