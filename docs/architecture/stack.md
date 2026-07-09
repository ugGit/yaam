# Tech Stack

## Frontend
- **Framework:** Angular (latest stable)
- **Language:** TypeScript
- **Styling:** TBD (Angular Material or Tailwind CSS)
- **State management:** TBD (NgRx or Angular Signals)

## Backend
- **Framework:** .NET C# (ASP.NET Core, latest LTS)
- **API style:** REST (OpenAPI/Swagger documented)
- **Auth:** ASP.NET Core Identity + JWT (1h access token, 7d refresh token, silent refresh). External providers (Google, LinkedIn) are a future feature.

## Database
- **PostgreSQL** — confirmed. Required for JSON column support (custom profile fields) and strong relational support (child tables for work experience, education, etc.).
- SQLite for local dev is acceptable but PostgreSQL should be used even locally to avoid divergence.

## AI Adapter Layer
- Provider-agnostic interface — see `ai-adapters/`
- **Local dev:** Ollama
- **Early production:** Anthropic Claude API or OpenAI API (trial credits)
- **Target production:** Infomaniak AI (Swiss hosting, GDPR-aligned)

## File Storage
- CV uploads (PDF only, 2 MB max) and cover letter plain text versions
- Local filesystem for dev, S3-compatible object storage for prod (provider TBD)

## Background Jobs
- Required for reminder notifications (daily job at 08:00 server time)
- Candidates: Hangfire (recommended — .NET native, PostgreSQL-backed), Quartz.NET

## Email
- TBD — for reminder notifications
- Candidates: Mailgun, Resend, SMTP relay

## Hosting
- TBD — see [operations/domain.md](../operations/domain.md)
- Candidates: Infomaniak (Swiss), Railway, Render

## CI/CD
- TBD — see [operations/cd-setup.md](../operations/cd-setup.md)
- Candidates: GitHub Actions

## Development Tools
- **Dependency updates:** Renovate (via agent integration, planned)
- **Package management:** npm (frontend), NuGet (.NET)
