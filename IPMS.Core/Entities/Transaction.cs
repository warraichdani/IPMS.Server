using IPMS.Core.Configs;

namespace IPMS.Core.Entities
{
    public sealed class Transaction
    {
        public Guid TransactionId { get; private set; }
        public Guid InvestmentId { get; private set; }
        public TransactionType Type { get; private set; }
        public decimal Amount { get; private set; }     // money
        public decimal Units { get; private set; }      // ownership
        public decimal UnitPrice { get; private set; }
        public DateOnly TransactionDate { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public bool IsDeleted { get; private set; }

        private Transaction() { }

        public Transaction(
    Guid transactionId,
    Guid investmentId,
    TransactionType type,
    decimal units,
    decimal unitPrice,
    DateOnly date,
    Guid createdBy,
    bool isDeleted)
        {
            TransactionId = transactionId;
            InvestmentId = investmentId;
            Type = type;
            Units = units;
            UnitPrice = unitPrice;
            TransactionDate = date;
            CreatedByUserId = createdBy;
            IsDeleted = isDeleted;
        }

        internal void SoftDelete()
        {
            if (IsDeleted)
                return;

            IsDeleted = true;
        }

        internal static Transaction Buy(
            Guid investmentId,
            decimal amount,
            decimal unitPrice,
            DateOnly date,
            Guid userId)
        {
            return new Transaction
            {
                InvestmentId = investmentId,
                Type = TransactionType.Buy,
                Amount = amount,
                Units = amount / unitPrice,
                UnitPrice = unitPrice,
                TransactionDate = date,
                CreatedByUserId = userId
            };
        }

        internal static Transaction Sell(
            Guid investmentId,
            decimal units,
            decimal unitPrice,
            DateOnly date,
            Guid userId)
        {
            return new Transaction
            {
                TransactionId = Guid.NewGuid(),
                InvestmentId = investmentId,
                Type = TransactionType.Sell,
                Amount = units * unitPrice,
                Units = units,
                UnitPrice = unitPrice,
                TransactionDate = date,
                CreatedByUserId = userId
            };
        }
    }

}
