
namespace IPMS.Core.Entities
{
    public sealed class PriceHistory
    {
        public Guid PriceHistoryId { get; private set; }
        public Guid InvestmentId { get; private set; }
        public DateOnly PriceDate { get; private set; }
        public decimal UnitPrice { get; private set; }

        private PriceHistory() { }

        public PriceHistory(Guid investmentId, DateOnly date, decimal unitPrice)
        {
            if (unitPrice <= 0)
                throw new ArgumentException("Unit price must be positive.");

            PriceHistoryId = Guid.NewGuid();
            InvestmentId = investmentId;
            PriceDate = date;
            UnitPrice = unitPrice;
        }
    }

}
