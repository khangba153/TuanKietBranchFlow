# Application rules

- Put use-case orchestration in services and keep service interfaces next to their implementations unless the project is reorganized explicitly.
- Use DTOs for inputs and outputs; do not return EF navigation graphs directly.
- Validate and normalize user input before persistence.
- Keep password hashing and reusable transformations in helpers, but keep business decisions in services.
- Use constants for stable role identifiers and response messages.
- Application may reference Infrastructure, but it must not depend on API controllers or Blazor components.
- Let callers receive meaningful response data and status information; do not hide failures in `async Task` methods returning no result when a result is expected.
- Match current result DTO flags and map entities explicitly; do not assume every boolean flag must be true on success.
- Confirm effective-date and branch-history business rules before implementing employee state transitions. Keep authorization checks when a repository loads an entity without a branch filter.

