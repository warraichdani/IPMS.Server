using IPMS.Helpers.Utilities;
using IPMS.Models.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services.Reports
{
    public sealed class TransactionHistoryReportExportService
    : ITransactionHistoryReportExportService
    {
        public FileExport ExportCsv(IEnumerable<TransactionHistoryRowDto> rows)
        {
            var csv = CsvWriter.Write(
                rows,
                new[]
                {
                "Transaction Date",
                "Investment Name",
                "Investment Status",
                "Transaction Type",
                "Units",
                "Unit Price",
                "Amount",
                "Total Value (At Date)",
                "Gain/Loss (At Date)"
                },
                r => new[]
                {
                r.TransactionDate.ToString("yyyy-MM-dd"),
                r.InvestmentName,
                r.InvestmentStatus,
                r.TransactionType,
                r.Units.ToString("0.####"),
                r.UnitPrice.ToString("0.####"),
                r.Amount.ToString("0.00"),
                r.TotalInvestmentValueAtDate.ToString("0.00"),
                r.GainLossAtDate.ToString("0.00")
                }
            );

            return new FileExport(
                csv,
                "text/csv",
                $"transaction_history_{DateTime.UtcNow:yyyyMMdd}.csv"
            );
        }

        public FileExport ExportJson(IEnumerable<TransactionHistoryRowDto> rows)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                rows,
                new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

            return new FileExport(
                Encoding.UTF8.GetBytes(json),
                "application/json",
                $"transaction_history_{DateTime.UtcNow:yyyyMMdd}.json"
            );
        }

        /// <summary>
        /// PDF is a simulation as per requirements.
        /// Later this can be replaced by QuestPDF / iText / DinkToPdf.
        /// </summary>
        public FileExport ExportPdf(IEnumerable<TransactionHistoryRowDto> rows)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Transaction History Report");
            sb.AppendLine($"Generated At: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine(new string('-', 80));

            foreach (var r in rows)
            {
                sb.AppendLine($"Date           : {r.TransactionDate:yyyy-MM-dd}");
                sb.AppendLine($"Investment     : {r.InvestmentName}");
                sb.AppendLine($"Status         : {r.InvestmentStatus}");
                sb.AppendLine($"Transaction    : {r.TransactionType}");
                sb.AppendLine($"Units          : {r.Units:0.####}");
                sb.AppendLine($"Unit Price     : {r.UnitPrice:0.####}");
                sb.AppendLine($"Amount         : {r.Amount:0.00}");
                sb.AppendLine($"Total Value    : {r.TotalInvestmentValueAtDate:0.00}");
                sb.AppendLine($"Gain/Loss      : {r.GainLossAtDate:0.00}");
                sb.AppendLine(new string('-', 80));
            }

            return new FileExport(
                Encoding.UTF8.GetBytes(sb.ToString()),
                "application/pdf",
                $"transaction_history_{DateTime.UtcNow:yyyyMMdd}.pdf"
            );
        }
    }

}
