using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Models.DTOs.Reports
{
    public sealed record PerformanceSummaryDto(
    DateOnly Date,
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
