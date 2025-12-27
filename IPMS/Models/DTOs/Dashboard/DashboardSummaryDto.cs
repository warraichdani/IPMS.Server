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
);

}
