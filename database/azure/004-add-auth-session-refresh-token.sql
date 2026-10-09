SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Chỉ dành cho database demo đã có schema nền.
IF DB_NAME() <> N'BranchFlowDB-Demo'
BEGIN
    ;THROW 50001, N'Đang chọn sai database.', 1;
END;

IF OBJECT_ID(N'dbo.AppUser', N'U') IS NULL
BEGIN
    ;THROW 50002, N'Không tìm thấy bảng AppUser.', 1;
END;

-- Dừng để kiểm tra, không ghi đè bảng auth đã tồn tại.
IF OBJECT_ID(N'dbo.AuthSession', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.RefreshToken', N'U') IS NOT NULL
BEGIN
    ;THROW 50003, N'Bảng auth đã tồn tại. Cần kiểm tra schema trước.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Một tài khoản có thể có nhiều phiên đăng nhập.
    CREATE TABLE dbo.AuthSession
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        UserId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        RevokedAt DATETIME2 NULL,

        CONSTRAINT PK_AuthSession PRIMARY KEY (Id),

        CONSTRAINT FK_AuthSession_AppUser
            FOREIGN KEY (UserId)
            REFERENCES dbo.AppUser(Id)
    );

    -- Hỗ trợ tìm các phiên của tài khoản khi logout-all.
    CREATE INDEX IX_AuthSession_UserId
        ON dbo.AuthSession(UserId);

    -- Lưu hash token và dấu vết rotation, không lưu token gốc.
    CREATE TABLE dbo.RefreshToken
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        SessionId UNIQUEIDENTIFIER NOT NULL,
        TokenHash VARCHAR(64) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        UsedAt DATETIME2 NULL,
        RevokedAt DATETIME2 NULL,
        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_RefreshToken PRIMARY KEY (Id),

        CONSTRAINT FK_RefreshToken_AuthSession
            FOREIGN KEY (SessionId)
            REFERENCES dbo.AuthSession(Id)
    );

    -- Hỗ trợ tìm các token thuộc cùng phiên.
    CREATE INDEX IX_RefreshToken_SessionId
        ON dbo.RefreshToken(SessionId);

    -- Mỗi hash token phải duy nhất.
    CREATE UNIQUE INDEX UQ_RefreshToken_TokenHash
        ON dbo.RefreshToken(TokenHash);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    -- Không để schema được tạo dở nếu một bước thất bại.
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;