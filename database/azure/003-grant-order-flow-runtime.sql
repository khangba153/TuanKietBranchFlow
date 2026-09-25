-- Chỉ cấp quyền cho database demo và user chạy API.
IF DB_NAME() <> N'BranchFlowDB-Demo'
    THROW 50000, N'Đang chọn sai database.', 1;

IF USER_ID(N'branchflow_api') IS NULL
    THROW 50001, N'Không tìm thấy database user branchflow_api.', 1;

-- Đọc menu và lịch sử đơn.
GRANT SELECT ON OBJECT::dbo.BranchProduct TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.BranchTopping TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.Category TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.NoteGroup TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.NoteOption TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.OrderItem TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.OrderItemNote TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.OrderItemTopping TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.Product TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.ProductSize TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.SalesOrder TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.Size TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.Topping TO [branchflow_api];
GRANT SELECT ON OBJECT::dbo.ToppingGroup TO [branchflow_api];

-- Tạo đơn và các dòng chi tiết.
GRANT INSERT ON OBJECT::dbo.SalesOrder TO [branchflow_api];
GRANT INSERT ON OBJECT::dbo.OrderItem TO [branchflow_api];
GRANT INSERT ON OBJECT::dbo.OrderItemTopping TO [branchflow_api];
GRANT INSERT ON OBJECT::dbo.OrderItemNote TO [branchflow_api];

-- Báo sai đơn và cấp mã đơn.
GRANT UPDATE ON OBJECT::dbo.SalesOrder TO [branchflow_api];
GRANT EXECUTE ON OBJECT::dbo.usp_GetNextOrderCode TO [branchflow_api];