param(
    [switch]$Apply,
    [switch]$RollbackTest,
    [switch]$ConfigureBranches
)

$ErrorActionPreference = 'Stop'

if ($Apply -and $RollbackTest) {
    throw 'Chỉ chọn Apply hoặc RollbackTest.'
}

$dataPath = Join-Path $PSScriptRoot 'shop-inventory.json'
$data = Get-Content -LiteralPath $dataPath -Raw -Encoding UTF8 | ConvertFrom-Json
$items = @($data.items)

if ($items.Count -ne 46) {
    throw 'Bộ seed phải giữ đủ 46 dòng kiểm kho gốc.'
}
if (@($items.name | Sort-Object -Unique).Count -ne $items.Count) {
    throw 'Tên nguyên liệu mẫu bị trùng.'
}

foreach ($item in $items) {
    if ([string]::IsNullOrWhiteSpace($item.name) -or $item.name.Length -gt 150 -or
        [string]::IsNullOrWhiteSpace($item.unit) -or $item.unit.Length -gt 50) {
        throw "Tên hoặc đơn vị không hợp lệ tại dòng $($item.id)."
    }
    if ($null -ne $item.referenceQuantity -and [decimal]$item.referenceQuantity -lt 0) {
        throw "Số kiểm đếm âm tại dòng $($item.id)."
    }
    if (-not $data.sources.PSObject.Properties[$item.source]) {
        throw "Thiếu nguồn tham khảo tại dòng $($item.id)."
    }
}

# Mặc định chỉ xem trước: không mở kết nối database.
if (-not $Apply -and -not $RollbackTest) {
    $items | Select-Object id, originalName, name, unit, referenceQuantity | Format-Table -AutoSize -Wrap
    Write-Output "Xem trước: $($items.Count) mặt hàng, $(@($items.unit | Sort-Object -Unique).Count) đơn vị. Chưa ghi database."
    return
}

# Chỉ đọc cấu hình local hiện có; không nhận connection string cloud từ tham số.
$secretPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/d4b78eec-ff78-437d-bd9e-a311d86b1622/secrets.json'
$settings = Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($settings.'ConnectionStrings:BranchFlowDatabase')
if ($builder.DataSource -ne '127.0.0.1,1433' -or $builder.InitialCatalog -ne 'BranchFlowDB') {
    throw 'Chỉ cho phép SQL local 127.0.0.1,1433 / BranchFlowDB. Không chạy Azure.'
}

$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
$transaction = $null
try {
    $connection.Open()
    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
    $lockCommand = $connection.CreateCommand()
    $lockCommand.Transaction = $transaction
    $lockCommand.CommandText = @'
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource=N'BranchFlow.InventoryConfiguration',
    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;
IF @result < 0 THROW 51001, N'Inventory configuration is busy.', 1;
'@
    [void]$lockCommand.ExecuteNonQuery()
    $lockCommand.Dispose()

    $passes = 1
    if ($RollbackTest) {
        $passes = 2
    }

    for ($pass = 1; $pass -le $passes; $pass++) {
        $addedUnits = 0
        $addedIngredients = 0
        $addedConfigurations = 0
        foreach ($item in $items) {
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            # Không sửa tên, đơn vị, trạng thái hoặc dữ liệu có sẵn; mismatch dừng cả batch.
            # Cấu hình tùy chọn chỉ thêm hàng kho mới Quantity=0, không sửa hàng đã có.
            $command.CommandText = @'
DECLARE @unitId int, @ingredientId int, @unitAdded int=0, @ingredientAdded int=0,
    @configurationAdded int=0;
SELECT @unitId=Id FROM dbo.IngredientUnit WITH (UPDLOCK,HOLDLOCK)
WHERE Name=@unit AND Deleted=0;
IF @unitId IS NULL
BEGIN
    INSERT dbo.IngredientUnit(Name,IsActive,Deleted,CreatedAt)
    VALUES(@unit,1,0,SYSUTCDATETIME());
    SET @unitId=CONVERT(int,SCOPE_IDENTITY());
    SET @unitAdded=1;
END;
IF EXISTS(SELECT 1 FROM dbo.IngredientUnit WHERE Id=@unitId AND IsActive=0)
    THROW 51002, N'Existing unit is inactive; review instead of changing it.', 1;

SELECT @ingredientId=Id FROM dbo.Ingredient WITH (UPDLOCK,HOLDLOCK)
WHERE Name=@name AND Deleted=0;
IF @ingredientId IS NULL
BEGIN
    INSERT dbo.Ingredient(Name,UnitId,IsActive,Deleted,CreatedAt)
    VALUES(@name,@unitId,1,0,SYSUTCDATETIME());
    SET @ingredientId=CONVERT(int,SCOPE_IDENTITY());
    SET @ingredientAdded=1;
END
ELSE IF EXISTS(SELECT 1 FROM dbo.Ingredient WHERE Id=@ingredientId
    AND (UnitId<>@unitId OR IsActive=0))
    THROW 51003, N'Existing ingredient has another unit or status; review manually.', 1;
IF @configure=1
BEGIN
    IF NOT EXISTS(SELECT 1 FROM dbo.Branch WHERE Deleted=0 AND IsActive=1)
        THROW 51004, N'No active local branches to configure.', 1;
    INSERT dbo.BranchIngredient(BranchId,IngredientId,Quantity,WarningThreshold,UpdatedAt)
    SELECT branch.Id,@ingredientId,0,@threshold,SYSUTCDATETIME()
    FROM dbo.Branch branch WITH (UPDLOCK,HOLDLOCK)
    WHERE branch.Deleted=0 AND branch.IsActive=1
        AND NOT EXISTS(SELECT 1 FROM dbo.BranchIngredient WITH (UPDLOCK,HOLDLOCK)
            WHERE BranchId=branch.Id AND IngredientId=@ingredientId);
    SET @configurationAdded=@@ROWCOUNT;
END;
SELECT @unitAdded AS UnitAdded,@ingredientAdded AS IngredientAdded,
    @configurationAdded AS ConfigurationAdded;
'@
            [void]$command.Parameters.Add('@unit', [System.Data.SqlDbType]::NVarChar, 50)
            $command.Parameters['@unit'].Value = [string]$item.unit
            [void]$command.Parameters.Add('@name', [System.Data.SqlDbType]::NVarChar, 150)
            $command.Parameters['@name'].Value = [string]$item.name
            [void]$command.Parameters.Add('@configure', [System.Data.SqlDbType]::Bit)
            $command.Parameters['@configure'].Value = [bool]$ConfigureBranches
            # Ngưỡng demo: 20% số kiểm đếm cũ, làm tròn lên theo đơn vị đóng gói.
            # Thiếu số tham khảo thì dùng 2; không sử dụng số này làm Quantity.
            $threshold = [decimal]2
            if ($null -ne $item.referenceQuantity) {
                $threshold = [Math]::Max([decimal]1, [Math]::Ceiling([decimal]$item.referenceQuantity * [decimal]0.2))
            }
            $thresholdParameter = $command.Parameters.Add('@threshold', [System.Data.SqlDbType]::Decimal)
            $thresholdParameter.Precision = 18
            $thresholdParameter.Scale = 3
            $thresholdParameter.Value = $threshold
            $reader = $command.ExecuteReader()
            try {
                if (-not $reader.Read()) {
                    throw 'Không đọc được kết quả seed.'
                }
                $addedUnits += $reader.GetInt32(0)
                $addedIngredients += $reader.GetInt32(1)
                $addedConfigurations += $reader.GetInt32(2)
            }
            finally {
                $reader.Dispose()
                $command.Dispose()
            }
        }
        Write-Output "Lượt ${pass}: thêm $addedUnits đơn vị, $addedIngredients mặt hàng, $addedConfigurations cấu hình chi nhánh."
        if ($RollbackTest -and $pass -eq 2 -and ($addedUnits -ne 0 -or $addedIngredients -ne 0 -or $addedConfigurations -ne 0)) {
            throw 'Seed chạy lại không idempotent.'
        }
    }

    if ($RollbackTest) {
        $transaction.Rollback()
        Write-Output 'Đã rollback toàn bộ kiểm thử. Không lưu danh mục/tồn kho.'
    }
    else {
        $transaction.Commit()
        Write-Output 'Đã seed danh mục local. Không ghi số kiểm đếm lịch sử vào tồn hiện tại.'
    }
    $transaction.Dispose()
    $transaction = $null
}
finally {
    if ($null -ne $transaction) {
        $transaction.Rollback()
        $transaction.Dispose()
    }
    $connection.Dispose()
}
