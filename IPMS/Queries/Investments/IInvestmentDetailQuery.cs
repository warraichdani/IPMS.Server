using IPMS.Models.DTOs.Investments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Investments
{
    public interface IInvestmentDetailQuery
    {
        InvestmentDetailDto? Get(Guid investmentId, Guid userId);
    }
}
