# Infrastructure rules

- Treat `Models/*.cs` and `Data/BranchFlowDbContext.cs` as generated Database First artifacts.
- Keep repository interfaces and implementations in separate files using the existing namespace.
- Repository interfaces extend `IRepositoryBase<TEntity>` when a repository abstraction is required.
- Repository implementations extend `RepositoryBase<TEntity>` and receive `BranchFlowDbContext` through the constructor.
- Put entity-specific queries in the matching repository rather than in `RepositoryBase<T>`.
- Use the same scoped DbContext instance across repositories and Unit of Work.
- Keep transaction ownership and `SaveChangesAsync` in Unit of Work for multi-repository workflows.
- Do not call `SaveChangesAsync` inside a repository method unless that method's contract explicitly owns the full operation.
- Inject specific repositories and IUnitOfWork separately as the current services do; do not add every table as a UnitOfWork property just to match BE_MARKET.
- Version database changes as reviewed SQL scripts, then scaffold locally. Never run a destructive baseline/reset or a test against production by default.

