using IPMS.Helpers.Utilities;
using IPMS.Models.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services.Reports
{
    public sealed class TopPerformingInvestmentsExportService
    : ITopPerformingInvestmentsExportService
    {
        public FileExport ExportCsv(IEnumerable<TopPerformingInvestmentRowDto> rows)
        {
            var csv = CsvWriter.Write(
                rows,
                new[]
                {
                "Investment Name",
                "Investment Type",
                "Units Held",
                "Cost Basis",
                "Current Value",
                "Gain/Loss",
                "Gain/Loss %"
                },
                r => new[]
                {
                r.InvestmentName,
                r.InvestmentType,
                r.UnitsHeld.ToString("0.####"),
                r.CostBasis.ToString("0.00"),
                r.CurrentValue.ToString("0.00"),
                r.GainLoss.ToString("0.00"),
                r.GainLossPercent.ToString("0.00")
                });

            return new FileExport(
                csv,
                "text/csv",
                $"top_performing_investments_{DateTime.UtcNow:yyyyMMdd}.csv"
            );
        }

        public FileExport ExportJson(IEnumerable<TopPerformingInvestmentRowDto> rows)
        {
            var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                rows,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

            return new FileExport(
                json,
                "application/json",
                $"top_performing_investments_{DateTime.UtcNow:yyyyMMdd}.json"
            );
        }

        public FileExport ExportPdf(IEnumerable<TopPerformingInvestmentRowDto> rows)
        {
            // Simulation as discussed (no real PDF rendering yet)
            var text = new StringBuilder();
            text.AppendLine("Top Performing Investments Report");
            text.AppendLine("--------------------------------");

            foreach (var r in rows)
            {
                text.AppendLine(
                    $"{r.InvestmentName} | {r.InvestmentType} | " +
                    $"Gain %: {r.GainLossPercent:0.00}"
                );
            }

            var bytes = Encoding.UTF8.GetBytes(text.ToString());

            return new FileExport(
                bytes,
                "application/pdf",
                $"top_performing_investments_{DateTime.UtcNow:yyyyMMdd}.pdf"
            );
        }
    }
}
