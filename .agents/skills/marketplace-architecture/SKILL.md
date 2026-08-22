---
name: marketplace-architecture
description: Implement, fix, or refactor BE_MarketPlace backend features across API, Application, and Infrastructure while preserving layer boundaries, repository and Unit of Work patterns, and dependency injection. Use for controllers, services, DTOs, helpers, repositories, EF Core queries, UnitOfWork, or Program.cs changes. Do not use for Blazor-only visual work.
---

# Marketplace Architecture

Follow the existing four-project architecture and make the smallest coherent cross-layer change.

## Workflow

1. Read `references/architecture.md`.
2. Inspect the requested files, their interfaces, entity key types, project references, and DI registrations.
3. Map each responsibility to its owning layer before editing.
4. Implement from the innermost dependency outward:
   - Infrastructure data access.
   - Application DTOs and service workflow.
   - API controller and DI composition.
5. Keep controllers thin and keep DbContext access inside Infrastructure.
6. For multi-entity writes, stage repository operations and save once through Unit of Work.
7. Add concise Vietnamese comments above new or modified methods and important business-logic blocks.
8. Prefer beginner-friendly code that a second-year student can explain: explicit variables, sequential steps, and basic LINQ.
9. Build every affected project. Use `$marketplace-build` for repository-wide verification.

## Guardrails

- Do not regenerate or casually edit EF models or `CyberSoftMarketPlaceDbContext`.
- Do not move HTTP concerns into Application or persistence concerns into controllers.
- Match the repository abstraction style already present for the affected entity.
- Confirm async return types from interface through controller; never assign the result of `Task` as if it returned a value.
- Update DI when a constructor or abstraction changes.
- Write Vietnamese comments in UTF-8. Explain method responsibilities and non-obvious decisions briefly; do not comment every line or restate obvious syntax.
- Follow the readability level of the current `ProductService.cs`. Avoid records, tuples, complex dictionary lookups, advanced LINQ, reflection, metaprogramming, and unnecessary abstractions when straightforward code is sufficient.
- Keep the flow visible as `Controller -> Service -> Repository/UnitOfWork -> DbContext`. If an advanced technique is necessary, explain it briefly in Vietnamese.
- Preserve existing user changes and avoid unrelated cleanup.

