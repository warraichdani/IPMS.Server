namespace IPMS.Core.Configs
{
    public abstract class Enumeration : IEquatable<Enumeration>
    {
        public string Value { get; }

        protected Enumeration(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;

        public override bool Equals(object? obj)
            => obj is Enumeration other && Value == other.Value;

        public bool Equals(Enumeration? other)
            => other is not null && Value == other.Value;

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(Enumeration? a, Enumeration? b)
            => Equals(a, b);

        public static bool operator !=(Enumeration? a, Enumeration? b)
            => !Equals(a, b);
    }

    public sealed class InvestmentType : Enumeration
    {
        public static readonly InvestmentType Stocks = new("Stocks");
        public static readonly InvestmentType Bonds = new("Bonds");
        public static readonly InvestmentType RealEstate = new("RealEstate");
        public static readonly InvestmentType Crypto = new("Crypto");
        public static readonly InvestmentType MutualFunds = new("MutualFunds");

        private InvestmentType(string value) : base(value) { }

        public static InvestmentType From(string value) =>
            value switch
            {
                "Stocks" => Stocks,
                "Bonds" => Bonds,
                "RealEstate" => RealEstate,
                "Crypto" => Crypto,
                "MutualFunds" => MutualFunds,
                _ => throw new ArgumentException($"Invalid InvestmentType: {value}")
            };
    }

    public sealed class InvestmentStatus : Enumeration
    {
        public static readonly InvestmentStatus Active = new("Active");
        public static readonly InvestmentStatus Sold = new("Sold");
        public static readonly InvestmentStatus OnHold = new("OnHold");

        private InvestmentStatus(string value) : base(value) { }

        public static InvestmentStatus From(string value) =>
            value switch
            {
                "Active" => Active,
                "Sold" => Sold,
                "OnHold" => OnHold,
                _ => throw new ArgumentException($"Invalid InvestmentStatus: {value}")
            };
    }

    public sealed class TransactionType : Enumeration
    {
        public static readonly TransactionType Buy = new("BuyMore");
        public static readonly TransactionType Sell = new("PartialSell");
        //public static readonly TransactionType Update = new("Update");

        private TransactionType(string value) : base(value) { }

        public static TransactionType From(string value) =>
            value switch
            {
                "Buy" or "BuyMore" => Buy,
                "Sell" or "PartialSell" => Sell,
                //"Update" => Update,
                _ => throw new ArgumentException($"Invalid TransactionType: {value}")
            };
    }

    public sealed class OtpType : Enumeration
    {
        public static readonly OtpType EmailConfirmation = new("EmailConfirmation");
        public static readonly OtpType PhoneConfirmation = new("PhoneConfirmation");

        private OtpType(string value) : base(value) { }

        public static OtpType From(string value) =>
            value switch
            {
                "EmailConfirmation" => EmailConfirmation,
                "PhoneConfirmation" => PhoneConfirmation,
                _ => throw new ArgumentException($"Invalid OtpType: {value}")
            };
    }
}
