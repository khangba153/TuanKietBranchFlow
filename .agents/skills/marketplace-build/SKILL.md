---
name: marketplace-build
description: Explicit repository command that builds Infrastructure, Application, API, and Web in dependency order, then reports actionable compiler failures and warnings. Use when the user asks to build, run compile checks, validate changes, or find current project errors.
---

# Marketplace Build

Validate the repository with a repeatable build order even though the root currently has no solution file.
The helper sends Web build output to a temporary directory and disables its Windows apphost so a running Blazor development server does not block compilation.

## Run

From the repository root:

```powershell
& ".agents/skills/marketplace-build/scripts/verify.ps1"
```

Use `-Configuration Release` when release validation is requested. Use `-NoRestore` only when packages are already restored and network-independent validation is required.

## Interpret results

- Stop at the first failed project because downstream failures may only be consequences.
- Report the project, file, line, error code, and shortest likely cause.
- If the user asked only for validation, do not edit source.
- If the user asked to fix errors, patch the root cause and rerun the full script.
- Separate compiler results from runtime dependencies such as SQL Server or Redis availability.

