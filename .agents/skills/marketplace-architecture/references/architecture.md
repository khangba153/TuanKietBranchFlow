# BranchFlow architecture

The skill keeps its original marketplace name for existing invocations, but these instructions belong to TuanKietBranchFlow, not BE_MARKET.

## Dependencies and responsibilities

- API references Application and Infrastructure; Application references Infrastructure. Never add the reverse dependency.
- Web references Application for DTOs and calls HTTP APIs; it does not call application services or repositories directly.
- `TuanKietBranchFlow.Infrastructure/Data/BranchFlowDbContext.cs` and `Models` are Database First generated files.
- `Infrastructure/Repositories` owns queries; `Infrastructure/UnitOfWork` owns saving. Services inject specific repositories and IUnitOfWork separately, all sharing a scoped DbContext.
- `Application/DTOs` contains request, response and internal service-result contracts. `Application/Services` owns business workflows and mapping.
- `Api/Controllers` owns routes, binding, HTTP validation and status mapping. `Api/Program.cs` composes DI, authentication, authorization and Swagger.
- `Web/Components` contains .NET 10 Blazor Web App pages and layouts with Interactive Server support.

## Runtime flow

HTTP request -> authentication/authorization -> controller -> service -> repository queries -> entity changes -> UnitOfWork.SaveChangesAsync -> service maps DTO -> controller result -> ASP.NET Core JSON response.

For read-only endpoints, no save is needed. One SaveChanges call on the shared context is preferred for changes that must succeed together; explicit multi-save transactions require a real business need.

## Business and learning boundaries

- OWNER reads across branches but does not create/update employees. ADMIN writes only within assigned scope. Validate identity from JWT and requested branch scope separately.
- Preserve employee assignment history and confirm date semantics before implementing transitions.
- Keep the full method understandable, with Vietnamese comments and explicit intermediate variables. Do not implement another layer when only asked to explain/check.
- See root AGENTS.md, PROJECT_CONTEXT.md and docs/learning/CURRENT_STEP.md for the latest decisions; historical prototypes do not override them.
