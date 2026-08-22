---
name: marketplace-review
description: Explicit code review command for BE_MarketPlace changes or selected files. Use when the user asks for review, audit, architecture checks, security checks, or a prioritized list of current problems. Review only unless the user also asks for fixes.
---

# Marketplace Review

Review like a project owner and lead with concrete findings rather than a general summary.

## Workflow

1. Read `references/review-checklist.md`.
2. Determine the requested scope. If no diff exists, inspect the named files or current project area.
3. Trace each behavior across relevant boundaries rather than reviewing one file in isolation.
4. Run targeted builds or `$marketplace-build` when compile status affects a finding.
5. Report findings ordered by severity with file and line evidence.
6. Include open questions and unverified runtime assumptions after findings.
7. If no material findings exist, state that clearly and describe the validation performed.

## Boundaries

- Do not edit files during a review-only request.
- Avoid style-only comments unless they hide a correctness or maintenance risk.
- Do not claim runtime behavior was tested when only compilation was verified.
- Prefer a small number of high-confidence findings over speculative noise.

