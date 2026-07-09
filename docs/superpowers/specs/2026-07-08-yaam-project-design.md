# YAAM Project Design

**Date:** 2026-07-08
**Status:** Approved

## Context

Solo side project: a job application tracker that covers the full application lifecycle. Working name: YAAM (Yet Another Application Manager). Target: English-first, i18n later. Swiss market angle (Infomaniak hosting, GDPR-aligned AI).

## Decisions Made

| Question | Decision |
|----------|----------|
| Language | English (i18n as future feature) |
| Team | Solo |
| Frontend | Angular |
| Backend | .NET C# (ASP.NET Core) |
| AI | Provider-agnostic adapter — Ollama (local), API (early prod), Infomaniak (target prod) |
| Repo structure | Monorepo (Approach A: docs-first) |
| License | AGPL-3.0 + commercial license option |
| Pricing | Freemium (free: tracking; paid: AI features) |

## Repo Structure

```
yaam/
├── docs/
│   ├── business/         business-plan.md, pricing.md, license.md
│   ├── specs/            mvp-features.md, future-features.md
│   ├── architecture/     stack.md, guidelines.md, decisions/
│   ├── marketing/        blog-outline.md, landing-page.md
│   ├── operations/       domain.md, cd-setup.md
│   └── superpowers/      AI-generated specs and plans
├── frontend/             Angular app
├── backend/              .NET C# API
└── ai-adapters/          provider-agnostic AI abstraction
```

## MVP Core Loop

CV import → Profile built → Application captured → Cover letter generated → Reminders set

### Features in MVP Scope
0. Account management (registration, login, JWT auth — prerequisite)
1. CV parsing (PDF only → structured typed profile)
2. User profile (predefined + custom fields, AI refinement)
3. Application tracking (company, role, contact, status, CV/CL versions)
4. Cover letter generation (profile + job posting + context → plain text)
5. Reminders (scheduled email notifications with templates)

### Out of Scope (MVP)
- Browser auto-fill plugin
- Automated email sending
- AI job search
- Word/LaTeX templates
- i18n

## Architecture

### Backend Layers
```
API → Application → Domain ← Infrastructure
```
Domain has no infrastructure dependencies. AI behind `IAIProvider` interface.

### AI Adapter Interface
```csharp
IAIProvider
  ├── ParseCV(fileContent: byte[]) → StructuredProfile
  ├── GenerateCoverLetter(context: CoverLetterContext) → string  // composable — ADR 002
  └── RefineProfile(profile: Profile, userAnswer: string) → ProfileUpdate
```

### Testing
- Backend: unit tests (domain logic) + integration tests (API, real DB)
- Frontend: component tests + e2e for core loop
- No mocking the database in integration tests

## Marketing
- Dev.to blog series documenting the full journey (8 posts planned)
- Landing page framed around time saved
- Blog-as-marketing from day one

## Decided Since Initial Design

| Question | Decision |
|----------|----------|
| Database | PostgreSQL — required for JSON column (custom profile fields) and child tables |
| Auth | ASP.NET Core Identity + JWT (1h access / 7d refresh) |
| CV file format | PDF only for MVP (2 MB max) |
| Profile data model | Typed entity + child tables + JSON column for custom fields — see ADR 001 |
| Cover letter context | Composable `CoverLetterContext` object — see ADR 002 |
| Background jobs | Required for reminders; Hangfire (PostgreSQL-backed) recommended |
| Timezone | Not stored for MVP — server time used for reminder notifications |
| Backend pattern | CQRS + MediatR, Repository pattern, EF Core Code-First |
| Validation | FluentValidation in MediatR pipeline behavior |
| API errors | RFC 9457 ProblemDetails |
| Frontend state | Angular Signals (primary), smart/dumb components as loose guideline |
| Mapping | Explicit layer mappers — no AutoMapper |

## Still Open

- Hosting provider (Infomaniak preferred)
- Domain name (candidates in operations/domain.md)
- Styling library (Angular Material vs Tailwind)
- State management (NgRx vs Signals)
- Email provider (Mailgun, Resend, or SMTP relay)
