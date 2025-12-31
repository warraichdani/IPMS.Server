using IPMS.Helpers.Utilities;
using IPMS.Models.DTOs.Reports;
using System.Text;
using System.Text.Json;

namespace IPMS.Services.Reports
{
    public sealed class PerformanceSummaryExportService
    : IPerformanceSummaryExportService
    {
        public FileExport ExportCsv(IEnumerable<PerformanceSummaryDto> rows)
        {
            var csv = CsvWriter.Write(
                rows,
                new[]
                {
                "Date",
                "Total Current Value",
                "Total Gain/Loss",
                "Gain/Loss %",
                "Active Investments",
                "Best Performer",
                "Best Performer %",
                "Worst Performer",
                "Worst Performer %"
                },
                r => new[]
                {
                r.Date.ToString("yyyy-MM-dd"),
                r.TotalCurrentValue.ToString("0.00"),
                r.TotalGainLoss.ToString("0.00"),
                r.TotalGainLossPercent.ToString("0.00"),
                r.ActiveInvestmentsCount.ToString(),
                r.BestPerformerName ?? "",
                r.BestPerformerGainPercent?.ToString("0.00") ?? "",
                r.WorstPerformerName ?? "",
                r.WorstPerformerGainPercent?.ToString("0.00") ?? ""
                });

            return new FileExport(
                csv,
                "text/csv",
                $"performance_summary_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        public FileExport ExportJson(IEnumerable<PerformanceSummaryDto> rows)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(
                rows,
                new JsonSerializerOptions { WriteIndented = true });

            return new FileExport(
                json,
                "application/json",
                $"performance_summary_{DateTime.UtcNow:yyyyMMdd}.json");
        }

        public FileExport ExportPdfSimulation(IEnumerable<PerformanceSummaryDto> rows)
        {
            var sb = new StringBuilder();

            sb.AppendLine("PERFORMANCE SUMMARY REPORT");
            sb.AppendLine("===========================");
            sb.AppendLine();

            foreach (var r in rows)
            {
                sb.AppendLine(
                    $"{r.Date:yyyy-MM-dd} | Value: {r.TotalCurrentValue:0.00} | " +
                    $"Gain %: {r.TotalGainLossPercent:0.00} | " +
                    $"Best: {r.BestPerformerName} | Worst: {r.WorstPerformerName}");
            }

            return new FileExport(
                Encoding.UTF8.GetBytes(sb.ToString()),
                "application/pdf",
                $"performance_summary_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
    }

}
