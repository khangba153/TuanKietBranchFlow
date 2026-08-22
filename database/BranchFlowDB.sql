/* =========================================================
   BRANCHFLOW DB - FINAL MERGED VERSION
   SQL Server 2019+

   Mục tiêu:
   - Giữ thiết kế dễ hiểu của bản học hiện tại.
   - Lấy các phần SQL tốt từ bản Codex khi thật sự cần.
   - Không ép soft delete cho dữ liệu giao dịch/lịch sử.
   - Không dùng CASCADE DELETE.
   - Thời gian hệ thống lưu UTC.
   ========================================================= */

USE master;
GO

IF DB_ID(N'BranchFlowDB') IS NULL
BEGIN
    CREATE DATABASE BranchFlowDB
    COLLATE Vietnamese_100_CI_AI;
END;
GO

USE BranchFlowDB;
GO

SET XACT_ABORT ON;
GO

-- Không chạy đè script khởi tạo lên database đã có schema.
IF OBJECT_ID(N'dbo.AppUser', N'U') IS NOT NULL
BEGIN
    THROW 51000,
          N'BranchFlowDB đã có bảng. Không chạy lại file khởi tạo; hãy dùng ALTER TABLE hoặc EF Core Migration.',
          1;
END;
GO


/* =========================================================
   MODULE 1 - ORGANIZATION + ACCOUNT + EMPLOYEE
   ========================================================= */

-- ROLE: dữ liệu master nên dùng soft delete.
CREATE TABLE dbo.[Role]
(
    Id INT IDENTITY(1,1) NOT NULL,
    Code NVARCHAR(30) NOT NULL,
    Name NVARCHAR(100) NOT NULL,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Role_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_Role_Deleted DEFAULT 0,

    CONSTRAINT PK_Role PRIMARY KEY (Id),

    CONSTRAINT CK_Role_Code_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Code))) > 0),

    CONSTRAINT CK_Role_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE UNIQUE INDEX UX_Role_Code
ON dbo.[Role](Code)
WHERE Deleted = 0;
GO


-- APP USER: tài khoản đăng nhập của OWNER / ADMIN / EMPLOYEE.
CREATE TABLE dbo.AppUser
(
    Id INT IDENTITY(1,1) NOT NULL,
    RoleId INT NOT NULL,

    Username NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(254) NULL,
    Phone NVARCHAR(30) NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_AppUser_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_AppUser_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_AppUser_Deleted DEFAULT 0,

    CONSTRAINT PK_AppUser PRIMARY KEY (Id),

    CONSTRAINT FK_AppUser_Role
        FOREIGN KEY (RoleId) REFERENCES dbo.[Role](Id),

    CONSTRAINT CK_AppUser_Username_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Username))) > 0),

    CONSTRAINT CK_AppUser_PasswordHash_NotBlank
        CHECK (LEN(LTRIM(RTRIM(PasswordHash))) > 0),

    CONSTRAINT CK_AppUser_FullName_NotBlank
        CHECK (LEN(LTRIM(RTRIM(FullName))) > 0)
);
GO

CREATE UNIQUE INDEX UX_AppUser_Username
ON dbo.AppUser(Username)
WHERE Deleted = 0;

CREATE UNIQUE INDEX UX_AppUser_Email
ON dbo.AppUser(Email)
WHERE Email IS NOT NULL AND Deleted = 0;

CREATE INDEX IX_AppUser_RoleId
ON dbo.AppUser(RoleId);
GO


-- BRANCH: chi nhánh của công ty.
CREATE TABLE dbo.Branch
(
    Id INT IDENTITY(1,1) NOT NULL,
    Code NVARCHAR(30) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Address NVARCHAR(500) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_Branch_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Branch_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_Branch_Deleted DEFAULT 0,

    CONSTRAINT PK_Branch PRIMARY KEY (Id),

    CONSTRAINT CK_Branch_Code_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Code))) > 0),

    CONSTRAINT CK_Branch_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0),

    CONSTRAINT CK_Branch_Address_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Address))) > 0)
);
GO

CREATE UNIQUE INDEX UX_Branch_Code
ON dbo.Branch(Code)
WHERE Deleted = 0;
GO


-- USER BRANCH:
-- Lưu lịch sử một user từng được phân công vào branch nào.
-- Không soft delete vì đây chính là dữ liệu lịch sử phân công.
CREATE TABLE dbo.UserBranch
(
    Id INT IDENTITY(1,1) NOT NULL,
    UserId INT NOT NULL,
    BranchId INT NOT NULL,

    ActiveFrom DATE NOT NULL,
    ActiveTo DATE NULL,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_UserBranch_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_UserBranch PRIMARY KEY (Id),

    CONSTRAINT FK_UserBranch_User
        FOREIGN KEY (UserId) REFERENCES dbo.AppUser(Id),

    CONSTRAINT FK_UserBranch_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT CK_UserBranch_ActiveDate
        CHECK (ActiveTo IS NULL OR ActiveTo >= ActiveFrom)
);
GO

-- Chặn trùng cùng user + branch ở trạng thái hiện tại.
-- Quy tắc EMPLOYEE chỉ có 1 branch hiện tại vẫn kiểm tra ở Service vì phụ thuộc Role.
CREATE UNIQUE INDEX UX_UserBranch_Current
ON dbo.UserBranch(UserId, BranchId)
WHERE ActiveTo IS NULL;

CREATE INDEX IX_UserBranch_Branch_Active
ON dbo.UserBranch(BranchId, ActiveTo)
INCLUDE (UserId, ActiveFrom);
GO


-- EMPLOYEE PROFILE:
-- Hồ sơ nhân sự 1-0..1 với AppUser.
-- LeaveDate dùng khi nghỉ việc; Deleted chỉ dùng khi cần ẩn/xóa mềm hồ sơ.
CREATE TABLE dbo.EmployeeProfile
(
    Id INT IDENTITY(1,1) NOT NULL,
    UserId INT NOT NULL,

    EmployeeCode NVARCHAR(30) NOT NULL,
    DateOfBirth DATE NULL,
    HireDate DATE NOT NULL,
    LeaveDate DATE NULL,

    Position NVARCHAR(100) NULL,
    Address NVARCHAR(500) NULL,
    AvatarUrl NVARCHAR(1000) NULL,

    BaseSalary DECIMAL(18,2) NOT NULL,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_EmployeeProfile_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_EmployeeProfile_Deleted DEFAULT 0,

    CONSTRAINT PK_EmployeeProfile PRIMARY KEY (Id),

    CONSTRAINT FK_EmployeeProfile_User
        FOREIGN KEY (UserId) REFERENCES dbo.AppUser(Id),

    CONSTRAINT CK_EmployeeProfile_EmployeeCode_NotBlank
        CHECK (LEN(LTRIM(RTRIM(EmployeeCode))) > 0),

    CONSTRAINT CK_EmployeeProfile_BaseSalary
        CHECK (BaseSalary >= 0),

    CONSTRAINT CK_EmployeeProfile_HireLeaveDate
        CHECK (LeaveDate IS NULL OR LeaveDate >= HireDate)
);
GO

CREATE UNIQUE INDEX UX_EmployeeProfile_User
ON dbo.EmployeeProfile(UserId)
WHERE Deleted = 0;

CREATE UNIQUE INDEX UX_EmployeeProfile_EmployeeCode
ON dbo.EmployeeProfile(EmployeeCode)
WHERE Deleted = 0;
GO


/* =========================================================
   MODULE 2 - MENU + PRODUCT
   ========================================================= */

CREATE TABLE dbo.Category
(
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(150) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_Category_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Category_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_Category_Deleted DEFAULT 0,

    CONSTRAINT PK_Category PRIMARY KEY (Id),

    CONSTRAINT CK_Category_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE UNIQUE INDEX UX_Category_Name
ON dbo.Category(Name)
WHERE Deleted = 0;
GO


CREATE TABLE dbo.Product
(
    Id INT IDENTITY(1,1) NOT NULL,
    CategoryId INT NOT NULL,

    Name NVARCHAR(150) NOT NULL,
    ImageUrl NVARCHAR(1000) NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_Product_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Product_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_Product_Deleted DEFAULT 0,

    CONSTRAINT PK_Product PRIMARY KEY (Id),

    CONSTRAINT FK_Product_Category
        FOREIGN KEY (CategoryId) REFERENCES dbo.Category(Id),

    CONSTRAINT CK_Product_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE UNIQUE INDEX UX_Product_Name
ON dbo.Product(Name)
WHERE Deleted = 0;

CREATE INDEX IX_Product_Category
ON dbo.Product(CategoryId, IsActive)
INCLUDE (Name, Deleted);
GO


CREATE TABLE dbo.Size
(
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(50) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_Size_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Size_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_Size_Deleted DEFAULT 0,

    CONSTRAINT PK_Size PRIMARY KEY (Id),

    CONSTRAINT CK_Size_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE UNIQUE INDEX UX_Size_Name
ON dbo.Size(Name)
WHERE Deleted = 0;
GO


-- Giá nằm ở ProductSize vì mỗi size có thể có giá khác nhau.
-- Quy tắc Product phải có ít nhất 2 size hoạt động kiểm tra ở Service.
CREATE TABLE dbo.ProductSize
(
    Id INT IDENTITY(1,1) NOT NULL,
    ProductId INT NOT NULL,
    SizeId INT NOT NULL,

    Price DECIMAL(18,2) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_ProductSize_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_ProductSize_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_ProductSize_Deleted DEFAULT 0,

    CONSTRAINT PK_ProductSize PRIMARY KEY (Id),

    CONSTRAINT FK_ProductSize_Product
        FOREIGN KEY (ProductId) REFERENCES dbo.Product(Id),

    CONSTRAINT FK_ProductSize_Size
        FOREIGN KEY (SizeId) REFERENCES dbo.Size(Id),

    CONSTRAINT CK_ProductSize_Price
        CHECK (Price >= 0)
);
GO

CREATE UNIQUE INDEX UX_ProductSize_Product_Size
ON dbo.ProductSize(ProductId, SizeId)
WHERE Deleted = 0;

CREATE INDEX IX_ProductSize_Product
ON dbo.ProductSize(ProductId, IsActive)
INCLUDE (SizeId, Price, Deleted);
GO


-- Giữ ToppingGroup để UI/quản lý topping rõ ràng hơn.
CREATE TABLE dbo.ToppingGroup
(
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_ToppingGroup_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_ToppingGroup_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_ToppingGroup_Deleted DEFAULT 0,

    CONSTRAINT PK_ToppingGroup PRIMARY KEY (Id),

    CONSTRAINT CK_ToppingGroup_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE UNIQUE INDEX UX_ToppingGroup_Name
ON dbo.ToppingGroup(Name)
WHERE Deleted = 0;
GO


CREATE TABLE dbo.Topping
(
    Id INT IDENTITY(1,1) NOT NULL,
    ToppingGroupId INT NOT NULL,

    Name NVARCHAR(150) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_Topping_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Topping_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_Topping_Deleted DEFAULT 0,

    CONSTRAINT PK_Topping PRIMARY KEY (Id),

    CONSTRAINT FK_Topping_ToppingGroup
        FOREIGN KEY (ToppingGroupId) REFERENCES dbo.ToppingGroup(Id),

    CONSTRAINT CK_Topping_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0),

    CONSTRAINT CK_Topping_Price
        CHECK (Price >= 0)
);
GO

CREATE UNIQUE INDEX UX_Topping_Name
ON dbo.Topping(Name)
WHERE Deleted = 0;
GO


-- Giữ NoteGroup để Service/UI có thể giới hạn 1 lựa chọn trong mỗi nhóm.
CREATE TABLE dbo.NoteGroup
(
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_NoteGroup_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_NoteGroup_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_NoteGroup_Deleted DEFAULT 0,

    CONSTRAINT PK_NoteGroup PRIMARY KEY (Id),

    CONSTRAINT CK_NoteGroup_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE UNIQUE INDEX UX_NoteGroup_Name
ON dbo.NoteGroup(Name)
WHERE Deleted = 0;
GO


CREATE TABLE dbo.NoteOption
(
    Id INT IDENTITY(1,1) NOT NULL,
    NoteGroupId INT NOT NULL,

    Name NVARCHAR(100) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_NoteOption_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_NoteOption_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_NoteOption_Deleted DEFAULT 0,

    CONSTRAINT PK_NoteOption PRIMARY KEY (Id),

    CONSTRAINT FK_NoteOption_NoteGroup
        FOREIGN KEY (NoteGroupId) REFERENCES dbo.NoteGroup(Id),

    CONSTRAINT CK_NoteOption_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE UNIQUE INDEX UX_NoteOption_Group_Name
ON dbo.NoteOption(NoteGroupId, Name)
WHERE Deleted = 0;
GO


-- Mapping bán hàng theo chi nhánh.
-- Không soft delete: bật/tắt bằng IsAvailable.
CREATE TABLE dbo.BranchProduct
(
    Id INT IDENTITY(1,1) NOT NULL,
    BranchId INT NOT NULL,
    ProductId INT NOT NULL,

    IsAvailable BIT NOT NULL
        CONSTRAINT DF_BranchProduct_IsAvailable DEFAULT 1,

    UpdatedAt DATETIME2(0) NULL,

    CONSTRAINT PK_BranchProduct PRIMARY KEY (Id),

    CONSTRAINT FK_BranchProduct_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT FK_BranchProduct_Product
        FOREIGN KEY (ProductId) REFERENCES dbo.Product(Id),

    CONSTRAINT UQ_BranchProduct_Branch_Product
        UNIQUE (BranchId, ProductId)
);
GO


CREATE TABLE dbo.BranchTopping
(
    Id INT IDENTITY(1,1) NOT NULL,
    BranchId INT NOT NULL,
    ToppingId INT NOT NULL,

    IsAvailable BIT NOT NULL
        CONSTRAINT DF_BranchTopping_IsAvailable DEFAULT 1,

    UpdatedAt DATETIME2(0) NULL,

    CONSTRAINT PK_BranchTopping PRIMARY KEY (Id),

    CONSTRAINT FK_BranchTopping_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT FK_BranchTopping_Topping
        FOREIGN KEY (ToppingId) REFERENCES dbo.Topping(Id),

    CONSTRAINT UQ_BranchTopping_Branch_Topping
        UNIQUE (BranchId, ToppingId)
);
GO


/* =========================================================
   MODULE 3 - ORDER / SALES
   ========================================================= */

-- Dùng SalesOrder thay cho Orders để tên bảng số ít và tránh từ khóa ORDER.
CREATE TABLE dbo.SalesOrder
(
    Id INT IDENTITY(1,1) NOT NULL,

    Code NVARCHAR(50) NOT NULL,
    BranchId INT NOT NULL,
    CreatedByUserId INT NOT NULL,

    BusinessDate DATE NOT NULL,
    DailySequence INT NOT NULL,

    TotalAmount DECIMAL(18,2) NOT NULL,

    Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_SalesOrder_Status DEFAULT N'COMPLETED',

    -- Dữ liệu báo sai đơn.
    ReportReason NVARCHAR(500) NULL,
    ReportedByUserId INT NULL,
    ReportedAt DATETIME2(0) NULL,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_SalesOrder_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    CONSTRAINT PK_SalesOrder PRIMARY KEY (Id),

    CONSTRAINT FK_SalesOrder_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT FK_SalesOrder_CreatedByUser
        FOREIGN KEY (CreatedByUserId) REFERENCES dbo.AppUser(Id),

    CONSTRAINT FK_SalesOrder_ReportedByUser
        FOREIGN KEY (ReportedByUserId) REFERENCES dbo.AppUser(Id),

    CONSTRAINT UQ_SalesOrder_Code
        UNIQUE (Code),

    CONSTRAINT UQ_SalesOrder_Branch_Date_Sequence
        UNIQUE (BranchId, BusinessDate, DailySequence),

    CONSTRAINT CK_SalesOrder_Code_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Code))) > 0),

    CONSTRAINT CK_SalesOrder_DailySequence
        CHECK (DailySequence > 0),

    CONSTRAINT CK_SalesOrder_TotalAmount
        CHECK (TotalAmount >= 0),

    CONSTRAINT CK_SalesOrder_Status
        CHECK
        (
            Status IN
            (
                N'COMPLETED',
                N'NEEDS_REVIEW',
                N'ADJUSTED',
                N'CANCELLED'
            )
        ),

    -- Khi đang NEEDS_REVIEW thì bắt buộc có thông tin báo sai.
    CONSTRAINT CK_SalesOrder_Review
        CHECK
        (
            Status <> N'NEEDS_REVIEW'
            OR
            (
                ReportedByUserId IS NOT NULL
                AND ReportedAt IS NOT NULL
                AND ReportReason IS NOT NULL
                AND LEN(LTRIM(RTRIM(ReportReason))) > 0
            )
        )
);
GO

CREATE INDEX IX_SalesOrder_Branch_Date_Status
ON dbo.SalesOrder(BranchId, BusinessDate, Status)
INCLUDE (Code, CreatedByUserId, TotalAmount);

CREATE INDEX IX_SalesOrder_CreatedByUser_Date
ON dbo.SalesOrder(CreatedByUserId, CreatedAt DESC)
INCLUDE (BranchId, Code, Status, TotalAmount);
GO


-- Counter dùng để sinh sequence theo từng branch + ngày.
-- Không soft delete vì sequence đã cấp không nên tái sử dụng.
CREATE TABLE dbo.OrderDailyCounter
(
    BranchId INT NOT NULL,
    BusinessDate DATE NOT NULL,

    LastNumber INT NOT NULL
        CONSTRAINT DF_OrderDailyCounter_LastNumber DEFAULT 0,

    UpdatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_OrderDailyCounter_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_OrderDailyCounter
        PRIMARY KEY (BranchId, BusinessDate),

    CONSTRAINT FK_OrderDailyCounter_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT CK_OrderDailyCounter_LastNumber
        CHECK (LastNumber >= 0)
);
GO


-- Snapshot tên/giá để lịch sử đơn không đổi khi menu thay đổi.
CREATE TABLE dbo.OrderItem
(
    Id INT IDENTITY(1,1) NOT NULL,
    SalesOrderId INT NOT NULL,
    ProductSizeId INT NOT NULL,

    ProductNameSnapshot NVARCHAR(150) NOT NULL,
    SizeNameSnapshot NVARCHAR(50) NOT NULL,

    Quantity INT NOT NULL,
    UnitPriceSnapshot DECIMAL(18,2) NOT NULL,

    -- Bao gồm giá sản phẩm + topping của dòng hàng này.
    -- Backend tính và lưu.
    SubtotalAmount DECIMAL(18,2) NOT NULL,

    CONSTRAINT PK_OrderItem PRIMARY KEY (Id),

    CONSTRAINT FK_OrderItem_SalesOrder
        FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrder(Id),

    CONSTRAINT FK_OrderItem_ProductSize
        FOREIGN KEY (ProductSizeId) REFERENCES dbo.ProductSize(Id),

    CONSTRAINT CK_OrderItem_ProductNameSnapshot_NotBlank
        CHECK (LEN(LTRIM(RTRIM(ProductNameSnapshot))) > 0),

    CONSTRAINT CK_OrderItem_SizeNameSnapshot_NotBlank
        CHECK (LEN(LTRIM(RTRIM(SizeNameSnapshot))) > 0),

    CONSTRAINT CK_OrderItem_Quantity
        CHECK (Quantity > 0),

    CONSTRAINT CK_OrderItem_UnitPriceSnapshot
        CHECK (UnitPriceSnapshot >= 0),

    CONSTRAINT CK_OrderItem_SubtotalAmount
        CHECK (SubtotalAmount >= 0)
);
GO

CREATE INDEX IX_OrderItem_SalesOrderId
ON dbo.OrderItem(SalesOrderId);
GO


CREATE TABLE dbo.OrderItemTopping
(
    Id INT IDENTITY(1,1) NOT NULL,
    OrderItemId INT NOT NULL,
    ToppingId INT NOT NULL,

    ToppingNameSnapshot NVARCHAR(150) NOT NULL,

    -- Số lượng topping trên mỗi món.
    Quantity INT NOT NULL
        CONSTRAINT DF_OrderItemTopping_Quantity DEFAULT 1,

    UnitPriceSnapshot DECIMAL(18,2) NOT NULL,

    CONSTRAINT PK_OrderItemTopping PRIMARY KEY (Id),

    CONSTRAINT FK_OrderItemTopping_OrderItem
        FOREIGN KEY (OrderItemId) REFERENCES dbo.OrderItem(Id),

    CONSTRAINT FK_OrderItemTopping_Topping
        FOREIGN KEY (ToppingId) REFERENCES dbo.Topping(Id),

    CONSTRAINT UQ_OrderItemTopping_Item_Topping
        UNIQUE (OrderItemId, ToppingId),

    CONSTRAINT CK_OrderItemTopping_NameSnapshot_NotBlank
        CHECK (LEN(LTRIM(RTRIM(ToppingNameSnapshot))) > 0),

    CONSTRAINT CK_OrderItemTopping_Quantity
        CHECK (Quantity > 0),

    CONSTRAINT CK_OrderItemTopping_UnitPriceSnapshot
        CHECK (UnitPriceSnapshot >= 0)
);
GO


CREATE TABLE dbo.OrderItemNote
(
    Id INT IDENTITY(1,1) NOT NULL,
    OrderItemId INT NOT NULL,
    NoteOptionId INT NOT NULL,

    NoteNameSnapshot NVARCHAR(100) NOT NULL,

    CONSTRAINT PK_OrderItemNote PRIMARY KEY (Id),

    CONSTRAINT FK_OrderItemNote_OrderItem
        FOREIGN KEY (OrderItemId) REFERENCES dbo.OrderItem(Id),

    CONSTRAINT FK_OrderItemNote_NoteOption
        FOREIGN KEY (NoteOptionId) REFERENCES dbo.NoteOption(Id),

    CONSTRAINT UQ_OrderItemNote_Item_Note
        UNIQUE (OrderItemId, NoteOptionId),

    CONSTRAINT CK_OrderItemNote_NameSnapshot_NotBlank
        CHECK (LEN(LTRIM(RTRIM(NoteNameSnapshot))) > 0)
);
GO


/* =========================================================
   MODULE 4 - INVENTORY / STOCK
   ========================================================= */

-- Giữ IngredientUnit để dữ liệu đơn vị thống nhất, tránh nhập tự do kg/KG/Kg...
CREATE TABLE dbo.IngredientUnit
(
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(50) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_IngredientUnit_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_IngredientUnit_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_IngredientUnit_Deleted DEFAULT 0,

    CONSTRAINT PK_IngredientUnit PRIMARY KEY (Id),

    CONSTRAINT CK_IngredientUnit_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE UNIQUE INDEX UX_IngredientUnit_Name
ON dbo.IngredientUnit(Name)
WHERE Deleted = 0;
GO


CREATE TABLE dbo.Ingredient
(
    Id INT IDENTITY(1,1) NOT NULL,
    UnitId INT NOT NULL,

    Name NVARCHAR(150) NOT NULL,

    IsActive BIT NOT NULL
        CONSTRAINT DF_Ingredient_IsActive DEFAULT 1,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Ingredient_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(0) NULL,

    Deleted BIT NOT NULL
        CONSTRAINT DF_Ingredient_Deleted DEFAULT 0,

    CONSTRAINT PK_Ingredient PRIMARY KEY (Id),

    CONSTRAINT FK_Ingredient_Unit
        FOREIGN KEY (UnitId) REFERENCES dbo.IngredientUnit(Id),

    CONSTRAINT CK_Ingredient_Name_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

CREATE UNIQUE INDEX UX_Ingredient_Name
ON dbo.Ingredient(Name)
WHERE Deleted = 0;
GO


-- Tồn hiện tại theo branch.
-- RowVersion dùng cho optimistic concurrency khi nhiều người cùng cập nhật tồn.
CREATE TABLE dbo.BranchIngredient
(
    Id INT IDENTITY(1,1) NOT NULL,
    BranchId INT NOT NULL,
    IngredientId INT NOT NULL,

    Quantity DECIMAL(18,3) NOT NULL
        CONSTRAINT DF_BranchIngredient_Quantity DEFAULT 0,

    WarningThreshold DECIMAL(18,3) NOT NULL
        CONSTRAINT DF_BranchIngredient_WarningThreshold DEFAULT 0,

    UpdatedAt DATETIME2(0) NULL,

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_BranchIngredient PRIMARY KEY (Id),

    CONSTRAINT FK_BranchIngredient_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT FK_BranchIngredient_Ingredient
        FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredient(Id),

    CONSTRAINT UQ_BranchIngredient_Branch_Ingredient
        UNIQUE (BranchId, IngredientId),

    CONSTRAINT CK_BranchIngredient_Quantity
        CHECK (Quantity >= 0),

    CONSTRAINT CK_BranchIngredient_WarningThreshold
        CHECK (WarningThreshold >= 0)
);
GO


-- Header của một nghiệp vụ nhập/xuất/hoàn tác/điều chỉnh kho.
-- Không soft delete vì đây là lịch sử giao dịch.
CREATE TABLE dbo.StockTransaction
(
    Id INT IDENTITY(1,1) NOT NULL,
    BranchId INT NOT NULL,

    PerformedByUserId INT NOT NULL,

    Type NVARCHAR(30) NOT NULL,

    -- Chỉ dùng khi Type = REVERSAL.
    OriginalTransactionId INT NULL,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_StockTransaction_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_StockTransaction PRIMARY KEY (Id),

    CONSTRAINT FK_StockTransaction_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT FK_StockTransaction_PerformedByUser
        FOREIGN KEY (PerformedByUserId) REFERENCES dbo.AppUser(Id),

    CONSTRAINT FK_StockTransaction_OriginalTransaction
        FOREIGN KEY (OriginalTransactionId) REFERENCES dbo.StockTransaction(Id),

    CONSTRAINT CK_StockTransaction_Type
        CHECK
        (
            Type IN
            (
                N'IN',
                N'OUT',
                N'REVERSAL',
                N'COUNT_ADJUSTMENT'
            )
        ),

    CONSTRAINT CK_StockTransaction_Original
        CHECK
        (
            (Type = N'REVERSAL' AND OriginalTransactionId IS NOT NULL)
            OR
            (Type <> N'REVERSAL' AND OriginalTransactionId IS NULL)
        )
);
GO

-- Một giao dịch gốc chỉ được reversal một lần.
CREATE UNIQUE INDEX UX_StockTransaction_OriginalTransaction
ON dbo.StockTransaction(OriginalTransactionId)
WHERE OriginalTransactionId IS NOT NULL;

CREATE INDEX IX_StockTransaction_Branch_Date
ON dbo.StockTransaction(BranchId, CreatedAt DESC)
INCLUDE (PerformedByUserId, Type, OriginalTransactionId);
GO


-- QuantityChange dùng dấu:
-- IN: dương
-- OUT: âm
-- COUNT_ADJUSTMENT: có thể dương hoặc âm
-- REVERSAL: ngược dấu giao dịch gốc
-- Backend chịu trách nhiệm kiểm tra dấu phù hợp Type.
CREATE TABLE dbo.StockTransactionDetail
(
    Id INT IDENTITY(1,1) NOT NULL,
    StockTransactionId INT NOT NULL,
    IngredientId INT NOT NULL,

    QuantityChange DECIMAL(18,3) NOT NULL,
    QuantityBefore DECIMAL(18,3) NOT NULL,
    QuantityAfter DECIMAL(18,3) NOT NULL,

    CONSTRAINT PK_StockTransactionDetail PRIMARY KEY (Id),

    CONSTRAINT FK_StockTransactionDetail_StockTransaction
        FOREIGN KEY (StockTransactionId) REFERENCES dbo.StockTransaction(Id),

    CONSTRAINT FK_StockTransactionDetail_Ingredient
        FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredient(Id),

    CONSTRAINT UQ_StockTransactionDetail_Transaction_Ingredient
        UNIQUE (StockTransactionId, IngredientId),

    CONSTRAINT CK_StockTransactionDetail_QuantityChange
        CHECK (QuantityChange <> 0),

    CONSTRAINT CK_StockTransactionDetail_QuantityBefore
        CHECK (QuantityBefore >= 0),

    CONSTRAINT CK_StockTransactionDetail_QuantityAfter
        CHECK (QuantityAfter >= 0),

    CONSTRAINT CK_StockTransactionDetail_Balance
        CHECK (QuantityAfter = QuantityBefore + QuantityChange)
);
GO


-- Phiếu kiểm kho.
CREATE TABLE dbo.Stocktake
(
    Id INT IDENTITY(1,1) NOT NULL,

    Code NVARCHAR(50) NOT NULL,
    BranchId INT NOT NULL,
    CheckedByUserId INT NOT NULL,

    -- Nếu kiểm kho có chênh lệch, trỏ tới COUNT_ADJUSTMENT đã tạo.
    AdjustmentTransactionId INT NULL,

    CompletedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Stocktake_CompletedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Stocktake PRIMARY KEY (Id),

    CONSTRAINT FK_Stocktake_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT FK_Stocktake_CheckedByUser
        FOREIGN KEY (CheckedByUserId) REFERENCES dbo.AppUser(Id),

    CONSTRAINT FK_Stocktake_AdjustmentTransaction
        FOREIGN KEY (AdjustmentTransactionId) REFERENCES dbo.StockTransaction(Id),

    CONSTRAINT UQ_Stocktake_Code
        UNIQUE (Code),

    CONSTRAINT CK_Stocktake_Code_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Code))) > 0)
);
GO

CREATE UNIQUE INDEX UX_Stocktake_AdjustmentTransaction
ON dbo.Stocktake(AdjustmentTransactionId)
WHERE AdjustmentTransactionId IS NOT NULL;

CREATE INDEX IX_Stocktake_Branch_Date
ON dbo.Stocktake(BranchId, CompletedAt DESC)
INCLUDE (CheckedByUserId, AdjustmentTransactionId);
GO


-- Difference tự tính để tránh BE gửi sai.
CREATE TABLE dbo.StocktakeItem
(
    Id INT IDENTITY(1,1) NOT NULL,
    StocktakeId INT NOT NULL,
    IngredientId INT NOT NULL,

    SystemQuantity DECIMAL(18,3) NOT NULL,
    ActualQuantity DECIMAL(18,3) NOT NULL,

    Difference AS (ActualQuantity - SystemQuantity) PERSISTED,

    CONSTRAINT PK_StocktakeItem PRIMARY KEY (Id),

    CONSTRAINT FK_StocktakeItem_Stocktake
        FOREIGN KEY (StocktakeId) REFERENCES dbo.Stocktake(Id),

    CONSTRAINT FK_StocktakeItem_Ingredient
        FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredient(Id),

    CONSTRAINT UQ_StocktakeItem_Stocktake_Ingredient
        UNIQUE (StocktakeId, IngredientId),

    CONSTRAINT CK_StocktakeItem_SystemQuantity
        CHECK (SystemQuantity >= 0),

    CONSTRAINT CK_StocktakeItem_ActualQuantity
        CHECK (ActualQuantity >= 0)
);
GO


/* =========================================================
   MODULE 5 - PAYROLL
   ========================================================= */

-- Payroll là dữ liệu tài chính/lịch sử nên KHÔNG soft delete.
CREATE TABLE dbo.Payroll
(
    Id INT IDENTITY(1,1) NOT NULL,

    EmployeeProfileId INT NOT NULL,

    -- Snapshot branch của kỳ lương để không sai lịch sử khi nhân viên chuyển chi nhánh.
    BranchId INT NOT NULL,

    [Year] SMALLINT NOT NULL,
    [Month] TINYINT NOT NULL,

    BaseSalarySnapshot DECIMAL(18,2) NOT NULL,

    WorkDays DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_Payroll_WorkDays DEFAULT 0,

    OvertimeHours DECIMAL(6,2) NOT NULL
        CONSTRAINT DF_Payroll_OvertimeHours DEFAULT 0,

    LeaveDays DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_Payroll_LeaveDays DEFAULT 0,

    TotalSalary DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_Payroll_TotalSalary DEFAULT 0,

    Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Payroll_Status DEFAULT N'DRAFT',

    PaidAt DATETIME2(0) NULL,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Payroll_CreatedAt DEFAULT SYSUTCDATETIME(),

    UpdatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Payroll_UpdatedAt DEFAULT SYSUTCDATETIME(),

    RowVersion ROWVERSION NOT NULL,

    CONSTRAINT PK_Payroll PRIMARY KEY (Id),

    CONSTRAINT FK_Payroll_EmployeeProfile
        FOREIGN KEY (EmployeeProfileId) REFERENCES dbo.EmployeeProfile(Id),

    CONSTRAINT FK_Payroll_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT UQ_Payroll_Employee_Year_Month
        UNIQUE (EmployeeProfileId, [Year], [Month]),

    CONSTRAINT CK_Payroll_Year
        CHECK ([Year] BETWEEN 2020 AND 2100),

    CONSTRAINT CK_Payroll_Month
        CHECK ([Month] BETWEEN 1 AND 12),

    CONSTRAINT CK_Payroll_NonNegative
        CHECK
        (
            BaseSalarySnapshot >= 0
            AND WorkDays >= 0
            AND OvertimeHours >= 0
            AND LeaveDays >= 0
            AND TotalSalary >= 0
        ),

    CONSTRAINT CK_Payroll_Status
        CHECK (Status IN (N'DRAFT', N'PAID')),

    CONSTRAINT CK_Payroll_PaidAt
        CHECK
        (
            (Status = N'DRAFT' AND PaidAt IS NULL)
            OR
            (Status = N'PAID' AND PaidAt IS NOT NULL)
        )
);
GO


/* =========================================================
   MODULE 6 - ORDER ADJUSTMENT + AUDIT
   ========================================================= */

-- Lịch sử chỉnh sửa order.
-- Không soft delete.
CREATE TABLE dbo.OrderAdjustment
(
    Id INT IDENTITY(1,1) NOT NULL,

    SalesOrderId INT NOT NULL,
    AdjustedByUserId INT NOT NULL,

    Reason NVARCHAR(500) NOT NULL,

    BeforeData NVARCHAR(MAX) NOT NULL,
    AfterData NVARCHAR(MAX) NOT NULL,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_OrderAdjustment_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_OrderAdjustment PRIMARY KEY (Id),

    CONSTRAINT FK_OrderAdjustment_SalesOrder
        FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrder(Id),

    CONSTRAINT FK_OrderAdjustment_AdjustedByUser
        FOREIGN KEY (AdjustedByUserId) REFERENCES dbo.AppUser(Id),

    CONSTRAINT CK_OrderAdjustment_Reason_NotBlank
        CHECK (LEN(LTRIM(RTRIM(Reason))) > 0),

    CONSTRAINT CK_OrderAdjustment_BeforeData_JSON
        CHECK (ISJSON(BeforeData) = 1),

    CONSTRAINT CK_OrderAdjustment_AfterData_JSON
        CHECK (ISJSON(AfterData) = 1)
);
GO

CREATE INDEX IX_OrderAdjustment_SalesOrder_Date
ON dbo.OrderAdjustment(SalesOrderId, CreatedAt DESC)
INCLUDE (AdjustedByUserId);
GO


-- Nhật ký toàn hệ thống.
-- BranchId NULL nếu thao tác thuộc dữ liệu dùng chung.
-- AuditLog là append-only, không soft delete.
CREATE TABLE dbo.AuditLog
(
    Id INT IDENTITY(1,1) NOT NULL,

    BranchId INT NULL,

    EntityName NVARCHAR(100) NOT NULL,
    EntityId INT NOT NULL,

    Action NVARCHAR(20) NOT NULL,

    BeforeData NVARCHAR(MAX) NULL,
    AfterData NVARCHAR(MAX) NULL,

    PerformedByUserId INT NOT NULL,

    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_AuditLog_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AuditLog PRIMARY KEY (Id),

    CONSTRAINT FK_AuditLog_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(Id),

    CONSTRAINT FK_AuditLog_PerformedByUser
        FOREIGN KEY (PerformedByUserId) REFERENCES dbo.AppUser(Id),

    CONSTRAINT CK_AuditLog_EntityName_NotBlank
        CHECK (LEN(LTRIM(RTRIM(EntityName))) > 0),

    CONSTRAINT CK_AuditLog_Action
        CHECK
        (
            Action IN
            (
                N'CREATE',
                N'UPDATE',
                N'SOFT_DELETE',
                N'RESTORE',
                N'ADJUST',
                N'REVERSE'
            )
        ),

    CONSTRAINT CK_AuditLog_BeforeData_JSON
        CHECK (BeforeData IS NULL OR ISJSON(BeforeData) = 1),

    CONSTRAINT CK_AuditLog_AfterData_JSON
        CHECK (AfterData IS NULL OR ISJSON(AfterData) = 1)
);
GO

CREATE INDEX IX_AuditLog_Branch_Date
ON dbo.AuditLog(BranchId, CreatedAt DESC)
INCLUDE (EntityName, EntityId, Action, PerformedByUserId);

CREATE INDEX IX_AuditLog_Entity_Date
ON dbo.AuditLog(EntityName, EntityId, CreatedAt DESC)
INCLUDE (Action, PerformedByUserId, BranchId);
GO


/* =========================================================
   PROCEDURE SINH MÃ ORDER AN TOÀN KHI NHIỀU REQUEST CÙNG LÚC
   Format: {BranchCode}-{yyyyMMdd}-{0001}
   Ví dụ: Q7-20260809-0001
   ========================================================= */

CREATE OR ALTER PROCEDURE dbo.usp_GetNextOrderCode
    @BranchId INT,
    @BusinessDate DATE,
    @OrderCode NVARCHAR(50) OUTPUT,
    @SequenceNumber INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BranchCode NVARCHAR(30);
    DECLARE @StartedTransaction BIT = 0;

    IF @@TRANCOUNT = 0
    BEGIN
        SET @StartedTransaction = 1;
        BEGIN TRANSACTION;
    END;

    BEGIN TRY

        SELECT @BranchCode = Code
        FROM dbo.Branch WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = @BranchId
          AND IsActive = 1
          AND Deleted = 0;

        IF @BranchCode IS NULL
        BEGIN
            THROW 51001,
                  N'Chi nhánh không tồn tại hoặc không hoạt động.',
                  1;
        END;


        SELECT @SequenceNumber = LastNumber
        FROM dbo.OrderDailyCounter WITH (UPDLOCK, HOLDLOCK)
        WHERE BranchId = @BranchId
          AND BusinessDate = @BusinessDate;


        IF @SequenceNumber IS NULL
        BEGIN
            SET @SequenceNumber = 1;

            INSERT INTO dbo.OrderDailyCounter
            (
                BranchId,
                BusinessDate,
                LastNumber
            )
            VALUES
            (
                @BranchId,
                @BusinessDate,
                @SequenceNumber
            );
        END
        ELSE
        BEGIN
            SET @SequenceNumber = @SequenceNumber + 1;

            UPDATE dbo.OrderDailyCounter
            SET LastNumber = @SequenceNumber,
                UpdatedAt = SYSUTCDATETIME()
            WHERE BranchId = @BranchId
              AND BusinessDate = @BusinessDate;
        END;


        SET @OrderCode =
            CONCAT
            (
                UPPER(@BranchCode),
                N'-',
                CONVERT(CHAR(8), @BusinessDate, 112),
                N'-',
                RIGHT
                (
                    N'0000' + CONVERT(NVARCHAR(10), @SequenceNumber),
                    4
                )
            );


        IF @StartedTransaction = 1
            COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH

        IF @StartedTransaction = 1
           AND XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;

    END CATCH;
END;
GO


/* =========================================================
   SEED DỮ LIỆU NỀN
   Chỉ seed dữ liệu cấu hình ổn định.
   Không seed tài khoản vì PasswordHash phải tạo bằng ứng dụng.
   ========================================================= */

INSERT INTO dbo.[Role] (Code, Name)
VALUES
(N'OWNER', N'Chủ sở hữu'),
(N'ADMIN', N'Quản trị viên'),
(N'EMPLOYEE', N'Nhân viên');
GO


INSERT INTO dbo.Size (Name)
VALUES
(N'M'),
(N'L');
GO


INSERT INTO dbo.IngredientUnit (Name)
VALUES
(N'kg'),
(N'g'),
(N'lít'),
(N'ml');
GO


INSERT INTO dbo.ToppingGroup (Name)
VALUES
(N'Trân châu'),
(N'Thạch'),
(N'Kem');
GO


INSERT INTO dbo.NoteGroup (Name)
VALUES
(N'Đá'),
(N'Đường');
GO


DECLARE @IceGroupId INT =
(
    SELECT Id
    FROM dbo.NoteGroup
    WHERE Name = N'Đá'
      AND Deleted = 0
);

DECLARE @SugarGroupId INT =
(
    SELECT Id
    FROM dbo.NoteGroup
    WHERE Name = N'Đường'
      AND Deleted = 0
);

INSERT INTO dbo.NoteOption (NoteGroupId, Name)
VALUES
(@IceGroupId, N'Bình thường'),
(@IceGroupId, N'Ít đá'),
(@IceGroupId, N'Không đá'),
(@SugarGroupId, N'Bình thường'),
(@SugarGroupId, N'Ít đường'),
(@SugarGroupId, N'Không đường');
GO


/* =========================================================
   CÁC QUY TẮC ĐỂ BACKEND XỬ LÝ
   =========================================================

   1. EMPLOYEE chỉ có 1 branch hiện tại:
      kiểm tra ở Service vì phụ thuộc Role.

   2. Product phải có ít nhất 2 ProductSize đang hoạt động:
      kiểm tra ở Service và transaction.

   3. Tạo order:
      FE chỉ gửi lựa chọn.
      BE lấy giá thật từ DB và tính:
      OrderItem.SubtotalAmount
      SalesOrder.TotalAmount.

   4. Note:
      Service chỉ cho phép tối đa 1 NoteOption trong mỗi NoteGroup
      trên cùng một OrderItem.

   5. Tạo mã order:
      gọi dbo.usp_GetNextOrderCode trong cùng transaction tạo order.

   6. Xuất kho:
      cập nhật BranchIngredient có điều kiện để không âm tồn.
      StockTransactionDetail lưu QuantityBefore/After/Change.

   7. Dấu QuantityChange:
      IN               > 0
      OUT              < 0
      REVERSAL         = ngược dấu giao dịch gốc
      COUNT_ADJUSTMENT = có thể +/-.
      Quy tắc này kiểm tra ở Service.

   8. Kiểm kho:
      StocktakeItem.Difference được SQL Server tự tính.
      Nếu có lệch, tạo StockTransaction loại COUNT_ADJUSTMENT.

   9. Payroll:
      TotalSalary do Backend tính.
      UpdatedAt do Backend cập nhật khi sửa.

   10. AuditLog / OrderAdjustment:
       chỉ INSERT, không xóa/sửa lịch sử.
   ========================================================= */

PRINT N'BranchFlowDB final schema đã tạo thành công.';
GO
