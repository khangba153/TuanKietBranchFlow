-- Bổ sung workflow chuyển chi nhánh. Chạy bằng tài khoản quản trị database.
-- Không xóa/reset dữ liệu và không cấp DELETE cho runtime.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.EmployeeTransferRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeTransferRequest
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeTransferRequest PRIMARY KEY,
        EmployeeId int NOT NULL,
        SourceAssignmentId int NOT NULL,
        SourceBranchId int NOT NULL,
        TargetBranchId int NOT NULL,
        RequestedByUserId int NOT NULL,
        ProcessedByUserId int NULL,
        EmployeeCode nvarchar(50) NOT NULL,
        EmployeeName nvarchar(200) NOT NULL,
        SourceBranchName nvarchar(200) NOT NULL,
        TargetBranchName nvarchar(200) NOT NULL,
        RequestedByName nvarchar(200) NOT NULL,
        ProcessedByName nvarchar(200) NULL,
        Status varchar(20) NOT NULL,
        Note nvarchar(500) NULL,
        ResolutionReason nvarchar(500) NULL,
        RequestedAt datetime2(7) NOT NULL CONSTRAINT DF_EmployeeTransferRequest_RequestedAt DEFAULT SYSUTCDATETIME(),
        ProcessedAt datetime2(7) NULL,
        EffectiveDate date NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_EmployeeTransferRequest_Employee FOREIGN KEY (EmployeeId) REFERENCES dbo.EmployeeProfile(Id),
        CONSTRAINT FK_EmployeeTransferRequest_Assignment FOREIGN KEY (SourceAssignmentId) REFERENCES dbo.UserBranch(Id),
        CONSTRAINT FK_EmployeeTransferRequest_Source FOREIGN KEY (SourceBranchId) REFERENCES dbo.Branch(Id),
        CONSTRAINT FK_EmployeeTransferRequest_Target FOREIGN KEY (TargetBranchId) REFERENCES dbo.Branch(Id),
        CONSTRAINT FK_EmployeeTransferRequest_Requester FOREIGN KEY (RequestedByUserId) REFERENCES dbo.AppUser(Id),
        CONSTRAINT FK_EmployeeTransferRequest_Processor FOREIGN KEY (ProcessedByUserId) REFERENCES dbo.AppUser(Id),
        CONSTRAINT CK_EmployeeTransferRequest_Branches CHECK (SourceBranchId <> TargetBranchId),
        CONSTRAINT CK_EmployeeTransferRequest_Status CHECK (Status IN ('PENDING','TRANSFERRED','REJECTED','CANCELLED','INVALIDATED')),
        CONSTRAINT CK_EmployeeTransferRequest_Resolution CHECK
        (
            (Status = 'PENDING' AND ProcessedAt IS NULL AND EffectiveDate IS NULL)
            OR (Status <> 'PENDING' AND ProcessedAt IS NOT NULL)
        ),
        CONSTRAINT CK_EmployeeTransferRequest_EffectiveDate CHECK
        (
            (Status = 'TRANSFERRED' AND EffectiveDate IS NOT NULL)
            OR (Status <> 'TRANSFERRED' AND EffectiveDate IS NULL)
        )
    );

    CREATE UNIQUE INDEX UX_EmployeeTransferRequest_PendingEmployee
        ON dbo.EmployeeTransferRequest(EmployeeId) WHERE Status = 'PENDING';
    CREATE INDEX IX_EmployeeTransferRequest_Source ON dbo.EmployeeTransferRequest(SourceBranchId, RequestedAt DESC);
    CREATE INDEX IX_EmployeeTransferRequest_Target ON dbo.EmployeeTransferRequest(TargetBranchId, RequestedAt DESC);
END;

COMMIT TRANSACTION;
