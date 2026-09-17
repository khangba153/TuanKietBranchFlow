/*
    BranchFlowDB / BranchFlowDB-Demo - dữ liệu menu ban đầu của Tuấn Kiệt.

    Quy ước đã chốt:
    - Mỗi tên ngăn bởi dấu "/" trong menu gốc là một Product riêng.
    - Giá dùng chung cho mọi chi nhánh và nằm tại ProductSize.
    - Tất cả món và topping được mở bán ban đầu tại mọi branch đang hoạt động.
    - Topping thường có đơn giá 5K/phần; quantity 1/2/3 tương ứng 5K/10K/15K.
    - UP SIZE 1300ML được lưu như topping riêng giá 10K và chỉ được chọn 1 lần.
    - Đây là script dữ liệu ban đầu chạy một lần, không phải script đồng bộ giá.
*/

SET XACT_ABORT ON;
GO

IF DB_NAME() NOT IN (N'BranchFlowDB', N'BranchFlowDB-Demo')
    THROW 50001, N'Hãy kết nối BranchFlowDB (local) hoặc BranchFlowDB-Demo (Azure) trước khi chạy seed.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ActiveBranchCount INT;

    SELECT @ActiveBranchCount = COUNT(*)
    FROM dbo.Branch B
    WHERE B.IsActive = 1
      AND B.Deleted = 0;

    IF @ActiveBranchCount = 0
        THROW 50002, N'Không tìm thấy branch nào đang hoạt động.', 1;

    -- Không chạy lại seed sau khi hệ thống đã có Product để tránh ghi đè dữ liệu vận hành.
    IF EXISTS (SELECT 1 FROM dbo.Product)
        THROW 50003, N'Bảng Product đã có dữ liệu. Dừng seed để tránh chèn hoặc ghi đè ngoài ý muốn.', 1;

    DECLARE @Menu TABLE
    (
        CategoryName NVARCHAR(150) NOT NULL,
        ProductName NVARCHAR(150) NOT NULL,
        Price500 DECIMAL(18,2) NOT NULL,
        Price700 DECIMAL(18,2) NOT NULL,
        Price1000 DECIMAL(18,2) NOT NULL,
        ImageUrl NVARCHAR(1000) NULL
    );

    DECLARE @ToppingMenu TABLE
    (
        ToppingGroupName NVARCHAR(100) NOT NULL,
        ToppingName NVARCHAR(150) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL
    );

    INSERT INTO @Menu
        (CategoryName, ProductName, Price500, Price700, Price1000, ImageUrl)
    VALUES
    (N'Bestseller!!! & Signature', N'HỒNG TRÀ TẮC ĐÁ XAY', 16000, 20000, 25000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ed2ohv7wl8fc'),
    (N'Bestseller!!! & Signature', N'HỒNG TRÀ ĐÀO ĐÁ XAY', 17000, 24000, 30000, NULL),
    (N'Bestseller!!! & Signature', N'HỒNG TRÀ TẮC THÁI XANH XÍ MUỘI', 17000, 24000, 28000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ecv8t8xxh8f8'),
    (N'Bestseller!!! & Signature', N'GIỌT NGỌC DƯỠNG SINH', 25000, 32000, 40000, NULL),
    (N'Bestseller!!! & Signature', N'LONG NHÃN TUẤN KIỆT', 24000, 28000, 34000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3edz32m2qld0c'),
    (N'Bestseller!!! & Signature', N'TRÀ ME MUỐI ỚT', 28000, 32000, 40000, NULL),
    (N'Bestseller!!! & Signature', N'ĐÁ ME ĐẬU PHỘNG', 22000, 28000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ecb3xppm0le9'),

    (N'Trà !!!', N'TRÀ TRÁI CÂY NHIỆT ĐỚI', 27000, 32000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ed2zht4iphfb'),
    (N'Trà !!!', N'TRÀ MÃNG CẦU', 27000, 30000, 40000, NULL),
    (N'Trà !!!', N'TRÀ DƯA LƯỚI', 27000, 30000, 40000, NULL),
    (N'Trà !!!', N'TRÀ THƠM', 27000, 30000, 37000, NULL),
    (N'Trà !!!', N'TRÀ XOÀI', 27000, 30000, 37000, NULL),
    (N'Trà !!!', N'TRÀ ỔI', 27000, 30000, 37000, NULL),
    (N'Trà !!!', N'TRÀ DƯA HẤU', 27000, 30000, 37000, NULL),
    (N'Trà !!!', N'TRÀ CHANH DÂY', 27000, 30000, 37000, NULL),
    (N'Trà !!!', N'TRÀ CHANH DÂY XOÀI', 27000, 30000, 37000, NULL),
    (N'Trà !!!', N'TRÀ VẢI TRÂN CHÂU THẠCH', 27000, 30000, 37000, NULL),
    (N'Trà !!!', N'TRÀ CHANH DÂY CAM XOÀI', 27000, 30000, 37000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ecfal197lod2'),
    (N'Trà !!!', N'TRÀ GỪNG CAM SẢ', 27000, 30000, 37000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ee7a5w8v5o9b'),
    (N'Trà !!!', N'TRÀ CÓC GIÃ TAY', 27000, 30000, 37000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3edv8977uht4a'),
    (N'Trà !!!', N'TRÀ CHANH TƯƠI', 22000, 27000, 32000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ee5gko888s20'),

    (N'Nước Ép', N'NƯỚC ÉP THƠM', 22000, 32000, 37000, NULL),
    (N'Nước Ép', N'NƯỚC ÉP ỔI', 22000, 32000, 37000, NULL),
    (N'Nước Ép', N'NƯỚC ÉP DƯA HẤU', 22000, 32000, 37000, NULL),
    (N'Nước Ép', N'NƯỚC ÉP CAM', 22000, 32000, 37000, NULL),

    (N'Nước Sâm', N'SÂM BÔNG CÚC', 19000, 24000, 28000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3eccnf6ik6t70'),
    (N'Nước Sâm', N'SÂM RONG BIỂN', 19000, 24000, 28000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ecfjh88mqp3c'),
    (N'Nước Sâm', N'SÂM ĐẮNG GIẢI NHIỆT', 19000, 24000, 28000, NULL),
    (N'Nước Sâm', N'SÂM BÍ ĐAO', 19000, 24000, 28000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-820l4-meh3vfp4nm6a23'),
    (N'Nước Sâm', N'SÂM THẢO MỘC 24 VỊ', 21000, 27000, 31000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ecvuwgm9bpa6'),

    (N'Matcha', N'MATCHA LATTE', 28000, 37000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ee84x98ek5cb'),
    (N'Matcha', N'MATCHA ĐÁ XAY', 28000, 37000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m4zz3ymvop1460'),
    (N'Matcha', N'MATCHA LATTE DÂU', 30000, 39000, 45000, NULL),
    (N'Matcha', N'MATCHA LATTE VIỆT QUẤT', 30000, 39000, 45000, NULL),
    (N'Matcha', N'MATCHA LATTE ĐÀO', 30000, 39000, 45000, NULL),
    (N'Matcha', N'SỮA CHUA MATCHA', 32000, 37000, 47000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ee8lav9lewd8'),

    (N'Trà Sữa', N'TRÀ SỮA TRUYỀN THỐNG', 20000, 29000, 38000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ecwg23nnwc05'),
    (N'Trà Sữa', N'TRÀ SỮA TRÂN CHÂU ĐƯỜNG ĐEN', 20000, 29000, 38000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3edbfma45v9c4'),

    (N'Sữa Tươi', N'SÂM DỨA SỮA', 22000, 27000, 37000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3xcel0ry1th42'),
    (N'Sữa Tươi', N'SỮA TƯƠI DÂU', 22000, 27000, 37000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ed2hyasmnwf3'),
    (N'Sữa Tươi', N'SỮA TƯƠI TRÂN CHÂU ĐƯỜNG ĐEN', 22000, 32000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3xcfw2f3mi000'),
    (N'Sữa Tươi', N'SỮA TƯƠI THẠCH ĐÀO', 22000, 32000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ecw0iqe8cc3d'),
    (N'Sữa Tươi', N'MILO DẦM', 24000, 32000, 40000, NULL),
    (N'Sữa Tươi', N'CACAO ĐÁ XAY', 24000, 32000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ed279qguco15'),
    (N'Sữa Tươi', N'DỪA XAY CACAO', 24000, 32000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ed92gkp069b9'),
    (N'Sữa Tươi', N'CHANH DÂY SỮA ĐÁ', 24000, 32000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3edtvqa8azs50'),

    (N'Sữa Chua', N'SỮA CHUA VIỆT QUẤT', 27000, 32000, 40000, NULL),
    (N'Sữa Chua', N'SỮA CHUA DÂU', 27000, 32000, 40000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ec9jlq5arg46'),
    (N'Sữa Chua', N'SỮA CHUA DƯA LƯỚI', 27000, 32000, 40000, NULL),
    (N'Sữa Chua', N'SỮA CHUA DƯA LƯỚI MIX TRÁI CÂY', 30000, 37000, 45000, NULL),
    (N'Sữa Chua', N'SỮA CHUA VIỆT QUẤT MIX TRÁI CÂY', 30000, 37000, 45000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3edu1gfumcoe5'),
    (N'Sữa Chua', N'SỮA CHUA DÂU MIX TRÁI CÂY', 30000, 37000, 45000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3edu70rpf3wc7'),

    (N'Nước Dừa', N'NƯỚC DỪA TẮC', 18000, 22000, 32000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m4cv0r3h9fg021'),
    (N'Nước Dừa', N'NƯỚC DỪA MIX HẠT CHIA', 24000, 28000, 37000, NULL),
    (N'Nước Dừa', N'NƯỚC DỪA MIX TRÂN CHÂU', 24000, 28000, 37000, NULL),
    (N'Nước Dừa', N'NƯỚC DỪA MIX ỔI', 24000, 28000, 37000, NULL),
    (N'Nước Dừa', N'NƯỚC DỪA MIX XOÀI', 24000, 28000, 37000, NULL),
    (N'Nước Dừa', N'NƯỚC DỪA MIX THƠM', 24000, 28000, 37000, NULL),
    (N'Nước Dừa', N'NƯỚC DỪA MIX MÍT', 24000, 28000, 37000, NULL),
    (N'Nước Dừa', N'NƯỚC DỪA MIX CAM', 24000, 28000, 37000, NULL),
    (N'Nước Dừa', N'NƯỚC DỪA MIX DƯA HẤU', 24000, 28000, 37000, NULL),
    (N'Nước Dừa', N'NƯỚC DỪA MIX MATCHA', 24000, 28000, 37000, N'https://down-zl-vn.img.susercontent.com/vn-11134517-7ras8-m3ee8apcrw6gda');

    INSERT INTO @ToppingMenu (ToppingGroupName, ToppingName, UnitPrice)
    VALUES
    (N'Trân châu', N'TRÂN CHÂU ĐEN', 5000),
    (N'Trân châu', N'TRÂN CHÂU TRẮNG', 5000),
    (N'Thạch', N'THẠCH CÁ', 5000),
    (N'Thạch', N'THẠCH TRỨNG', 5000),
    (N'Thạch', N'THẠCH TÁO', 5000),
    (N'Thạch', N'THẠCH TRÁI CÂY', 5000),
    (N'Thạch', N'THẠCH PUDDING', 5000),
    (N'Thạch', N'THẠCH RAU CÂU', 5000),
    (N'Thạch', N'THẠCH CỦ NĂNG', 5000),
    (N'Up size', N'UP SIZE 1300ML', 10000);

    IF (SELECT COUNT(*) FROM @Menu) <> 62
        THROW 50004, N'Danh sách menu không đủ 62 sản phẩm sau khi tách các lựa chọn.', 1;

    IF EXISTS
    (
        SELECT M.ProductName
        FROM @Menu M
        GROUP BY M.ProductName
        HAVING COUNT(*) > 1
    )
        THROW 50005, N'Danh sách seed có tên sản phẩm bị trùng.', 1;

    IF (SELECT COUNT(*) FROM @ToppingMenu) <> 10
        THROW 50009, N'Danh sách topping không đủ 10 lựa chọn.', 1;

    -- Menu thật dùng dung tích; ngừng M/L cũ nếu chúng chưa được sản phẩm nào sử dụng.
    UPDATE S
    SET S.IsActive = 0,
        S.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Size S
    WHERE S.Name IN (N'M', N'L')
      AND S.Deleted = 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ProductSize PS
          WHERE PS.SizeId = S.Id
            AND PS.Deleted = 0
      );

    INSERT INTO dbo.Size (Name)
    SELECT V.Name
    FROM (VALUES (N'500ML'), (N'700ML'), (N'1000ML')) V(Name)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Size S
        WHERE S.Name = V.Name
          AND S.Deleted = 0
    );

    INSERT INTO dbo.Category (Name)
    SELECT DISTINCT M.CategoryName
    FROM @Menu M
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Category C
        WHERE C.Name = M.CategoryName
          AND C.Deleted = 0
    );

    INSERT INTO dbo.Product (CategoryId, Name, ImageUrl)
    SELECT C.Id, M.ProductName, M.ImageUrl
    FROM @Menu M
    INNER JOIN dbo.Category C
        ON C.Name = M.CategoryName
       AND C.Deleted = 0;

    ;WITH MenuPrice AS
    (
        SELECT M.ProductName, X.SizeName, X.Price
        FROM @Menu M
        CROSS APPLY
        (
            VALUES
                (N'500ML', M.Price500),
                (N'700ML', M.Price700),
                (N'1000ML', M.Price1000)
        ) X(SizeName, Price)
    )
    INSERT INTO dbo.ProductSize (ProductId, SizeId, Price)
    SELECT P.Id, S.Id, MP.Price
    FROM MenuPrice MP
    INNER JOIN dbo.Product P
        ON P.Name = MP.ProductName
       AND P.Deleted = 0
    INNER JOIN dbo.Size S
        ON S.Name = MP.SizeName
       AND S.Deleted = 0;

    INSERT INTO dbo.BranchProduct (BranchId, ProductId, IsAvailable)
    SELECT B.Id, P.Id, 1
    FROM dbo.Branch B
    CROSS JOIN dbo.Product P
    INNER JOIN @Menu M
        ON M.ProductName = P.Name
    WHERE B.IsActive = 1
      AND B.Deleted = 0
      AND P.Deleted = 0;

    INSERT INTO dbo.ToppingGroup (Name)
    SELECT N'Up size'
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.ToppingGroup TG
        WHERE TG.Name = N'Up size'
          AND TG.Deleted = 0
    );

    INSERT INTO dbo.Topping (ToppingGroupId, Name, Price)
    SELECT TG.Id, TM.ToppingName, TM.UnitPrice
    FROM @ToppingMenu TM
    INNER JOIN dbo.ToppingGroup TG
        ON TG.Name = TM.ToppingGroupName
       AND TG.Deleted = 0;

    INSERT INTO dbo.BranchTopping (BranchId, ToppingId, IsAvailable)
    SELECT B.Id, T.Id, 1
    FROM dbo.Branch B
    CROSS JOIN dbo.Topping T
    INNER JOIN @ToppingMenu TM
        ON TM.ToppingName = T.Name
    WHERE B.IsActive = 1
      AND B.Deleted = 0
      AND T.Deleted = 0;

    -- Kiểm tra toàn vẹn trước khi commit dữ liệu ban đầu.
    IF
    (
        SELECT COUNT(*)
        FROM dbo.Product P
        INNER JOIN @Menu M ON M.ProductName = P.Name
        WHERE P.Deleted = 0
    ) <> 62
        THROW 50006, N'Không tạo đủ 62 Product.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.ProductSize PS
        INNER JOIN dbo.Product P ON P.Id = PS.ProductId
        INNER JOIN @Menu M ON M.ProductName = P.Name
        WHERE PS.Deleted = 0
    ) <> 186
        THROW 50007, N'Không tạo đủ 186 ProductSize.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.BranchProduct BP
        INNER JOIN dbo.Product P ON P.Id = BP.ProductId
        INNER JOIN @Menu M ON M.ProductName = P.Name
    ) <> 62 * @ActiveBranchCount
        THROW 50008, N'Không tạo đủ BranchProduct cho các branch đang hoạt động.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.Topping T
        INNER JOIN @ToppingMenu TM ON TM.ToppingName = T.Name
        WHERE T.Deleted = 0
    ) <> 10
        THROW 50010, N'Không tạo đủ 10 Topping.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.BranchTopping BT
        INNER JOIN dbo.Topping T ON T.Id = BT.ToppingId
        INNER JOIN @ToppingMenu TM ON TM.ToppingName = T.Name
    ) <> 10 * @ActiveBranchCount
        THROW 50011, N'Không tạo đủ BranchTopping cho các branch đang hoạt động.', 1;

    COMMIT TRANSACTION;

    PRINT CONCAT(
        N'Seed menu hoàn tất: 62 sản phẩm, 186 mức giá, 10 topping; mở bán tại ',
        @ActiveBranchCount,
        N' branch đang hoạt động.'
    );
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

/* Kiểm tra sau khi seed. */
SELECT
    B.Code AS BranchCode,
    C.Name AS Category,
    P.Name AS ProductName,
    MAX(CASE WHEN S.Name = N'500ML' THEN PS.Price END) AS Price500ML,
    MAX(CASE WHEN S.Name = N'700ML' THEN PS.Price END) AS Price700ML,
    MAX(CASE WHEN S.Name = N'1000ML' THEN PS.Price END) AS Price1000ML,
    BP.IsAvailable,
    P.ImageUrl
FROM dbo.Product P
INNER JOIN dbo.Category C
    ON C.Id = P.CategoryId
INNER JOIN dbo.ProductSize PS
    ON PS.ProductId = P.Id
   AND PS.Deleted = 0
INNER JOIN dbo.Size S
    ON S.Id = PS.SizeId
   AND S.Deleted = 0
INNER JOIN dbo.BranchProduct BP
    ON BP.ProductId = P.Id
INNER JOIN dbo.Branch B
    ON B.Id = BP.BranchId
WHERE P.Deleted = 0
  AND C.Deleted = 0
GROUP BY B.Code, C.Name, P.Name, BP.IsAvailable, P.ImageUrl
ORDER BY B.Code, C.Name, P.Name;
GO

SELECT
    B.Code AS BranchCode,
    TG.Name AS ToppingGroup,
    T.Name AS ToppingName,
    T.Price AS UnitPrice,
    BT.IsAvailable,
    CASE
        WHEN T.Name = N'UP SIZE 1300ML' THEN N'Chọn tối đa 1 lần'
        ELSE N'Quantity 1/2/3 = 5K/10K/15K'
    END AS PricingRule
FROM dbo.Topping T
INNER JOIN dbo.ToppingGroup TG
    ON TG.Id = T.ToppingGroupId
INNER JOIN dbo.BranchTopping BT
    ON BT.ToppingId = T.Id
INNER JOIN dbo.Branch B
    ON B.Id = BT.BranchId
WHERE T.Deleted = 0
ORDER BY B.Code, TG.Name, T.Name;
GO
