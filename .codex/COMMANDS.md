# Project commands

Codex custom prompts are deprecated and are not repository-scoped. This project therefore exposes shared commands as explicitly invokable skills:

| Command | Purpose |
| --- | --- |
| `$marketplace-build` | Build all four projects in dependency order and summarize failures. |
| `$marketplace-review` | Review current code or changes for correctness, security, architecture, and missing verification. |
| `$marketplace-architecture` | Implement or refactor a backend feature in the correct project layers. |
| `$marketplace-blazor-bootstrap` | Build or adjust classic Blazor Server UI with Bootstrap conventions. |

Examples:

```text
$marketplace-build
$marketplace-review focus=BE_MarketPlace.Application/Services/UserService.cs
$marketplace-architecture add a product creation endpoint
$marketplace-blazor-bootstrap create a responsive marketplace header
```

