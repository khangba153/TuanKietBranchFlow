param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
$projects = @(
    "BE_MarketPlace.Infrastructure\BE_MarketPlace.Infrastructure.csproj",
    "BE_MarketPlace.Application\BE_MarketPlace.Application.csproj",
    "BE_MarketPlace.Api\BE_MarketPlace.Api.csproj",
    "BE_MarketPlace.Web\BE_MarketPlace.Web.csproj"
)

Push-Location $repositoryRoot

try {
    foreach ($project in $projects) {
        $arguments = @("build", $project, "--configuration", $Configuration, "--nologo")

        if ($NoRestore) {
            $arguments += "--no-restore"
        }

        if ($project -like "BE_MarketPlace.Web\*") {
            # Avoid locked bin files while the Blazor development server is running.
            $verificationOutput = Join-Path $env:TEMP "BE_MarketPlace.Web-build-$PID"
            $arguments += @("--output", $verificationOutput)
            $arguments += "-p:UseAppHost=false"
        }

        Write-Host "Building $project"
        & dotnet @arguments

        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $project with exit code $LASTEXITCODE."
        }
    }

    Write-Host "All BE Marketplace projects built successfully."
}
finally {
    Pop-Location
}

