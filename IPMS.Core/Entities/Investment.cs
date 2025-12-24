using IPMS.Core.Configs;
using IPMS.Core.Repositories;

namespace IPMS.Core.Entities
{
    public sealed class Investment
    {
        // Identity
        public Guid InvestmentId { get; private set; }
        public Guid UserId { get; private set; }

        // Descriptive
        public string InvestmentName { get; private set; }
        public InvestmentType InvestmentType { get; private set; }
        public InvestmentStatus Status { get; private set; }

        // Financial State (snapshot)
        public decimal TotalUnits { get; private set; }
        public decimal CostBasis { get; private set; }
        public decimal CurrentUnitPrice { get; private set; }

        // Metadata
        public DateOnly PurchaseDate { get; private set; }
        public string? Broker { get; private set; }
        public string? Notes { get; private set; }
        public Guid? LastTransactionId { get; private set; }

        // Aggregate children
        private readonly List<Transaction> _transactions = new();
        public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

        private Investment() { } // ORM / mapper

        public Investment(
    Guid investmentId,
    Guid userId,
    string name,
    InvestmentType type,
    InvestmentStatus status,
    DateOnly purchaseDate,
    decimal totalUnits,
    decimal costBasis,
    Guid lastTransactionId)
        {
            InvestmentId = investmentId;
            UserId = userId;
            InvestmentName = name;
            InvestmentType = type;
            Status = status;
            PurchaseDate = purchaseDate;
            TotalUnits = totalUnits;
            CostBasis = costBasis;
            LastTransactionId = lastTransactionId;
        }
        // Factory (important in DDD)
        public static Investment Create(
            Guid userId,
            string name,
            InvestmentType type,
            decimal initialAmount,
            decimal initialUnitPrice,
            DateOnly purchaseDate)
        {
            var units = initialAmount / initialUnitPrice;

            var investment = new Investment
            {
                InvestmentId = Guid.NewGuid(),
                UserId = userId,
                InvestmentName = name,
                InvestmentType = type,
                Status = InvestmentStatus.Active,
                PurchaseDate = purchaseDate,
                TotalUnits = units,
                CostBasis = initialAmount,
                CurrentUnitPrice = initialUnitPrice
            };

            investment.AddTransaction(Transaction.Buy(
                investment.InvestmentId,
                initialAmount,
                initialUnitPrice,
                purchaseDate,
                userId));

            return investment;
        }

        // Behavior: Buy more
        public void Buy(decimal amount, decimal unitPrice, DateOnly date, Guid userId)
        {
            var units = amount / unitPrice;

            TotalUnits += units;
            CostBasis += amount;
            CurrentUnitPrice = unitPrice;

            _transactions.Add(Transaction.Buy(
                InvestmentId, amount, unitPrice, date, userId));
        }

        // Behavior: Sell
        public void Sell(decimal unitsToSell, decimal unitPrice, DateOnly date, Guid userId)
        {
            if (unitsToSell > TotalUnits)
                throw new InvalidOperationException("Cannot sell more units than owned.");

            var avgCost = CostBasis / TotalUnits;
            var costRemoved = unitsToSell * avgCost;

            TotalUnits -= unitsToSell;
            CostBasis -= costRemoved;
            CurrentUnitPrice = unitPrice;

            _transactions.Add(Transaction.Sell(
                InvestmentId, unitsToSell, unitPrice, date, userId));

            if (TotalUnits == 0)
                Status = InvestmentStatus.Sold;
        }

        // Valuation update (NOT a transaction)
        public void UpdateMarketPrice(decimal unitPrice)
        {
            CurrentUnitPrice = unitPrice;
        }

        // Derived (not stored)
        public decimal CurrentValue => TotalUnits * CurrentUnitPrice;

        public void AddTransaction(Transaction transaction)
        {
            _transactions.Add(transaction);
            UpdateLastTransaction(transaction);
        }

        public void UpdateLastTransaction(Transaction transaction)
        {
            LastTransactionId = transaction.TransactionId;
        }

        public Transaction GetLastTransaction(ITransactionRepository transactionRepo)
        {
            if (!LastTransactionId.HasValue)
                throw new InvalidOperationException("No transactions exist for this investment.");

            return transactionRepo.GetById(LastTransactionId.Value)
                ?? throw new InvalidOperationException("Last transaction not found in repository.");
        }
    }
}