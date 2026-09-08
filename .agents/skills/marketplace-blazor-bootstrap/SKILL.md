---
name: marketplace-blazor-bootstrap
description: Build or fix TuanKietBranchFlow.Web .NET 10 Blazor Web App Interactive Server pages, layouts and forms using Bootstrap. Respect guided-learning steps and Bootstrap-only requests. Do not use for API or EF Core-only work, and do not copy classic BE_MARKET hosting.
---

# BranchFlow Blazor Bootstrap (legacy skill name)

Create responsive BranchFlow UI in its existing Blazor Web App model, one requested learning step at a time.

## Workflow

1. Read `references/blazor-server.md`.
2. Read root AGENTS-WEB.md and CURRENT_STEP; inspect `Program.cs`, `Components/App.razor`, `Components/Routes.razor`, imports, page render mode and layout.
3. Confirm the requested component role:
   - Layout: inherit `LayoutComponentBase` and render `@Body`.
   - Page: declare `@page` and use the intended layout.
   - Shared component: keep it independent of routing.
4. Build structure with semantic HTML and Bootstrap utilities first.
5. Use Bootstrap responsive breakpoints and avoid fixed dimensions unless the reference layout requires them.
6. If the user says Bootstrap only, add no JavaScript, C# event handler, or custom CSS.
7. Build `TuanKietBranchFlow.Web/TuanKietBranchFlow.Web.csproj` after code changes. Do not complete missing API features during a UI-only task.

## Quality checks

- Desktop and mobile layouts remain usable.
- Navigation and form controls have accessible labels.
- The layout contains exactly one appropriate `@Body`.
- No interactive behavior is implied by static `href="#"` controls.
- Vietnamese source text remains valid UTF-8.
