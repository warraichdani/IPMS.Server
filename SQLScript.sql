CREATE TABLE dbo.Users (
    UserId          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    
    Email           NVARCHAR(256) NOT NULL UNIQUE,
    FirstName       NVARCHAR(100) NOT NULL,
    LastName        NVARCHAR(100) NULL,
    
    PasswordHash    VARBINARY(512) NOT NULL,
    PasswordSalt    VARBINARY(256) NOT NULL,
    
    IsActive        BIT NOT NULL DEFAULT(1),
    IsDeleted       BIT NOT NULL DEFAULT(0), -- soft delete
    
    CreatedAt       DATETIME2(0) NOT NULL DEFAULT(SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(0) NULL
);

-- Roles
CREATE TABLE dbo.Roles (
    RoleId   INT IDENTITY(1,1) PRIMARY KEY,
    Name     NVARCHAR(50) NOT NULL UNIQUE -- 'Admin', 'User'
);

-- UserRoles
CREATE TABLE dbo.UserRoles (
    UserId    UNIQUEIDENTIFIER NOT NULL,
    RoleId    INT NOT NULL,
    AssignedAt DATETIME2(0) NOT NULL DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);


CREATE TABLE dbo.Portfolios (
    PortfolioId     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    OwnerUserId     UNIQUEIDENTIFIER NOT NULL,
    Description     NVARCHAR(1000) NULL,
    IsActive        BIT NOT NULL DEFAULT(1),
    IsDeleted       BIT NOT NULL DEFAULT(0),
    CreatedAt       DATETIME2(0) NOT NULL DEFAULT(SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(0) NULL,
    CONSTRAINT FK_Portfolio_Owner FOREIGN KEY (OwnerUserId) REFERENCES dbo.Users(UserId)
);

CREATE TABLE dbo.Investments (
    InvestmentId     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UserId           UNIQUEIDENTIFIER NOT NULL, -- new column
    InvestmentName   NVARCHAR(200) NOT NULL,
    InvestmentType   NVARCHAR(50) NOT NULL,   -- e.g. 'Stocks','Bonds','Crypto'
    InitialAmount    DECIMAL(18,2) NOT NULL,
    PurchaseDate     DATE NOT NULL,
    Broker           NVARCHAR(200) NULL,
    Notes            NVARCHAR(1000) NULL,
    Status           NVARCHAR(50) NOT NULL,   -- e.g. 'Active','Sold','OnHold'
    TotalUnits       DECIMAL(18,4) NOT NULL,  -- total units purchased
    CostBasis        DECIMAL(18,2) NOT NULL,  -- total cost basis
    UnitPrice        DECIMAL(18,4) NOT NULL,  -- current or purchase unit price
    IsDeleted        BIT NOT NULL DEFAULT(0),
    CreatedAt        DATETIME2(0) NOT NULL DEFAULT(SYSUTCDATETIME()),
    UpdatedAt        DATETIME2(0) NULL,
    CONSTRAINT FK_Investment_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId), -- foreign key
    CONSTRAINT CK_Investment_InitialAmount_Positive CHECK (InitialAmount > 0),
    CONSTRAINT CK_Investment_PurchaseDate_NotFuture CHECK (PurchaseDate <= CAST(SYSUTCDATETIME() AS DATE)),
    CONSTRAINT CK_Investment_TotalUnits_Positive CHECK (TotalUnits > 0),
    CONSTRAINT CK_Investment_CostBasis_Positive CHECK (CostBasis >= 0),
    CONSTRAINT CK_Investment_UnitPrice_Positive CHECK (UnitPrice > 0)
);
--Drop table dbo.Transactions

-- Transactions table (unchanged except still referencing InvestmentId)
CREATE TABLE dbo.Transactions (
    TransactionId    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    InvestmentId     UNIQUEIDENTIFIER NOT NULL,
    TransactionType  NVARCHAR(50) NOT NULL,   -- e.g. 'BuyMore','PartialSell'
    Units DECIMAL(18,4) NOT NULL,
	UnitPrice DECIMAL(18,4) NOT NULL,
	TransactionDate  DATE NOT NULL,
    Notes            NVARCHAR(1000) NULL,
    CreatedAt        DATETIME2(0) NOT NULL DEFAULT(SYSUTCDATETIME()),
    CreatedByUserId  UNIQUEIDENTIFIER NOT NULL,
    IsDeleted        BIT NOT NULL DEFAULT(0),
    CONSTRAINT FK_Transaction_Investment FOREIGN KEY (InvestmentId) REFERENCES dbo.Investments(InvestmentId),
    CONSTRAINT FK_Transaction_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Transaction_Date_NotFuture CHECK (TransactionDate <= CAST(SYSUTCDATETIME() AS DATE))
);

ALTER TABLE Transactions DROP COLUMN Amount;

CREATE TABLE dbo.PriceHistory (
    PriceHistoryId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    InvestmentId UNIQUEIDENTIFIER NOT NULL,
    PriceDate DATE NOT NULL,
    UnitPrice DECIMAL(18,4) NOT NULL,
    CONSTRAINT FK_PriceHistory_Investment FOREIGN KEY (InvestmentId) REFERENCES dbo.Investments(InvestmentId),
    CONSTRAINT CK_PriceHistory_UnitPrice_Positive CHECK (UnitPrice > 0)
);


-- ActivityLog
CREATE TABLE dbo.ActivityLog (
    ActivityId        BIGINT IDENTITY(1,1) PRIMARY KEY,
    ActorUserId       UNIQUEIDENTIFIER NULL,
    Action            NVARCHAR(100) NOT NULL,
    EntityType        NVARCHAR(100) NULL,
    EntityId          NVARCHAR(100) NULL,
    Summary           NVARCHAR(500) NULL,
    DetailsJson       NVARCHAR(MAX) NULL,
    OccurredAt        DATETIME2(0) NOT NULL DEFAULT(SYSUTCDATETIME()),
    IPAddress         NVARCHAR(45) NULL,
    CONSTRAINT FK_Activity_ActorUser FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(UserId)
);


CREATE TABLE dbo.RefreshTokens (
    RefreshTokenId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Token NVARCHAR(200) NOT NULL,
    ExpiresAt DATETIME2(0) NOT NULL,
    Revoked BIT NOT NULL DEFAULT(0),
    CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_RefreshToken_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);

ALTER TABLE dbo.Users
ADD EmailConfirmed BIT NOT NULL DEFAULT(0);

CREATE TABLE dbo.UserOtps (
    OtpId Int Identity(1,1) NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    OtpType NVARCHAR(50) NOT NULL,       -- 'EmailConfirmation', 'PhoneConfirmation'
    OtpCode NVARCHAR(10) NOT NULL,       -- e.g. 6-digit code
    ExpiryDateTime DATETIME2(0) NOT NULL,
    IsUsed BIT NOT NULL DEFAULT(0),
    CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UserOtp_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);




