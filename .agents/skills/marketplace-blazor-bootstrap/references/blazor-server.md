# Current Blazor Server shape

The web project uses the classic server-side hosting model:

```csharp
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
```

Keep these roles intact:

- `Pages/_Host.cshtml`: server host page and static asset links.
- `App.razor`: router and route fallback.
- `_Imports.razor`: shared Razor namespaces and component imports.
- `Shared/HomePageMaster.razor`: layout deriving from `LayoutComponentBase`.
- `Pages/*.razor`: routed pages.

Do not introduce `AddRazorComponents`, interactive render modes, `Routes.razor`, or per-component render modes unless the user explicitly requests migration.

For Bootstrap-only tasks:

- Use `container`, grid, flex, spacing, sizing, display, border, color, and responsive utility classes.
- Use Bootstrap Icons only when its stylesheet is already included.
- Avoid custom CSS, JavaScript, and C# event handlers.

