# API rules

- Controllers should depend on application service interfaces, not repositories or DbContext.
- Keep route, model binding, authentication, authorization, and HTTP status mapping in controllers.
- Register repositories, Unit of Work, application services, DbContext, and external infrastructure in `Program.cs`.
- Preserve middleware ordering: authentication before authorization, then controller mapping.
- Never return exception stack traces or secrets to clients.
- When adding an endpoint, document its expected request, successful response, and relevant failure status codes.
- Preserve OWNER read-only and ADMIN write scope; obtain acting user identity from validated claims and check branch assignment server-side.
- Teach production error handling, safe request logs and health checks before public deployment. Do not enable Development on a public host merely to show Swagger.

