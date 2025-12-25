using IPMS.Models.DTOs.Investments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Queries.Investments
{
    public interface IInvestmentEditQuery
    {
        EditInvestmentDto? Get(Guid investmentId, Guid userId);
    }
}
