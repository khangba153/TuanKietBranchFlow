# API rules

- Controllers should depend on application service interfaces, not repositories or DbContext.
- Keep route, model binding, authentication, authorization, and HTTP status mapping in controllers.
- Register repositories, Unit of Work, application services, DbContext, and external infrastructure in `Program.cs`.
- Preserve middleware ordering: authentication before authorization, then controller mapping.
- Never return exception stack traces or secrets to clients.
- When adding an endpoint, document its expected request, successful response, and relevant failure status codes.


