# TuanKiet BranchFlow

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![BranchFlow CI](https://github.com/khangba153/TuanKietBranchFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/khangba153/TuanKietBranchFlow/actions/workflows/ci.yml)

A beverage shop management application built as a personal graduation project, with a multi-branch data model, a Blazor Web interface, and an ASP.NET Core Web API.

[Live demo on Azure](https://branchflow-web-khang153-gphzgbakgdg0dndr.eastasia-01.azurewebsites.net) · [CI workflow](https://github.com/khangba153/TuanKietBranchFlow/actions/workflows/ci.yml)

Demo access is available on request from the author. Account passwords are not published in this repository.

> This is an actively developed portfolio project. The public deployment is a demo environment, not a production-ready system.

## Features

- **Access control:** HttpOnly cookie authentication on Web, JWT authentication on API, `OWNER`, `ADMIN`, and `EMPLOYEE` roles, and branch-scoped API access. Refresh-token rotation and account-wide logout revoke API sessions.
- **Employee management:** branch employee lists, search and status filters, profile viewing, creation, editing and work-status changes in Web; direct branch transfers or cross-admin transfer requests with destination acceptance and assignment history.
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

Files within each layer are grouped by feature. See the [folder organization guide](docs/architecture/folder-structure.md).

**Backend:** C#, .NET 10, ASP.NET Core Web API, EF Core Database First, SQL Server.

**Web:** Blazor Web App Interactive Server and Bootstrap.

**Delivery:** GitHub Actions CI, Azure App Service for Web/API, Azure SQL Database.

## Current Status

**Updated: 2026-11-16**

Menu management and employee ordering have been deployed to the Azure demo and manually smoke-tested by the author.

The auth update is deployed to Azure. Web uses a Secure, HttpOnly session cookie and keeps API tokens on the server, not in browser storage. API access tokens last 15 minutes; refresh tokens rotate within a fixed seven-day session, with only their hashes stored in SQL. Logout revokes all existing sessions of the same account; detected refresh-token reuse also triggers account-wide revocation.

The author manually smoke-tested login, F5/new tabs, cookie flags, WebSocket connectivity, refresh rotation, account-wide logout, and login rate limiting on Azure at the auth milestone. The current local Release test suite has 191 passing tests.

**Demo limits:** Web token storage and rate-limit counters are in process memory. The deployment uses one instance; Web restart/deploy requires signing in again. There is no distributed session store, and backup restoration has not yet been rehearsed.

Deployment is manual. GitHub Actions performs restore, Release build, and automated tests; automatic deployment is not enabled.

B4.1 is implemented locally: read-only administrator/owner order lists with branch/date/code/status filters, pagination, server-side summary metrics, mobile order cards, and snapshot-based details. ADMIN is limited to currently assigned branches; OWNER can read all non-deleted branches. OWNER/ADMIN browser smoke tests at 320/390/768/1440px and local SQL summary checks passed. This update has not been deployed; remaining acceptance cases are tracked in the [B4.1 implementation and acceptance checklist](docs/features/order-management-b4.1.md).

Closeout checks include SQL pagination with 25 temporary orders (rolled back), EMPLOYEE browser regression, HTTP role-boundary checks, and a [dependency security patch and audit](docs/security/package-audit-20261115.md). The current UI is accepted provisionally, not a finalized design.

B4.2a is implemented locally: assigned ADMINs can keep or cancel a NEEDS_REVIEW order with a required reason and atomic before/after history. SQL concurrency/rollback and real HTTP role checks passed; the author accepted the UI on 2026-11-16. See the [B4.2a report](docs/features/order-review-b4.2a.md).

B4.2b is implemented locally: ADMINs can adjust existing lines, quantity, size, toppings and notes with a server-calculated preview and atomic history. Changed sizes use current prices; unchanged sizes retain sale snapshots. Unit, SQL/HTTP and browser checks passed; see the [B4.2b report and limits](docs/features/order-adjustment-b4.2b.md). These updates have not been deployed. Inventory, payroll, reporting, and system-wide audit logging remain planned.

Employee profile editing is implemented and checked locally: ADMIN-only editing through the existing scoped PUT API, field validation, unsaved-change confirmation, error-preserving forms and mobile-friendly actions. Browser, local SQL and real HTTP role/status checks passed; see the [implementation and UX report](docs/features/employee-profile-edit.md). This update has not been deployed.

Employee work-status changes are implemented and checked locally: explicit confirmation, effective-date explanations, preserved assignment history, transactional session/token revocation and serialized status/login operations. Concurrent SQL, rollback, HTTP authorization and responsive browser checks passed; see the [work-status report](docs/features/employee-work-status.md). This update has not been deployed.

Employee branch transfers are implemented and checked locally: direct transfer for an ADMIN managing both branches, otherwise a pending request for destination acceptance, with rejection/withdrawal history. SQL concurrency/rollback, status-transfer races, order branch rechecks, HTTP scope and mobile/desktop browser checks passed; see the [transfer workflow and report](docs/features/employee-transfer-workflow.md). Apply SQL scripts 006/007 before deploying this version; cloud schema and deployment have not been changed.

Employee closeout hardening is implemented locally: profile editing shares the account transaction lock with status/transfers, tracks only edited properties, create/edit validation is aligned, duplicate writes return conflict, and inactive branches cannot create/reactivate employees. SQL/HTTP regression checks passed; create-form interaction/mobile re-verification remains pending because browser access was unavailable. See the [audit and remediation report](docs/features/employee-module-closeout-audit.md).

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

The baseline does not include `AuthSession` and `RefreshToken`, which are required for login. Their versioned schema is in [004-add-auth-session-refresh-token.sql](database/azure/004-add-auth-session-refresh-token.sql). For local setup, review a separate copy, change its database-name guard to your local `BranchFlowDB`, and run it only against that local database after the baseline. It deliberately refuses to overwrite existing auth tables.

[005-grant-auth-runtime.sql](database/azure/005-grant-auth-runtime.sql) grants the Azure demo API database user `SELECT`, `INSERT`, and `UPDATE` on these tables. Both scripts target `BranchFlowDB-Demo` as committed; do not run them unchanged for local setup or assume the same runtime database user exists locally. Local account/data provisioning is not yet automated.

Login also requires an active account with a password hash generated using the API's `PasswordHasher<AppUser>`. Employee menu access requires valid branch assignment and menu availability data.

### 3. Configure API secrets

Supply your local connection string and signing key through .NET User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:BranchFlowDatabase" "<local-connection-string>" --project TuanKietBranchFlow.Api
dotnet user-secrets set "Jwt:Key" "<strong-random-signing-key>" --project TuanKietBranchFlow.Api
dotnet user-secrets set "Jwt:ExpireMinutes" "15" --project TuanKietBranchFlow.Api
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
