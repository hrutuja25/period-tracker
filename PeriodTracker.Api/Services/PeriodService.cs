using PeriodTracker.Api.Models;
using PeriodTracker.Api.Repositories;

namespace PeriodTracker.Api.Services;

public sealed class PeriodService(
    IPeriodRepository periodRepository,
    IPredictionCacheRepository predictionCacheRepository,
    IPredictionRepository predictionRepository)
{
    public async Task<IReadOnlyList<PeriodResponse>> ListPeriodsAsync(Guid userId)
    {
        var periods = await periodRepository.ListPeriodsAsync(userId);
        return periods.Select(PeriodResponse.FromDomain).ToList();
    }

    public async Task<PeriodResponse> CreatePeriodAsync(Guid userId, CreatePeriodRequest request)
    {
        ValidatePeriodDates(request);

        var created = await periodRepository.CreatePeriodAsync(userId, request);
        await predictionCacheRepository.RemoveAsync(userId);
        await predictionRepository.RemoveAsync(userId);

        return PeriodResponse.FromDomain(created);
    }

    private static void ValidatePeriodDates(CreatePeriodRequest request)
    {
        if (request.EndDate < request.StartDate)
        {
            throw new ArgumentException("endDate must be on or after startDate");
        }

        if ((request.EndDate.DayNumber - request.StartDate.DayNumber) > 14)
        {
            throw new ArgumentException("period length looks too long; please check the dates");
        }
    }
}
