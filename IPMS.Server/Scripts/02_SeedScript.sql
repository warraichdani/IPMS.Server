	/* =========================================================
       1. Ensure Roles Exist
       ========================================================= */

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Admin')
    BEGIN
        INSERT INTO Roles (Name)
        VALUES ('Admin');
    END;

    IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'User')
    BEGIN
        INSERT INTO Roles (Name)
        VALUES ('User');
    END;

    /* =========================================================
       2. Remove Existing Admin User (Optional Reset)
       ========================================================= */

    DELETE FROM UserRoles
    WHERE UserId IN (
        SELECT UserId FROM Users WHERE Email = 'adminuser@mail.com'
    );

	Delete from UserRoles where UserId = (select UserId from Users WHERE Email = 'adminuser@mail.com')
	Delete from RefreshTokens where UserId = (select UserId from Users WHERE Email = 'adminuser@mail.com')
    DELETE FROM Users
    WHERE Email = 'adminuser@mail.com';

    /* =========================================================
       3. Insert Admin User and Capture UserId
       ========================================================= */

    DECLARE @UserIdTable TABLE (UserId uniqueidentifier);

    insert into Users (Email,FirstName, LastName, PasswordHash, PasswordSalt, IsActive, IsDeleted, CreatedAt, EmailConfirmed)
		OUTPUT INSERTED.UserId INTO @UserIdTable
			values('adminuser@mail.com','Admin', 'User', 0xBC859625277E057049CA1879BC4E35F2778FA7E4F92E16D189D8503728CA9B9DE7A51CB3307FC9335ADF4C02CC2731D9E3136B5B26DCD5C3CC8CD48B253F8322
					, 0xDD8BD179204BB913CA224EF5E184EB5ABCA4FE9DBBB2BEA8B4EDAF8DB23836EF9927F8B124BBAFE2EB12791C478D0D4A9AFA8E5D7A7BCD22C7DBB5E4BE98D645667D505F236909FCD3406B1A03C43D5FD46AB0075C24E3BC47AF0F7941C8A75CDF1E1CC229DAFD7CA698133A58404373E9A0CF7D75DFE5E204E05F034FDF2E9E
					,1, 0, '2025-12-23 12:28:19', 1)


    /* =========================================================
       4. Assign Roles to Admin User
       ========================================================= */

    INSERT INTO UserRoles (UserId, RoleId, AssignedAt)
    SELECT
        (select userId from @UserIdTable),
        RoleId,
        GETDATE()
    FROM Roles
	WHERE Name IN ('Admin');
    --WHERE Name IN ('Admin', 'User');


	  /* =========================================================
       5. Portfolio User Creation role assigning
       ========================================================= */

	DELETE FROM UserRoles
    WHERE UserId IN (
        SELECT UserId FROM Users WHERE Email = 'user@mail.com'
    );

	Delete from UserRoles where UserId = (select UserId from Users WHERE Email = 'user@mail.com')
	Delete from RefreshTokens where UserId = (select UserId from Users WHERE Email = 'user@mail.com')
    DELETE FROM Users
    WHERE Email = 'user@mail.com';

	/* =========================================================
       6. Insert NOrmal User and Capture UserId
       ========================================================= */

    DECLARE @PortfolioUserIdTable TABLE (UserId uniqueidentifier);

    insert into Users (Email,FirstName, LastName, PasswordHash, PasswordSalt, IsActive, IsDeleted, CreatedAt, EmailConfirmed)
		OUTPUT INSERTED.UserId INTO @PortfolioUserIdTable
			values('user@mail.com','Porfolio', 'User', 0xBC859625277E057049CA1879BC4E35F2778FA7E4F92E16D189D8503728CA9B9DE7A51CB3307FC9335ADF4C02CC2731D9E3136B5B26DCD5C3CC8CD48B253F8322
					, 0xDD8BD179204BB913CA224EF5E184EB5ABCA4FE9DBBB2BEA8B4EDAF8DB23836EF9927F8B124BBAFE2EB12791C478D0D4A9AFA8E5D7A7BCD22C7DBB5E4BE98D645667D505F236909FCD3406B1A03C43D5FD46AB0075C24E3BC47AF0F7941C8A75CDF1E1CC229DAFD7CA698133A58404373E9A0CF7D75DFE5E204E05F034FDF2E9E
					,1, 0, '2025-12-23 12:28:19', 1)


    /* =========================================================
       7. Assign Roles to Admin User
       ========================================================= */

    INSERT INTO UserRoles (UserId, RoleId, AssignedAt)
    SELECT
        (select userId from @PortfolioUserIdTable),
        RoleId,
        GETDATE()
    FROM Roles
	WHERE Name IN ('User');


	 /* =========================================================
       8. Seed Script------
       ========================================================= */


	SET NOCOUNT ON;

DECLARE @InvestmentCount INT = 50;
DECLARE @TransactionsPerInvestment INT = 5;

DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
DECLARE @OneYearBack DATE = DATEADD(YEAR, -1, @Today);

DECLARE @InvestmentIds TABLE (
    InvestmentId UNIQUEIDENTIFIER,
    UserId UNIQUEIDENTIFIER
);

DECLARE @i INT = 1;

WHILE @i <= 50
BEGIN
    DECLARE @UserId UNIQUEIDENTIFIER =
        (SELECT TOP 1 UserId FROM @PortfolioUserIdTable ORDER BY NEWID());

    DECLARE @InvestmentId UNIQUEIDENTIFIER = NEWID();

    DECLARE @PurchaseDate DATE =
        DATEADD(
            DAY,
            ABS(CHECKSUM(NEWID())) % DATEDIFF(DAY, @OneYearBack, @Today),
            @OneYearBack
        );

    DECLARE @UnitPrice DECIMAL(18,4) =
        CAST(10 + (ABS(CHECKSUM(NEWID())) % 9000) / 100.0 AS DECIMAL(18,4));

    DECLARE @Units DECIMAL(18,4) =
        CAST(10 + (ABS(CHECKSUM(NEWID())) % 500) AS DECIMAL(18,4));

    DECLARE @CostBasis DECIMAL(18,2) = @Units * @UnitPrice;

    INSERT INTO dbo.Investments (
        InvestmentId, UserId, InvestmentName, InvestmentType,
        InitialAmount, PurchaseDate, Broker, Notes,
        Status, TotalUnits, CostBasis, UnitPrice
    )
    VALUES (
        @InvestmentId,
        @UserId,
        CONCAT('Investment ', @i),
        CASE ABS(CHECKSUM(NEWID())) % 3
            WHEN 0 THEN 'Stocks'
            WHEN 1 THEN 'Bonds'
            ELSE 'Crypto'
        END,
        @CostBasis,
        @PurchaseDate,
        'Demo Broker',
        'Seeded investment',
        'Active',
        @Units,
        @CostBasis,
        @UnitPrice
    );

    INSERT INTO @InvestmentIds VALUES (@InvestmentId, @UserId);

    -- Initial price history
    INSERT INTO dbo.PriceHistory (InvestmentId, PriceDate, UnitPrice)
    VALUES (@InvestmentId, @PurchaseDate, @UnitPrice);

    SET @i += 1;
END


--DECLARE @UserId UNIQUEIDENTIFIER;
SELECT TOP 1 @UserId = UserId FROM @PortfolioUserIdTable;

-- Cursor over generated investments
DECLARE InvestmentCursor CURSOR LOCAL FAST_FORWARD FOR
SELECT TOP (@InvestmentCount)
    NEWID() AS InvestmentId,
    CONCAT('Investment ', ROW_NUMBER() OVER (ORDER BY (SELECT NULL))) AS InvestmentName
FROM sys.objects;

OPEN InvestmentCursor;

--DECLARE @InvestmentId UNIQUEIDENTIFIER;
--DECLARE @UserId UNIQUEIDENTIFIER;

DECLARE investment_cursor CURSOR FOR
SELECT InvestmentId, UserId FROM @InvestmentIds;

OPEN investment_cursor;
FETCH NEXT FROM investment_cursor INTO @InvestmentId, @UserId;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @Txn INT = 1;

    DECLARE @TxnDate DATE =
        (SELECT PurchaseDate FROM dbo.Investments WHERE InvestmentId = @InvestmentId);

    DECLARE @CurrentUnits DECIMAL(18,4) =
        (SELECT TotalUnits FROM dbo.Investments WHERE InvestmentId = @InvestmentId);

    WHILE @Txn <= 5
    BEGIN
        -- Move date forward but cap at today
        SET @TxnDate = DATEADD(DAY, 20, @TxnDate);
        IF @TxnDate > @Today SET @TxnDate = @Today;

        DECLARE @TxnType NVARCHAR(50) =
            CASE ABS(CHECKSUM(NEWID())) % 2
                WHEN 0 THEN 'Buy'
                ELSE 'Sell'
            END;

        DECLARE @TxnUnits DECIMAL(18,4) =
            CAST(1 + (ABS(CHECKSUM(NEWID())) % 20) AS DECIMAL(18,4));

        DECLARE @TxnPrice DECIMAL(18,4) =
            CAST(10 + (ABS(CHECKSUM(NEWID())) % 9000) / 100.0 AS DECIMAL(18,4));

        IF @TxnType = 'Sell' AND @CurrentUnits <= @TxnUnits
            SET @TxnType = 'Buy';

        DECLARE @TransactionId UNIQUEIDENTIFIER = NEWID();

        INSERT INTO dbo.Transactions (
            TransactionId, InvestmentId, TransactionType,
            Units, UnitPrice, TransactionDate, CreatedByUserId
        )
        VALUES (
            @TransactionId,
            @InvestmentId,
            @TxnType,
            @TxnUnits,
            @TxnPrice,
            @TxnDate,
            @UserId
        );

        -- Update units
        IF @TxnType = 'Buy'
            SET @CurrentUnits += @TxnUnits;
        ELSE
            SET @CurrentUnits -= @TxnUnits;

        -- Price history at transaction
        INSERT INTO dbo.PriceHistory (InvestmentId, PriceDate, UnitPrice)
        VALUES (@InvestmentId, @TxnDate, @TxnPrice);

        -- Extra price history between transactions
        DECLARE @ph INT = 1;
        WHILE @ph <= 2
        BEGIN
            DECLARE @MidDate DATE =
                DATEADD(DAY, 5 * @ph, DATEADD(DAY, -20, @TxnDate));

            IF @MidDate > @Today SET @MidDate = @Today;

            DECLARE @MidPrice DECIMAL(18,4) =
                CAST(@TxnPrice * (0.95 + (ABS(CHECKSUM(NEWID())) % 10) / 100.0)
                     AS DECIMAL(18,4));

            INSERT INTO dbo.PriceHistory (InvestmentId, PriceDate, UnitPrice)
            VALUES (@InvestmentId, @MidDate, @MidPrice);

            -- Update investment unit price
            UPDATE dbo.Investments
            SET UnitPrice = @MidPrice
            WHERE InvestmentId = @InvestmentId;

            SET @ph += 1;
        END

        -- Update investment
        UPDATE dbo.Investments
        SET
            TotalUnits = @CurrentUnits,
            UnitPrice = @TxnPrice,
            LastTransactionId = @TransactionId,
            UpdatedAt = SYSUTCDATETIME()
        WHERE InvestmentId = @InvestmentId;

        SET @Txn += 1;
    END

    FETCH NEXT FROM investment_cursor INTO @InvestmentId, @UserId;
END

CLOSE investment_cursor;
DEALLOCATE investment_cursor;


