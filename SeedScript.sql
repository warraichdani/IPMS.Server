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

    DELETE FROM Users
    WHERE Email = 'adminuser@mail.com';

    /* =========================================================
       3. Insert Admin User and Capture UserId
       ========================================================= */

    DECLARE @UserIdTable TABLE (UserId uniqueidentifier);

    insert into Users (Email,FirstName, LastName, PasswordHash, PasswordSalt, IsActive, IsDeleted, CreatedAt, EmailConfirmed)
		OUTPUT INSERTED.UserId INTO @UserIdTable
			values('adminuser@mail.com','Admin', 'User', Convert(varbinary,'0xBC859625277E057049CA1879BC4E35F2778FA7E4F92E16D189D8503728CA9B9DE7A51CB3307FC9335ADF4C02CC2731D9E3136B5B26DCD5C3CC8CD48B253F8322')
					, Convert(varbinary,'0xDD8BD179204BB913CA224EF5E184EB5ABCA4FE9DBBB2BEA8B4EDAF8DB23836EF9927F8B124BBAFE2EB12791C478D0D4A9AFA8E5D7A7BCD22C7DBB5E4BE98D645667D505F236909FCD3406B1A03C43D5FD46AB0075C24E3BC47AF0F7941C8A75CDF1E1CC229DAFD7CA698133A58404373E9A0CF7D75DFE5E204E05F034FDF2E9E')
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
    WHERE Name IN ('Admin', 'User');