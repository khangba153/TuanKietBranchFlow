# Blazor Server rules

- Preserve the classic Blazor Server pipeline: `AddRazorPages`, `AddServerSideBlazor`, `MapBlazorHub`, and fallback to `/_Host`.
- Use `App.razor` as the router root and Razor layouts deriving from `LayoutComponentBase`.
- Prefer Bootstrap markup and utility classes before adding scoped CSS.
- Keep shared navigation and page chrome in `Shared`, and page content in `Pages`.
- Do not add JavaScript or C# event handlers when the user asks for static Bootstrap-only markup.
- Check desktop and mobile layout behavior after changing headers, navigation, forms, or grids.


