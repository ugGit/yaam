# Architecture Guidelines

## Layer Structure (Backend)

```
Presentation Layer → Controllers (HTTP in/out, no business logic)
Application Layer  → Use cases / commands / queries (orchestration)
Domain Layer       → Entities, value objects, domain logic (no infrastructure deps)
Infrastructure     → DB, AI adapters, email, file storage (implements interfaces)
```

Dependencies flow inward: Infrastructure → Application → Domain. Domain knows nothing about infrastructure.

## Layer Structure (Frontend)

```
Pages / Routes     → Top-level routed components
Feature Modules    → Self-contained feature areas (profile, applications, cover-letters)
Shared             → Reusable components, pipes, directives
Core               → Services, interceptors, guards, auth
```

## Architectural Patterns

Concrete patterns are documented in [patterns.md](patterns.md). Summary:

- **Backend:** CQRS + MediatR, Repository pattern, EF Core Code-First, FluentValidation in pipeline behaviors, RFC 9457 ProblemDetails for all errors
- **Frontend:** Angular Signals as primary state mechanism, smart/dumb component split as a loose guideline, feature modules with lazy loading

## Coding Style

- Follow official Angular and .NET style guides
- No magic strings — use enums and constants
- Explicit over implicit — avoid clever code
- One responsibility per class/component
- Map consequently between layers

### Backend Naming Conventions

- **Handler parameter:** `command` for `IRequestHandler<TCommand>`, `query` for `IRequestHandler<TQuery>`. Never `request`.
- **Controller CancellationToken:** Always named `cancellationToken`. Never `ct`. Never `= default` — ASP.NET Core always injects it.
- **Type aliases:** No abbreviated aliases (`DomainApp`, `DomainNote`). If a `using` alias is genuinely needed to resolve ambiguity, use the full class name as the alias (e.g., `using Application = Yaam.Domain.Entities.Application`). Prefer resolving the ambiguity by restructuring instead.

## Testing Approach

- **Backend:** Unit tests for critical domain logic; integration tests for API endpoints (real DB, no mocks)
- **Frontend:** Component tests for critical UI; e2e tests for the core loop (CV → cover letter)
- Test the behavior, not the implementation
- TBD: test framework choices (.NET: xUnit; Angular: Jest or Karma)

## AI Adapter Interface

The AI layer is abstracted behind an interface so providers are swappable:

```csharp
IAIProvider
  ├── ParseCV(fileContent: byte[]) → StructuredProfile
  ├── GenerateCoverLetter(context: CoverLetterContext) → string
  └── RefineProfile(profile: Profile, userAnswer: string) → ProfileUpdate

CoverLetterContext                            // composable — see ADR 002
  ├── Profile: Profile                        // required
  ├── JobPosting: string                      // required
  ├── UserInstructions: string?               // optional, stored per application
  ├── ReferenceCoverLetters: string[]?        // optional, stored on user profile (plain text)
  └── [future inputs as nullable props]       // e.g., Style, DeepProfile — added without changing the interface
```

Each provider (Ollama, Anthropic, OpenAI, Infomaniak) implements `IAIProvider`. Providers read only the context fields they support and ignore the rest.

## Data Model (Draft)

Core entities:
- `User` — account + profile metadata
- `Profile` — structured CV data + custom fields
- `Application` — job application record
- `CoverLetter` — generated text + version history
- `Reminder` — scheduled follow-up per application

Detailed schema TBD in specs.
