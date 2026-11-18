-- DỰ THẢO CẤP QUYỀN: chỉ chạy riêng sau khi duyệt triển khai Azure.
-- Không thay schema, không cấp DELETE và không cho runtime sửa Quantity trực tiếp.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'BranchFlowDB-Demo'
BEGIN
    ;THROW 50001, N'Đang chọn sai database demo.', 1;
END;
IF USER_ID(N'branchflow_api') IS NULL
BEGIN
    ;THROW 50002, N'Không tìm thấy database user branchflow_api.', 1;
END;
IF OBJECT_ID(N'dbo.BranchIngredient', N'U') IS NULL
    OR OBJECT_ID(N'dbo.Ingredient', N'U') IS NULL
    OR OBJECT_ID(N'dbo.IngredientUnit', N'U') IS NULL
    OR OBJECT_ID(N'dbo.StockTransactionDetail', N'U') IS NULL
    OR OBJECT_ID(N'dbo.StocktakeItem', N'U') IS NULL
BEGIN
    ;THROW 50003, N'Chưa có đủ schema kho để kiểm tra dữ liệu đã sử dụng.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;
    GRANT SELECT, INSERT, UPDATE ON OBJECT::dbo.IngredientUnit TO [branchflow_api];
    GRANT SELECT, INSERT, UPDATE ON OBJECT::dbo.Ingredient TO [branchflow_api];
    GRANT SELECT, INSERT ON OBJECT::dbo.BranchIngredient TO [branchflow_api];
    GRANT UPDATE (WarningThreshold, UpdatedAt) ON OBJECT::dbo.BranchIngredient TO [branchflow_api];
    GRANT SELECT ON OBJECT::dbo.StockTransactionDetail TO [branchflow_api];
    GRANT SELECT ON OBJECT::dbo.StocktakeItem TO [branchflow_api];
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;
    THROW;
END CATCH;
