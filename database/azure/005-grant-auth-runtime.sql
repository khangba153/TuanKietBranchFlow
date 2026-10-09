SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Chỉ cấp quyền trên database demo.
IF DB_NAME() <> N'BranchFlowDB-Demo'
BEGIN
    ;THROW 50001, N'Đang chọn sai database.', 1;
END;

-- Dùng database user đã có trong script cấp quyền trước.
IF USER_ID(N'branchflow_api') IS NULL
BEGIN
    ;THROW 50002, N'Không tìm thấy database user branchflow_api.', 1;
END;

IF OBJECT_ID(N'dbo.AuthSession', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RefreshToken', N'U') IS NULL
BEGIN
    ;THROW 50003, N'Cần tạo đủ hai bảng auth trước khi cấp quyền.', 1;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Đọc phiên, tạo phiên và cập nhật thời điểm thu hồi.
    GRANT SELECT, INSERT, UPDATE
        ON OBJECT::dbo.AuthSession TO [branchflow_api];

    -- Đọc hash, tạo token mới và cập nhật UsedAt/RevokedAt.
    GRANT SELECT, INSERT, UPDATE
        ON OBJECT::dbo.RefreshToken TO [branchflow_api];

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;