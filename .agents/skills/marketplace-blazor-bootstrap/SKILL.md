---
name: marketplace-blazor-bootstrap
description: Build or fix UI in BE_MarketPlace.Web using the existing classic Blazor Server hosting model, Razor components, Bootstrap, and Bootstrap Icons. Use for layouts, headers, navigation, pages, responsive grids, forms, or Razor compile errors. Do not use for API or EF Core-only work.
---

# Marketplace Blazor Bootstrap

Create responsive marketplace UI without accidentally migrating the project to the newer Blazor Web App model.

## Workflow

1. Read `references/blazor-server.md`.
2. Inspect `Program.cs`, `App.razor`, `_Imports.razor`, `_Host.cshtml`, the affected page, and its layout.
3. Confirm the requested component role:
   - Layout: inherit `LayoutComponentBase` and render `@Body`.
   - Page: declare `@page` and use the intended layout.
   - Shared component: keep it independent of routing.
4. Build structure with semantic HTML and Bootstrap utilities first.
5. Use Bootstrap responsive breakpoints and avoid fixed dimensions unless the reference layout requires them.
6. If the user says Bootstrap only, add no JavaScript, C# event handler, or custom CSS.
7. Build `BE_MarketPlace.Web/BE_MarketPlace.Web.csproj`.

## Quality checks

- Desktop and mobile layouts remain usable.
- Navigation and form controls have accessible labels.
- The layout contains exactly one appropriate `@Body`.
- No interactive behavior is implied by static `href="#"` controls.
- Vietnamese source text remains valid UTF-8.

