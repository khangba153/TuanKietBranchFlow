param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
# Build theo thứ tự phụ thuộc của bốn project BranchFlow.
$projects = @(
    "TuanKietBranchFlow.Infrastructure\TuanKietBranchFlow.Infrastructure.csproj",
    "TuanKietBranchFlow.Application\TuanKietBranchFlow.Application.csproj",
    "TuanKietBranchFlow.Api\TuanKietBranchFlow.Api.csproj",
    "TuanKietBranchFlow.Web\TuanKietBranchFlow.Web.csproj"
)

Push-Location $repositoryRoot

try {
    foreach ($project in $projects) {
        $arguments = @("build", $project, "--configuration", $Configuration, "--nologo")

        if ($NoRestore) {
            $arguments += "--no-restore"
        }

        if ($project -like "TuanKietBranchFlow.Web\*") {
            # Xuất Web vào thư mục tạm để tránh đụng file của server đang chạy.
            $verificationOutput = Join-Path $env:TEMP "TuanKietBranchFlow.Web-build-$PID"
            $arguments += @("--output", $verificationOutput)
            $arguments += "-p:UseAppHost=false"
        }

        Write-Host "Building $project"
        & dotnet @arguments

        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $project with exit code $LASTEXITCODE."
        }
    }

    Write-Host "All TuanKietBranchFlow projects built successfully."
}
finally {
    Pop-Location
}
