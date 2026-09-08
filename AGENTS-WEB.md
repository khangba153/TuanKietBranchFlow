# BranchFlow Blazor Web App rules

- Preserve .NET 10 Blazor Web App with `AddRazorComponents().AddInteractiveServerComponents()` and `MapRazorComponents<App>().AddInteractiveServerRenderMode()`.
- Inspect `Components/App.razor`, `Components/Routes.razor`, imports, layout and the page's render mode. Registering Interactive Server does not make every component interactive automatically.
- Do not introduce classic `_Host.cshtml`, `AddServerSideBlazor` or `MapBlazorHub` from BE_MARKET. Router is in `Components/Routes.razor`; layouts derive from `LayoutComponentBase`.
- Prefer Bootstrap markup and utility classes before adding scoped CSS.
- Keep layout/navigation in `Components/Layout` and routed pages in `Components/Pages` following the existing structure.
- Do not add JavaScript or C# event handlers when the user asks for static Bootstrap-only markup.
- Check desktop and mobile layout behavior after changing headers, navigation, forms, or grids.
- UI calls API through a Web HTTP client layer; never inject Application services or repositories to bypass API authentication.
- Build a small functional slice with loading, empty, validation and failure states. Do not implement missing backend features during a UI-only lesson without permission.
- Protect per-user authentication state; test F5, logout, expired token and two browser sessions. Client-side role display never replaces API authorization.
- Before deployment, trace browser-to-Web and Web-to-API separately; verify HTTPS, environment-specific base URL and Interactive Server reconnect/WebSocket requirements.

