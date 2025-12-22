namespace IPMS.Core.Configs
{
    public class AppConfigs
    {
        public List<string> InvestmentTypes { get; set; } = new();
        public List<string> InvestmentStatuses { get; set; } = new();
        public List<string> TransactionTypes { get; set; } = new();
        public List<string> OtpTypes { get; set; } = new();
    }

    public enum InvestmentType
    {
        Stocks,
        Bonds,
        RealEstate,
        Crypto
    }

    public enum InvestmentStatus
    {
        Active,
        Sold,
        OnHold
    }

    public enum TransactionType
    {
        BuyMore,
        PartialSell,
        Update
    }

    public enum OtpType
    {
        EmailConfirmation,
        PhoneConfirmation
    }
}
