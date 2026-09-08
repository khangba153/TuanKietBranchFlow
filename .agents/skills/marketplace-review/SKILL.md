---
name: marketplace-review
description: Explicit read-only review of TuanKietBranchFlow changes for correctness, security, architecture and verification gaps. For learner check requests, evaluate their answers and current step first. Do not implement fixes without a request to do so.
---

# BranchFlow Review (legacy skill name)

Review like a project owner and lead with concrete findings rather than a general summary.

## Workflow

1. Read `references/review-checklist.md`.
   Read root AGENTS.md and docs/learning/CURRENT_STEP.md to distinguish unfinished lessons from completed features; assess learner answers before introducing the next step.
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
