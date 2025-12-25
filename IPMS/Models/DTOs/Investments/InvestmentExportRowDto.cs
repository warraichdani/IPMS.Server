using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Models.DTOs.Investments
{
    public sealed record InvestmentExportRowDto(
    string InvestmentName,
    string InvestmentType,
    decimal Amount,           // CostBasis
    decimal CurrentValue,
    decimal GainLossPercent,
    DateOnly PurchaseDate,
    string Status
);
}
