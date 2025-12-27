using IPMS.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Performance
{
    public interface IPortfolioPerformanceQuery
    {
        IReadOnlyList<PerformancePointDto> GetLast12Months(Guid userId);
    }
}
