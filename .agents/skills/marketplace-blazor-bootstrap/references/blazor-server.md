# Current BranchFlow Blazor shape

BranchFlow uses .NET 10 Blazor Web App with Interactive Server support registered by AddRazorComponents/AddInteractiveServerComponents and MapRazorComponents/AddInteractiveServerRenderMode.

## Files to inspect

- `TuanKietBranchFlow.Web/Program.cs`: hosting, DI and middleware.
- `Components/App.razor`: document root, styles and blazor.web.js.
- `Components/Routes.razor`: router and default layout.
- `Components/_Imports.razor`: component imports.
- `Components/Layout`: shared layout and navigation.
- `Components/Pages`: routed pages; inspect per-page/global render modes before adding handlers.

Do not add `_Host.cshtml`, classic AddServerSideBlazor or a separate MapBlazorHub pipeline. BE_MARKET is only a reference for learning, not this app's hosting template.

## API integration and deployment

Build API calls in the Web layer, reuse appropriate DTOs and preserve per-user state. Teach auth storage choices and server-side rendering lifecycle before copying old LocalStorage code. Do not use a singleton token/client state that could mix users.

Trace browser -> Web host and Web host -> API host separately. Configure API base URL per environment. Show loading, empty, invalid input, 401/403 and recoverable API errors. Verify F5, logout and reconnect in interactive flows.

For public hosting, check runtime support, HTTPS, WebSockets/session affinity requirements for the chosen host, and safe error display. Adding Interactive Server support alone does not make a static page interactive.

For Bootstrap-only tasks, use grid/flex/utilities and semantic accessible HTML. Use Bootstrap Icons only if its stylesheet is present; do not add custom CSS, JavaScript or C# handlers under that constraint.
