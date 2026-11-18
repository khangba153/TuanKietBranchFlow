# Bộ seed danh mục kiểm kho của quán

Chuẩn hóa ngày 2026-11-18, giữ đủ 46 dòng gốc. Sản phẩm được chọn để demo,
không khẳng định đúng toàn bộ hãng/quy cách quán đã dùng.

## Cách dùng

Từ root project, dùng PowerShell:

```powershell
# Xem trước, không kết nối database.
./database/seeds/inventory/Seed-InventoryCatalog.ps1

# Kiểm tra SQL local: chạy hai lượt trong cùng transaction rồi rollback.
./database/seeds/inventory/Seed-InventoryCatalog.ps1 -RollbackTest

# Nạp danh mục vào SQL local khi muốn dùng dữ liệu này.
./database/seeds/inventory/Seed-InventoryCatalog.ps1 -Apply

# Thêm cấu hình Quantity=0/ngưỡng mẫu cho từng chi nhánh local đang hoạt động.
./database/seeds/inventory/Seed-InventoryCatalog.ps1 -Apply -ConfigureBranches
```

Script chỉ nhận cấu hình local hiện có của API, bắt buộc server 127.0.0.1,1433
và database BranchFlowDB. Không chạy cloud, không chứa secret, không sửa schema,
không xóa/ghi đè nguyên liệu đã có, không bật lại mục inactive. Trùng tên nhưng khác
đơn vị/trạng thái thì dừng và rollback, không lén đổi đơn vị đã sử dụng.

Chạy lại không tạo trùng. Đây là seed dữ liệu mẫu bằng SQL trực tiếp có transaction,
không thay thế API nghiệp vụ; không được dùng để nhập tồn cho cửa hàng thật.

## Đơn vị và số lượng

- Có 11 đơn vị đóng gói: Gói, Hộp, Lon, Túi, Cuộn, Xô, Hũ, Chai, Thùng, Bịch, Bao.
  (Danh sách thực tế được kiểm tra bằng script; không có quy đổi giữa các đơn vị.)
- 35 hồng trà = 35 gói theo mẫu đã chọn, không phải 35kg.
- 5 đào dầm = 5 xô; 5kg/xô nằm trong tên, không tự nhân thành 25kg trong tồn.
- 3 thạch cá = 3 hũ 2,5kg, không tự đổi thành 7,5kg.
- Bao là bao ngoài chứa nhiều bịch nhựa đựng nước; 2,5 giữ là 2,5 bao, không gán kg.
- Me đác thiếu số lượng -> JSON null, không gán 0.
- referenceQuantity là số kiểm đếm lịch sử của người dùng, không biết ngày/chi nhánh
  thực tế; không phải tồn hiện tại hoặc số lượng mua hàng được xác minh.
- Mặc định script chỉ seed IngredientUnit và Ingredient. Khi thêm ConfigureBranches,
  script tạo BranchIngredient còn thiếu với Quantity=0 cho các branch active/chưa xóa;
  không thay đổi Quantity/ngưỡng của cấu hình đã tồn tại, không tạo giao dịch hoặc kiểm kê.
  Ngưỡng mẫu = max(1,ceil(referenceQuantity*20%)); thiếu số tham khảo dùng 2.
  Đây là mức demo theo đơn vị đóng gói, không phải chính sách cảnh báo vận hành thực tế.
  Tồn đầu kỳ phải đi qua luồng nhập kho khi B5.2
  được triển khai; số lượng gốc đã được lưu sẵn để đối chiếu sau.
- group là nhãn tổ chức trong file dữ liệu, chưa phải trường/phân loại trên UI hoặc DB.
  Bao bì/vật tư dùng chung bảng Ingredient theo schema hiện có, không mở thêm module.

## Nguồn và độ chắc chắn

Mỗi dòng trong `shop-inventory.json` có originalName, name, unit, referenceQuantity,
source, match và note khi cần. Map sources giữ URL đầy đủ để tra lại.

- Có sản phẩm thực tế đại diện: trà Lộc Phát, trà túi lọc Cozy, trà Thái ChaTraMue,
  Milo Nestlé gói 1kg, đào/vải ngâm Hosen, 3Q Wings, trân châu đen Gia Uy,
  siro Golden Farm/Gia Thịnh Phát, thạch cá Douxian, thạch trứng Trung Lâm...
- Quy cách người dùng xác nhận được ưu tiên: cuộn ép; thạch cá hũ 2,5kg; túi trân
  châu; loại đen chưa nấu; đào dầm xô 5kg; me đác hũ Tấn Lộc; bao ngoài chứa bịch.
- Không xác minh được sản phẩm me hạt đác Tấn Lộc chính xác trên web: giữ đúng tên
  người dùng, chỉ liên kết nguồn thương hiệu. Không tự thay bằng chanh dây hạt đác.
- Đào dầm: nguồn tham khảo tìm thấy mứt filling đào xô 5kg, không chứng minh đó là
  cùng sản phẩm quán dùng. Giữ tên đào dầm theo người dùng, không tự gán hãng STC.
- Thạch trái cây tổng hợp: chỉ xác minh nhóm jelly vị trái cây; không bịa hãng,
  khối lượng hoặc mã SKU. Bao nhỏ/lớn/5kg/đôi cũng chưa có SKU khớp chính xác.
- Bánh Milo: chọn Nuggets 30g từ nguồn bán lẻ lịch sử, chưa khẳng định là đúng
  loại bánh quán từng dùng hoặc hiện còn bán. Không gọi mọi bánh Milo là Cubes.
- Các nhãn/khối lượng còn lại là lựa chọn demo, không phải bằng chứng thực tế quán.
- Không lấy giá bán, hạn sử dụng, số cái/thùng hoặc tỉ lệ quy đổi để tự seed.

Nguồn tiêu biểu:

- [Trà Lộc Phát](https://tralocphat.com/collections/tra-la)
- [Milo Nestlé Professional](https://www.nestleprofessional.com.vn/nestle-milo/milo-bot-goi-1kg)
- [Sản phẩm mứt xô 5kg STC](https://stcfood.com.vn/mut-sinh-to-nuoc-ep-trai-cay)
- [Thạch cá Douxian 2,5kg](https://kotavn.com/product/thach-ca-douxian-hop-2-5kg/)
- [Tấn Lộc: thông tin thương hiệu và nhóm sản phẩm](https://dametanloc.vn/vi/gioi-thieu.html)

## Trạng thái thực hiện

Đã chạy RollbackTest trên SQL local ngày 2026-11-18: lượt 1 thêm 11 đơn vị và 46 mặt
hàng; lượt 2 thêm 0/0; đã rollback toàn bộ.

Sau khi người dùng duyệt nạp, đã chạy Apply ngày 2026-11-18: thêm 11 đơn vị và 46
mặt hàng vào BranchFlowDB local. Chạy lại kiểm chứng thêm 0/0, không tạo trùng.
Không tạo cấu hình chi nhánh, không ghi tồn hoặc giao dịch kho, không chạy Azure.

Sau yêu cầu cấu hình đầy đủ cùng ngày: đã thử RollbackTest -ConfigureBranches
(92 cấu hình mới, lượt 2 thêm 0), rồi Apply tạo 92 cấu hình = 46 mặt hàng x 2 chi nhánh
local đang hoạt động. Chạy lại thêm 0. Tất cả cấu hình mới Quantity=0, có ngưỡng mẫu;
không cập nhật số kiểm đếm lịch sử thành tồn thực tế hoặc tạo StockTransaction.
