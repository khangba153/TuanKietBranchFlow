# Review checklist

## Correctness

- Interface and implementation signatures agree.
- Async methods return the values their callers expect.
- Entity key types match repository lookup methods.
- Null handling and input normalization occur before dereference.
- Multi-entity writes save atomically when required.
- Exceptions do not leave transactions open or hide the root failure without useful logging.

## Architecture and DI

- Controllers call services rather than repositories.
- Services do not depend on HTTP abstractions.
- EF Core access remains in Infrastructure.
- Every constructor dependency has the correct DI registration and lifetime.
- DbContext, repositories, and Unit of Work share a compatible scoped lifetime.

## Security

- Passwords are hashed and never returned.
- Secrets are not hard-coded or exposed.
- Authentication and authorization middleware are ordered correctly.
- User-controlled data is validated before persistence or URL generation.
- Error responses do not expose stack traces or sensitive internals.

## API behavior

- Routes and binding attributes match the request contract.
- Status codes match success, validation, conflict, authentication, and server-error outcomes.
- DTOs prevent accidental serialization of EF navigation graphs.

## Blazor

- Hosting configuration matches classic Blazor Server.
- Routed pages, layouts, and shared components have distinct roles.
- Bootstrap layout remains responsive.
- Forms and interactive-looking controls have appropriate behavior or are clearly static.

## Verification

- Build affected projects.
- Identify runtime services not exercised by compilation, especially SQL Server and Redis.
- Note missing automated tests when behavior is non-trivial.

