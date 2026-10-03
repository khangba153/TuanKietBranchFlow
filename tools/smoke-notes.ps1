param([string]$ApiBase = 'http://localhost:5007')

$ErrorActionPreference = 'Stop'
# Chỉ chạy trên API và SQL local; không in hoặc lưu khóa/token vào log
if ($ApiBase -ne 'http://localhost:5007') { throw 'Chỉ cho phép API local đã xác minh.' }
$secretPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/d4b78eec-ff78-437d-bd9e-a311d86b1622/secrets.json'
$settings = Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
$config = Get-Content (Join-Path $PSScriptRoot '../TuanKietBranchFlow.Api/appsettings.json') -Raw | ConvertFrom-Json
$issuer = $settings.'Jwt:Issuer'
$audience = $settings.'Jwt:Audience'
if (-not $issuer) { $issuer = $config.Jwt.Issuer }
if (-not $audience) { $audience = $config.Jwt.Audience }
$connectionString = $settings.'ConnectionStrings:BranchFlowDatabase'
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
if ($builder.DataSource -ne '127.0.0.1,1433' -or $builder.InitialCatalog -ne 'BranchFlowDB') {
    throw 'Database không phải môi trường local đã xác minh.'
}

function Encode-Base64Url([byte[]]$Bytes) {
    return [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+','-').Replace('/','_')
}

# Token test ngắn hạn ký bằng cấu hình local, không thay đổi tài khoản hoặc auth
function New-LocalTestToken([string]$Role, [int]$UserId = 1) {
    $payload = @{
        iss = $issuer; aud = $audience
        exp = [DateTimeOffset]::UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier' = "$UserId"
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name' = 'note_smoke_test'
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' = $Role
        jti = [Guid]::NewGuid().ToString()
    } | ConvertTo-Json -Compress
    $header = Encode-Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
    $body = Encode-Base64Url ([Text.Encoding]::UTF8.GetBytes($payload))
    $signer = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($settings.'Jwt:Key'))
    try { $signature = Encode-Base64Url ($signer.ComputeHash([Text.Encoding]::UTF8.GetBytes("$header.$body"))) }
    finally { $signer.Dispose() }
    return "$header.$body.$signature"
}

$admin = New-LocalTestToken 'ADMIN'
$owner = New-LocalTestToken 'OWNER'
$employee = New-LocalTestToken 'EMPLOYEE'
$sql = [System.Data.SqlClient.SqlConnection]::new($connectionString)
try {
    $sql.Open()
    $command = $sql.CreateCommand()
    $command.CommandText = "SELECT TOP(1) ub.UserId, ub.BranchId FROM dbo.UserBranch ub JOIN dbo.AppUser u ON u.Id=ub.UserId JOIN dbo.Role r ON r.Id=u.RoleId JOIN dbo.Branch b ON b.Id=ub.BranchId WHERE r.Code='EMPLOYEE' AND u.IsActive=1 AND u.Deleted=0 AND b.Deleted=0 AND ub.ActiveFrom<=CAST(GETDATE() AS date) AND (ub.ActiveTo IS NULL OR ub.ActiveTo>=CAST(GETDATE() AS date)) ORDER BY ub.Id"
    $reader = $command.ExecuteReader()
    if (-not $reader.Read()) { throw 'Cần EMPLOYEE local có phân công để kiểm tra menu.' }
    $employeeUserId = $reader.GetInt32(0)
    $employeeBranchId = $reader.GetInt32(1)
    $reader.Close()
}
finally { $sql.Dispose() }
$assignedEmployee = New-LocalTestToken 'EMPLOYEE' $employeeUserId
$script:checks = 0
function Send-TestRequest([string]$Method, [string]$Route, [string]$Token, $Body, [int]$Expected) {
    $parameters = @{ Uri = "$ApiBase/$Route"; Method = $Method; SkipHttpErrorCheck = $true }
    if ($Token) { $parameters.Headers = @{ Authorization = "Bearer $Token" } }
    if ($null -ne $Body) { $parameters.Body = $Body | ConvertTo-Json -Compress; $parameters.ContentType = 'application/json; charset=utf-8' }
    $response = Invoke-WebRequest @parameters
    if ([int]$response.StatusCode -ne $Expected) {
        throw "$Method $Route expected $Expected, received $($response.StatusCode)"
    }
    $script:checks++
    if ($response.Content) { return $response.Content | ConvertFrom-Json }
}

$groupIds = [Collections.Generic.List[int]]::new()
$optionIds = [Collections.Generic.List[int]]::new()
$suffix = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,6)
try {
    $null = Send-TestRequest GET 'api/note-groups' '' $null 401
    $null = Send-TestRequest GET 'api/note-groups' $employee $null 403
    $null = Send-TestRequest GET 'api/note-groups' $owner $null 200
    $null = Send-TestRequest POST 'api/note-groups' $owner @{ Name='Denied' } 403
    $null = Send-TestRequest POST 'api/note-options' $employee @{ NoteGroupId=1; Name='Denied' } 403
    $null = Send-TestRequest POST 'api/note-groups' $admin @{ Name='   ' } 400
    $null = Send-TestRequest POST 'api/note-options' $admin @{ NoteGroupId=0; Name='Invalid' } 400

    $group = Send-TestRequest POST 'api/note-groups' $admin @{ Name="  Smoke Notes $suffix  " } 201
    $groupIds.Add($group.id)
    if ($group.name -ne "Smoke Notes $suffix") { throw 'Tên nhóm chưa Trim.' }
    $null = Send-TestRequest POST 'api/note-groups' $admin @{ Name=$group.name } 409
    $second = Send-TestRequest POST 'api/note-groups' $admin @{ Name="Smoke Notes Other $suffix" } 201
    $groupIds.Add($second.id)

    $option = Send-TestRequest POST 'api/note-options' $admin @{ NoteGroupId=$group.id; Name='  Lựa chọn thử  ' } 201
    $optionIds.Add($option.id)
    if ($option.name -ne 'Lựa chọn thử') { throw 'Tên lựa chọn chưa Trim.' }
    $menu = Send-TestRequest GET "api/order-menu?branchId=$employeeBranchId" $assignedEmployee $null 200
    if (-not ($menu.noteGroups | Where-Object id -eq $group.id)) { throw 'Nhóm mới hoạt động không hiện ở EMPLOYEE.' }
    $null = Send-TestRequest POST 'api/note-options' $admin @{ NoteGroupId=$group.id; Name='Lựa chọn thử' } 409
    $other = Send-TestRequest POST 'api/note-options' $admin @{ NoteGroupId=$second.id; Name='Lựa chọn thử' } 201
    $optionIds.Add($other.id)
    $null = Send-TestRequest PUT "api/note-options/$($option.id)" $admin @{ Name='Lựa chọn thử' } 400
    $updated = Send-TestRequest PUT "api/note-options/$($option.id)" $admin @{ Name='  Lựa chọn đã sửa  '; IsActive=$false } 200
    if ($updated.isActive -or $updated.name -ne 'Lựa chọn đã sửa') { throw 'Cập nhật lựa chọn sai.' }
    $menu = Send-TestRequest GET "api/order-menu?branchId=$employeeBranchId" $assignedEmployee $null 200
    if (($menu.noteGroups | Where-Object id -eq $group.id).options | Where-Object id -eq $option.id) { throw 'EMPLOYEE vẫn thấy lựa chọn tạm ngừng.' }
    $null = Send-TestRequest PUT "api/note-options/$($option.id)" $admin @{ Name='Lựa chọn đã sửa'; IsActive=$true } 200
    $null = Send-TestRequest PUT "api/note-options/$($option.id)" $owner @{ Name='Denied'; IsActive=$true } 403
    $null = Send-TestRequest DELETE "api/note-options/$($option.id)" $owner $null 403
    $updatedGroup = Send-TestRequest PUT "api/note-groups/$($group.id)" $admin @{ Name=$group.name; IsActive=$false } 200
    if ($updatedGroup.isActive -or $updatedGroup.noteOptions.Count -ne 1) { throw 'Cập nhật nhóm làm mất lựa chọn.' }
    $menu = Send-TestRequest GET "api/order-menu?branchId=$employeeBranchId" $assignedEmployee $null 200
    if ($menu.noteGroups | Where-Object id -eq $group.id) { throw 'EMPLOYEE vẫn thấy nhóm tạm ngừng.' }
    $null = Send-TestRequest PUT "api/note-groups/$($group.id)" $admin @{ Name=$group.name; IsActive=$true } 200
    $menu = Send-TestRequest GET "api/order-menu?branchId=$employeeBranchId" $assignedEmployee $null 200
    if (-not ($menu.noteGroups | Where-Object id -eq $group.id)) { throw 'EMPLOYEE không thấy nhóm mở lại.' }

    $null = Send-TestRequest DELETE "api/note-options/$($option.id)" $admin $null 204
    $null = Send-TestRequest DELETE "api/note-options/$($option.id)" $admin $null 404
    $groups = Send-TestRequest GET 'api/note-groups' $admin $null 200
    if (($groups | Where-Object id -eq $group.id).noteOptions.Count -ne 0) { throw 'Lựa chọn đã xóa vẫn còn trong GET.' }
    $null = Send-TestRequest DELETE "api/note-groups/$($second.id)" $admin $null 204
    $null = Send-TestRequest PUT "api/note-options/$($other.id)" $admin @{Name='Unavailable';IsActive=$true} 404
    $null = Send-TestRequest DELETE "api/note-options/$($other.id)" $admin $null 404
    $null = Send-TestRequest POST 'api/note-options' $admin @{NoteGroupId=$second.id;Name='Unavailable'} 404
    $null = Send-TestRequest DELETE "api/note-groups/$($second.id)" $admin $null 404

    # Kiểm chứng xóa mềm và nhóm đã xóa không làm xóa lựa chọn con
    $sql = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    try {
        $sql.Open()
        $command = $sql.CreateCommand()
        $command.CommandText = 'SELECT Deleted, UpdatedAt FROM dbo.NoteOption WHERE Id=@Id'
        $null = $command.Parameters.AddWithValue('@Id', $option.id)
        $reader = $command.ExecuteReader()
        if (-not $reader.Read() -or -not $reader.GetBoolean(0) -or $reader.IsDBNull(1)) { throw 'Không giữ bản ghi xóa mềm.' }
        $reader.Close()
        $command.Parameters['@Id'].Value = $other.id
        $reader = $command.ExecuteReader()
        if (-not $reader.Read() -or $reader.GetBoolean(0)) { throw 'Xóa nhóm đã thay đổi Deleted của lựa chọn con.' }
        $reader.Close()
    }
    finally { $sql.Dispose() }
    Write-Output "PASS: $script:checks API checks + SQL soft-delete checks (local only)."
}
finally {
    # Chỉ xóa mềm các nhóm thử do script vừa tạo; không đụng dữ liệu sẵn có
    foreach ($id in $groupIds) {
        $null = Invoke-WebRequest -Uri "$ApiBase/api/note-groups/$id" -Method Delete -Headers @{ Authorization="Bearer $admin" } -SkipHttpErrorCheck
    }
    $admin = $null; $owner = $null; $employee = $null; $settings = $null
}
