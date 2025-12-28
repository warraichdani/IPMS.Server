namespace IPMS.Models.DTOs.Dashboard
{
    public sealed record DashboardSummaryDto(
    decimal TotalCurrentValue,
    decimal TotalGainLoss,
    decimal TotalGainLossPercent,
    int ActiveInvestmentsCount,
    string? BestPerformerName,
    decimal? BestPerformerGainPercent,
    string? WorstPerformerName,
    decimal? WorstPerformerGainPercent
)
    {
        public static DashboardSummaryDto Empty() =>
            new(0, 0, 0, 0, null, null, null, null);
    }

}
