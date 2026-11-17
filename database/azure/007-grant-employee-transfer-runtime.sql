SET XACT_ABORT ON;
IF USER_ID(N'branchflow_api') IS NULL
BEGIN
    ;THROW 50001, N'Không tìm thấy database user branchflow_api.', 1;
END;
IF OBJECT_ID(N'dbo.EmployeeTransferRequest', N'U') IS NULL
BEGIN
    ;THROW 50002, N'Cần chạy script 006 trước.', 1;
END;
GRANT SELECT, INSERT, UPDATE ON OBJECT::dbo.EmployeeTransferRequest TO [branchflow_api];
