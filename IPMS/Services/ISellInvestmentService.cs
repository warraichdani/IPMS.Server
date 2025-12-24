using IPMS.Commands;
using IPMS.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services
{
    public interface ISellInvestmentService
    {
        SellInvestmentResponse Execute(SellInvestmentCommand cmd);
    }
}
