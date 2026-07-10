# YAAM Backend

.NET 10 solution using Clean Architecture, CQRS + MediatR, EF Core, PostgreSQL 16.

## Projects

| Project | Purpose |
|---------|---------|
| `Yaam.API` | HTTP layer — controllers, middleware, DI wiring |
| `Yaam.Application` | Use cases — commands, queries, MediatR handlers, FluentValidation validators |
| `Yaam.Domain` | Entities, value objects, repository interfaces (no infrastructure dependencies) |
| `Yaam.Infrastructure` | EF Core, PostgreSQL, AI adapters (implements Domain interfaces) |
| `Yaam.Tests.Unit` | Unit tests — xUnit, NSubstitute, FluentAssertions |
| `Yaam.Tests.Integration` | Integration tests — xUnit, real database via `WebApplicationFactory` |

## Start

Requires PostgreSQL running. Quickest way:

```bash
docker compose up -d   # from repo root
```

Then:

```bash
dotnet restore
dotnet run --project Yaam.API
```

API at `https://localhost:7131` · Swagger at `https://localhost:7131/swagger`

## Formatting — dotnet format

```bash
dotnet format --verify-no-changes   # check only (used in CI)
dotnet format                       # fix
```

Rules come from `backend/.editorconfig`. The main constraints for C#: 4-space indent, LF line endings, `var` preferred where type is apparent, system usings sorted first.

## Testing

### Unit tests

```bash
dotnet test Yaam.Tests.Unit
```

Test domain logic in isolation. Use NSubstitute to stub dependencies; use FluentAssertions for readable assertions. No database, no HTTP.

### Integration tests

```bash
dotnet test Yaam.Tests.Integration
```

Integration tests use `WebApplicationFactory<Program>` and a real PostgreSQL database. The connection string is set via the `ConnectionStrings__DefaultConnection` environment variable, or falls back to `appsettings.Development.json`.

To run integration tests locally, make sure the database is up:

```bash
docker compose up -d   # from repo root
dotnet test Yaam.Tests.Integration
```

### Run everything

```bash
dotnet test   # runs both test projects
```

## Adding a use case

Every feature follows the same pattern:

1. Add a `Command` or `Query` + `Handler` in `Yaam.Application/Features/<Feature>/`
2. Add a `Validator` alongside it if the command takes user input
3. Add a repository interface to `Yaam.Domain/Repositories/` if new data access is needed
4. Implement the repository in `Yaam.Infrastructure/Persistence/Repositories/`
5. Add the controller action in `Yaam.API/Controllers/` — one line: translate HTTP → MediatR → HTTP

See [architecture patterns](../docs/architecture/patterns.md) for naming conventions and full examples.

## Migrations

```bash
# From the backend/ directory
dotnet ef migrations add <MigrationName> --project Yaam.Infrastructure --startup-project Yaam.API
dotnet ef database update --project Yaam.Infrastructure --startup-project Yaam.API
```
