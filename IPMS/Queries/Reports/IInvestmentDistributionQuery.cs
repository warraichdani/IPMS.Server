using IPMS.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Reports
{
    public interface IInvestmentDistributionQuery
    {
        IReadOnlyList<AllocationDto> GetDistribution(
            Guid userId,
            DateOnly from,
            DateOnly to
        );
    }
}
