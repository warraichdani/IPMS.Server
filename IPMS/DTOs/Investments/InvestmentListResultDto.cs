using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.DTOs.Investments
{
    public sealed record InvestmentListResultDto(
    IReadOnlyList<InvestmentListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
}
