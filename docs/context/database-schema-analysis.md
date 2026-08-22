# BranchFlowDB merged schema — bản phân tích

Nguồn: `C:\Users\Admin\Downloads\BranchFlowDB_Final_Merged.txt`.

## Thống kê đã kiểm tra

- SQL Server 2019+, database `BranchFlowDB`.
- 30 `CREATE TABLE`.
- 42 foreign key.
- 61 check constraint.
- 61 default constraint.
- 31 câu lệnh tạo index tường minh, ngoài primary key/unique constraint sinh index.
- Hai cột `ROWVERSION` thực tế tại `BranchIngredient` và `Payroll`.
- Một computed persisted column: `StocktakeItem.Difference`.
- Một procedure: `dbo.usp_GetNextOrderCode`.
- Seed role, size, ingredient unit, topping group, note group và note option.
- Không phát hiện pattern connection string/password/user id/secret trong script.

## Danh sách bảng

### Tổ chức và tài khoản

`Role`, `AppUser`, `Branch`, `UserBranch`, `EmployeeProfile`.

### Menu

`Category`, `Product`, `Size`, `ProductSize`, `ToppingGroup`, `Topping`, `NoteGroup`, `NoteOption`, `BranchProduct`, `BranchTopping`.

### Order

`SalesOrder`, `OrderDailyCounter`, `OrderItem`, `OrderItemTopping`, `OrderItemNote`.

`OrderAdjustment` được đặt ở module audit trong script nhưng thuộc flow điều chỉnh order.

### Kho

`IngredientUnit`, `Ingredient`, `BranchIngredient`, `StockTransaction`, `StockTransactionDetail`, `Stocktake`, `StocktakeItem`.

### Payroll và audit

`Payroll`, `OrderAdjustment`, `AuditLog`.

## Quy tắc quan trọng trong schema

- Khóa chính dùng `INT IDENTITY(1,1)`.
- Chuỗi tiếng Việt dùng `NVARCHAR`; thời gian dùng `DATETIME2` với default UTC.
- Tiền `DECIMAL(18,2)`; lượng kho `DECIMAL(18,3)`.
- Không cascade delete.
- Filtered unique index hỗ trợ soft delete cho master data.
- Transaction/history không soft delete.
- `SalesOrder.Code` và `(BranchId, BusinessDate, DailySequence)` phải duy nhất.
- Snapshot order bảo toàn tên và giá lịch sử.
- `QuantityAfter = QuantityBefore + QuantityChange` được bảo vệ bằng CHECK.
- Reversal gốc và stocktake adjustment bị giới hạn liên kết một lần.
- JSON audit/order adjustment có `ISJSON` check.

## Quy tắc phải xử lý trong backend

- EMPLOYEE chỉ có một branch hiện tại.
- Product có ít nhất hai size hoạt động trong cùng transaction.
- FE chỉ gửi lựa chọn order; backend đọc giá thật và tính subtotal/total.
- Tối đa một note option trong mỗi note group cho một order item.
- Procedure sinh mã phải nằm trong cùng transaction tạo order.
- Xuất kho cập nhật có điều kiện để chống âm và phải xử lý concurrency.
- Dấu `QuantityChange` phải phù hợp loại giao dịch.
- Stocktake có lệch phải tạo `COUNT_ADJUSTMENT`.
- Backend tính payroll total và cập nhật `UpdatedAt`.
- `AuditLog` và `OrderAdjustment` chỉ insert.

## Vấn đề phải sửa trước khi gọi là final

1. File không có `SET ANSI_NULLS ON`.
2. File không có `SET QUOTED_IDENTIFIER ON`, trong khi dùng filtered index và persisted computed column.
3. Header dùng `USE master`, `CREATE DATABASE`, `USE BranchFlowDB`; cần tách hoặc điều chỉnh cho môi trường Azure SQL/database đã được provision.
4. Cần chốt order sequence bốn chữ số.
5. Cần chốt FK người tạo order và nơi kiểm tra role EMPLOYEE.
6. Cần chốt mức ràng buộc branch/type cho reversal và stocktake adjustment.
7. File có phần mở rộng `.txt`; khi đưa vào source nên lưu `.sql` sau khi hoàn tất review.

Không chạy script vào database chứa dữ liệu thật trước khi xử lý các điểm này và có backup.

## Quan hệ với EF Core

Ngày 2026-08-22 đã chốt dùng **Database First**:

```text
Hoàn thiện schema SQL
-> tạo/cập nhật database có kiểm soát
-> scaffold entity và DbContext vào Infrastructure
-> viết Repository/Service dựa trên model được sinh
```

Schema SQL là nguồn sự thật. Không tạo EF Core Migration cho thay đổi schema nếu chưa có quyết định mới thay thế Database First; không chỉnh entity sinh tự động rồi mong database thay đổi theo. Khi database đổi, cập nhật script/SQL trước và scaffold lại có kiểm soát, tránh ghi đè phần code nghiệp vụ.
