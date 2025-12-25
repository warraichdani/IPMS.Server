using IPMS.Core.Configs;

namespace IPMS.Validators
{
    public class InvestmentValidator
    {
        private readonly AppConfigs _configs;

        public InvestmentValidator(AppConfigs configs)
        {
            _configs = configs;
        }

        public bool IsValidType(string type) =>
            _configs.InvestmentTypes.Contains(type);

        public bool IsValidStatus(string status) =>
            _configs.InvestmentStatuses.Contains(status);

        public bool IsValidTransaction(string transaction) =>
            _configs.TransactionTypes.Contains(transaction);

        public bool IsValidOtpType(string otpType) =>
            _configs.OtpTypes.Contains(otpType);
    }
}
