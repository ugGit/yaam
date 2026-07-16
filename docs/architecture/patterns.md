# Architectural Patterns

Decisions made through design review. These apply across the entire codebase — new code should follow them by default. Deviations need a documented reason.

---

## Backend

### CQRS + MediatR
Every use case in the UseCase layer is a self-contained `Command` or `Query` with a matching `Handler`. MediatR dispatches them.

```
Commands → mutate state       (e.g., CreateApplicationCommand)
Queries  → read state only    (e.g., GetApplicationByIdQuery)
```

- One class per use case. No fat service classes with ten methods.
- Handlers receive their dependencies via constructor injection.
- MediatR pipeline behaviors handle cross-cutting concerns (validation, logging) — not the handlers themselves.

### MediatR Pipeline Behaviors (order matters)

```
Request
  → LoggingBehavior          (log command/query name + duration)
  → ValidationBehavior       (run FluentValidation — abort if invalid)
  → Handler
  → Response
```

### FluentValidation
Validators live in the Application layer alongside their Command/Query.

```
CreateApplicationCommand
CreateApplicationCommandValidator  ← one per command/query that needs validation
```

- Validators are registered automatically via assembly scanning.
- The `ValidationBehavior` pipeline behavior runs them before the handler. Validation failures return RFC 9457 `ProblemDetails` with a `400 Bad Request` — the handler never runs.
- Domain-level invariants (e.g., "a profile must have a first name") are enforced by the domain entity itself via guard clauses, not by FluentValidation. FluentValidation handles API input shape; domain guards handle business rules.

### Repository Pattern + EF Core Code-First
- Entities are defined as C# classes first. Migrations are generated from them via `dotnet ef migrations add`.
- Each aggregate root has its own repository interface in the Domain layer and implementation in the Infrastructure layer.

```
// Domain layer
IApplicationRepository
  GetById(id) → Application?
  GetAllForUser(userId) → IReadOnlyList<Application>
  Save(application) → void
  Delete(id) → void

// Infrastructure layer
ApplicationRepository : IApplicationRepository  (uses AppDbContext internally)
```

- Repositories return domain entities, not EF tracking objects.
- Do not expose `IQueryable` from repositories — callers get lists or single entities, not composable queries. This keeps the data access boundary clean.
- Read-heavy queries (e.g., application list with filters) may bypass the repository and use `DbContext` directly in the Query handler for performance. This is the one deliberate exception to the repository rule.

### API Error Responses — RFC 9457 ProblemDetails
All error responses follow the RFC 9457 `ProblemDetails` format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9457",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "errors": {
    "companyName": ["Company name is required."]
  }
}
```

- Configured globally via `app.UseExceptionHandler` + `IProblemDetailsService`.
- Validation errors: `400` with field-level `errors` map.
- Not found: `404` with a `detail` message.
- Auth failures: `401` / `403` — no detail to avoid information leakage.
- Unhandled exceptions: `500` with no internal detail exposed in production.

---

## Frontend

### Angular Signals — primary state mechanism
Use Signals for all component and service state wherever possible. Avoid `BehaviorSubject` / `Observable` for state that Signals can handle.

```typescript
// Prefer
readonly applications = signal<Application[]>([]);
readonly isLoading = signal(false);
readonly selectedApplication = computed(() => ...);

// Avoid for state (Observables still fine for events, HTTP responses)
private applicationsSubject = new BehaviorSubject<Application[]>([]);
```

- HTTP calls still use `HttpClient` (returns Observables) — convert to signals at the service boundary with `toSignal()`.
- Effects (`effect()`) for side effects that react to signal changes (e.g., persist to localStorage, trigger analytics).

### Smart / Dumb Components — loose guideline
Apply where it naturally fits; don't force it everywhere.

**Container (smart) components** — own signals, call services, handle routing:
```typescript
// ApplicationDetailComponent — fetches data, owns state
```

**Presentational (dumb) components** — accept `@Input()` / `input()`, emit `@Output()` / `output()`:
```typescript
// ApplicationCardComponent — renders one application, emits statusChange
```

Rule of thumb: if a component directly injects a service to fetch data, it's a container. If it only displays what it's given, it's presentational. Don't split artificially — a form that manages its own field state can stay as one component.

### Feature Module Structure
Each feature area is a self-contained module with lazy loading:

```
src/app/
  features/
    applications/
      components/       presentational components
      pages/            routed container components
      services/         feature-scoped services + signals
      models/           TypeScript interfaces for this feature
    profile/
    cover-letters/
    reminders/
    auth/
  shared/               components, pipes, directives used across features
  core/                 app-wide services, interceptors, guards, auth
```

---

## Cross-Cutting

### Mapping Between Layers
Map explicitly between layers — do not let domain entities leak into API responses or let API DTOs flow into the domain.

```
API Request DTO → Command/Query (Application layer)
Domain Entity   → Response DTO  (in the handler or a dedicated mapper)
```

A simple static mapper class or a mapping library (e.g., Mapperly — compile-time, no reflection) is fine. AutoMapper is discouraged — magic mapping hides intent.

### No Business Logic in Controllers
Controllers do one thing: translate HTTP in/out to MediatR in/out.

```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateApplicationRequest request)
{
    var command = new CreateApplicationCommand(request.CompanyName, ...);
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

No if-statements, no service calls, no domain logic in controllers.

### Consistent Naming
| Concept | Naming |
|---------|--------|
| Commands | `VerbNounCommand` (e.g., `CreateApplicationCommand`) |
| Queries | `GetNounByXQuery` (e.g., `GetApplicationByIdQuery`) |
| Handlers | `VerbNounCommandHandler`, `GetNounByXQueryHandler` |
| Validators | same name as command/query + `Validator` suffix |
| Repositories | `INounRepository` / `NounRepository` |
| Angular services | `NounService` (e.g., `ApplicationService`) |
| Angular signals | camelCase noun (e.g., `applications`, `isLoading`) |
