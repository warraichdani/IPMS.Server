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
        public Boolean IsDeleted { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public decimal InitialAmount { get; private set; }

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
    decimal unitPrice,
    decimal costBasis,
    Guid lastTransactionId,
    string broker,
    string notes)
        {
            InvestmentId = investmentId;
            UserId = userId;
            InvestmentName = name;
            InvestmentType = type;
            Status = status;
            PurchaseDate = purchaseDate;
            TotalUnits = totalUnits;
            CostBasis = costBasis;
            CurrentUnitPrice = unitPrice; 
            LastTransactionId = lastTransactionId;
            Broker = broker;
            Notes = notes;
        }
        // Factory (important in DDD)
        public static Investment Create(
            Guid userId,
            string name,
            InvestmentType type,
            decimal initialAmount,
            decimal initialUnitPrice,
            DateOnly purchaseDate,
            InvestmentStatus investmentStatus,
            string? broker,
            string? notes)
        {
            if (initialAmount <= 0)
                throw new InvalidOperationException("Initial amount must be positive.");

            if (initialUnitPrice <= 0)
                throw new InvalidOperationException("Initial unit price must be positive.");

            var units = initialAmount / initialUnitPrice;

            var investment = new Investment
            {
                InvestmentId = Guid.NewGuid(),
                UserId = userId,
                InvestmentName = name,
                InvestmentType = type,
                Status = investmentStatus,
                InitialAmount = initialAmount,
                PurchaseDate = purchaseDate,
                TotalUnits = units,
                CostBasis = initialAmount,
                CurrentUnitPrice = initialUnitPrice,
                Broker = broker,
                Notes = notes
            };

            investment.AddTransaction(Transaction.Buy(
                investment.InvestmentId,
                units,
                initialUnitPrice,
                purchaseDate,
                userId));

            return investment;
        }


        // Behavior: Buy more
        public Transaction Buy(decimal amount, decimal unitPrice, DateOnly date, Guid userId)
        {
            var units = amount / unitPrice;

            TotalUnits += units;
            CostBasis += amount;
            //CurrentUnitPrice = unitPrice;

            var newTransaction = Transaction.Buy(
                InvestmentId, amount, unitPrice, date, userId);

            _transactions.Add(newTransaction);

            return newTransaction;
        }

        // Behavior: Sell
        public Transaction Sell(decimal amount, decimal unitPrice, DateOnly date, Guid userId)
        {
            var unitsToSell = amount / unitPrice;
            if (unitsToSell > TotalUnits)
                throw new InvalidOperationException("Cannot sell more units than owned.");

            var avgCost = CostBasis / TotalUnits;
            var costRemoved = unitsToSell * avgCost;

            TotalUnits -= unitsToSell;
            CostBasis -= costRemoved;
            //CurrentUnitPrice = unitPrice;

            var newTransaction = Transaction.Sell(
                InvestmentId, unitsToSell, unitPrice, date, userId);

            _transactions.Add(newTransaction);

            if (TotalUnits == 0)
                Status = InvestmentStatus.Sold;

            return newTransaction;
        }

        // Valuation update (NOT a transaction)
        public void UpdateCurrentPrice(decimal amount)
        {
            if (amount < 0)
                throw new InvalidOperationException("New pricing must be positive.");

            CurrentUnitPrice = amount / TotalUnits;
        }

        // Derived (not stored)
        public decimal CurrentValue => TotalUnits * CurrentUnitPrice;

        public void AddTransaction(Transaction transaction)
        {
            _transactions.Add(transaction);
            UpdateLastTransaction(transaction.TransactionId);
        }

        public void UpdateLastTransaction(Guid transactionId)
        {
            LastTransactionId = transactionId;
        }

        public void UpdateDetails(
            string name,
            InvestmentType type,
            DateOnly purchaseDate,
            string? broker,
            string? notes)
        {
            if (Status == InvestmentStatus.Sold)
                throw new InvalidOperationException("Sold investment cannot be updated.");

            InvestmentName = name;
            InvestmentType = type;
            PurchaseDate = purchaseDate;
            Broker = broker;
            Notes = notes;
        }

        public void SoftDelete(Guid userId)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Investment already deleted.");

            IsDeleted = true;
            Status = InvestmentStatus.Sold; // or Archived if you add later
            UpdatedAt = DateTime.UtcNow;
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