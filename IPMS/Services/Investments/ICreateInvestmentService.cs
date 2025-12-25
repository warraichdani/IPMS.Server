using IPMS.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services.Investments
{
    public interface ICreateInvestmentService
    {
        Guid Execute(CreateInvestmentCommand cmd, Guid userId);
    }
}
