# BE Marketplace architecture

## Dependency direction

```text
BE_MarketPlace.Api --------> BE_MarketPlace.Application
        |                              |
        +------------------------------+--> BE_MarketPlace.Infrastructure

BE_MarketPlace.Web is currently a separate classic Blazor Server host.
```

Infrastructure must not reference Application or API. Application must not reference API.

## Layer ownership

### Infrastructure

- `Data/CyberSoftMarketPlaceDbContext.cs`: EF Core context.
- `Models/*.cs`: database entities and views; treat as generated.
- `Repositories/IRepositoryBase.cs` and `RepositoryBase.cs`: common operations.
- Entity repositories: entity-specific queries.
- `UnitofWork/UnitOfWork.cs`: shared context, transactions, and saving.

### Application

- `DTO`: API-safe request and response contracts.
- `Constant`: stable domain and response constants.
- `Helper`: stateless reusable transformations such as normalization and hashing.
- `Services`: business workflows coordinating repositories and Unit of Work.

### API

- `Controller`: routes, model binding, auth metadata, and status-code mapping.
- `Program.cs`: DbContext, repository, service, authentication, Redis, Swagger, CORS, and middleware registration.

### Web

- Classic Blazor Server host using Razor Pages and SignalR.
- UI concerns belong here; do not place persistence access directly in Razor markup.

## Feature path

For a typical write endpoint:

```text
HTTP request
  -> Controller
  -> Application service
  -> Repository or repositories
  -> UnitOfWork.SaveChangesAsync()
  -> Application response DTO
  -> HTTP status and response body
```

Check that interface signatures and concrete implementations agree at every boundary.

