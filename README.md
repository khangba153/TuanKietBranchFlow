# TuanKiet BranchFlow

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![BranchFlow CI](https://github.com/khangba153/TuanKietBranchFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/khangba153/TuanKietBranchFlow/actions/workflows/ci.yml)

A beverage shop management application built as a personal graduation project, with a multi-branch data model, a Blazor Web interface, and an ASP.NET Core Web API.

[Live demo on Azure](https://branchflow-web-khang153-gphzgbakgdg0dndr.eastasia-01.azurewebsites.net) · [CI workflow](https://github.com/khangba153/TuanKietBranchFlow/actions/workflows/ci.yml)

Demo access is available on request from the author. Account passwords are not published in this repository.

> This is an actively developed portfolio project. The public deployment is a demo environment, not a production-ready system.

## Features

- **Access control:** JWT authentication, `OWNER`, `ADMIN`, and `EMPLOYEE` roles, and branch-scoped API access.
- **Employee management:** branch employee lists, search and status filters, profile viewing and creation in Web; API support for updates, status changes, and branch transfers with assignment history.
- **Menu management:** ADMIN can create, edit, and soft-delete categories, sizes, products and prices, toppings, and quick notes. OWNER has read-only access. Menu definitions and prices are shared; product and topping availability is branch-specific.
- **Employee ordering:** browse and filter the branch menu, choose sizes and options, manage a cart, and submit orders.
- **Order validation:** the API rechecks availability and prices, generates daily order codes, and saves order details and name/price snapshots in one transaction.
- **Order history:** employees can view their own orders and report an incorrect completed order for review.

The Web interface uses Bootstrap layouts for desktop and mobile.

## Architecture and Stack

A layered monolith with four application projects:

```text
Blazor component → Web API client → API controller
→ Application service → Repository / Unit of Work
→ EF Core DbContext → SQL Server
```

| Project | Responsibility |
|---|---|
| `TuanKietBranchFlow.Web` | Blazor Web App Interactive Server, Bootstrap UI, HTTP API clients |
| `TuanKietBranchFlow.Api` | Controllers, authentication, authorization, and dependency injection |
| `TuanKietBranchFlow.Application` | DTOs, validation, and business services |
| `TuanKietBranchFlow.Infrastructure` | Database First models, DbContext, repositories, and Unit of Work |
| `TuanKietBranchFlow.Tests` | xUnit and Moq tests |

**Backend:** C#, .NET 10, ASP.NET Core Web API, EF Core Database First, SQL Server.

**Web:** Blazor Web App Interactive Server and Bootstrap.

**Delivery:** GitHub Actions CI, Azure App Service for Web/API, Azure SQL Database.

## Current Status

**Updated: 2026-10-06**

Menu management and employee ordering have been deployed to the Azure demo and manually smoke-tested by the author.

The public demo currently uses JWT authentication with browser-side token storage. Refresh-token rotation and account-wide session revocation are implemented and manually tested in the local API. Web integration with HttpOnly cookies and server-side token storage is still in progress; this auth update has **not** been deployed to Azure.

Deployment is manual. GitHub Actions performs restore, Release build, and automated tests; automatic deployment is not enabled.

Next work: complete and deploy the auth update, then add administrator/owner order workflows. Inventory, payroll, reporting, and audit logging remain planned.

## Running Locally

### Prerequisites

- .NET 10 SDK
- SQL Server 2019+ (Docker is optional)
- Git

### 1. Clone and restore

```powershell
git clone https://github.com/khangba153/TuanKietBranchFlow.git
cd TuanKietBranchFlow
dotnet restore TuanKietBranchFlow.slnx
dotnet tool restore
```

### 2. Prepare the database

Run [database/BranchFlowDB.sql](database/BranchFlowDB.sql) against a local SQL Server to initialize `BranchFlowDB`. The script refuses to initialize an existing schema.

**Current setup limitation:** the baseline script does not yet include the new `AuthSession` and `RefreshToken` tables required by the local auth implementation. Versioned setup scripts for these tables and local test-account provisioning are still pending. A fresh database is therefore not yet sufficient to run the current login flow.

Login also requires an active account with a password hash generated using the API's `PasswordHasher<AppUser>`. Employee menu access requires valid branch assignment and menu availability data.

The tools under `tools/BranchFlow.DemoSeed` and `tools/BranchFlow.PasswordReset` target the Azure demo; they are **not local setup tools**. Do not run them to initialize a local database.

### 3. Configure API secrets

Supply your local connection string and signing key through .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:BranchFlowDatabase" "<local-connection-string>" --project TuanKietBranchFlow.Api
dotnet user-secrets set "Jwt:Key" "<strong-random-signing-key>" --project TuanKietBranchFlow.Api
```

Replace the placeholders locally. Do not commit credentials or use the Azure demo database for local testing.

The Web development API address is configured in [appsettings.Development.json](TuanKietBranchFlow.Web/appsettings.Development.json):

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5007/"
  }
}
```

### 4. Start API and Web

Once the database and account setup requirements above are satisfied, run these commands in separate terminals:

```powershell
dotnet run --project TuanKietBranchFlow.Api --launch-profile http
```

```powershell
dotnet run --project TuanKietBranchFlow.Web --launch-profile http
```

- Web: `http://localhost:5032`
- Swagger (Development only): `http://localhost:5007/swagger`
- Health: `http://localhost:5007/health` — checks the application, not database connectivity.

## Tests and CI

```powershell
dotnet build TuanKietBranchFlow.slnx --configuration Release
dotnet test TuanKietBranchFlow.slnx --configuration Release
```

xUnit and Moq tests cover selected service rules, including access checks, validation, menu pricing, order transactions, and Web token-service behavior. They do not provide complete database integration or end-to-end browser coverage.

The [CI workflow](.github/workflows/ci.yml) runs restore, Release build, and tests on an Ubuntu runner for pushes and pull requests to `main`.

## Documentation

- [Business requirements](docs/context/business-requirements-v1.5.md)
- [Database analysis](docs/context/database-schema-analysis.md)
- [API roadmap](docs/context/api-endpoint-roadmap-v0.3.md)
- [UI prototypes — planned designs, not deployment screenshots](docs/prototype-ui)

## Author

**Thanh Chi Khang Ba**

[GitHub](https://github.com/khangba153) · [LinkedIn](https://www.linkedin.com/in/thanh-chi-khang-ba-1871ba434/)
