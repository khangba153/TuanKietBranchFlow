# Infrastructure rules

- Treat `Models/*.cs` and `Data/CyberSoftMarketPlaceDbContext.cs` as generated database artifacts.
- Keep repository interfaces and implementations in separate files using the existing namespace.
- Repository interfaces extend `IRepositoryBase<TEntity>` when a repository abstraction is required.
- Repository implementations extend `RepositoryBase<TEntity>` and receive `CyberSoftMarketPlaceDbContext` through the constructor.
- Put entity-specific queries in the matching repository rather than in `RepositoryBase<T>`.
- Use the same scoped DbContext instance across repositories and Unit of Work.
- Keep transaction ownership and `SaveChangesAsync` in Unit of Work for multi-repository workflows.
- Do not call `SaveChangesAsync` inside a repository method unless that method's contract explicitly owns the full operation.


