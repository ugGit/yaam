# YAAM — Yet Another Application Manager

A job application tracker that closes the loop: import your CV, build your profile, capture applications, generate cover letters, and stay on top of reminders.

## The core loop

1. **CV in** — parse your CV, build a structured profile
2. **Profile refined** — AI asks follow-up questions to sharpen your profile
3. **Application captured** — log where you applied, with which CV/cover letter, to whom, when
4. **Cover letter generated** — AI drafts a cover letter from your profile + job posting
5. **Reminders set** — email follow-up reminders with pre-filled templates

## Stack

| Layer | Tech |
|-------|------|
| Frontend | Angular 22 |
| Backend | .NET 10 (ASP.NET Core) |
| Database | PostgreSQL 16 |
| AI | Provider-agnostic adapter — Ollama (local), API (prod) |

---

## Getting started

### Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10.x | https://dot.net |
| Node.js | 24.x | https://nodejs.org |
| Docker + Compose | any recent | https://docs.docker.com/get-docker |

### 1 — Start the database and Ollama

```bash
docker compose up -d
```

This starts PostgreSQL 16 on `localhost:5432` and Ollama on `localhost:11434`.

> **Port conflict?** If port 5432 is already in use, create `docker-compose.override.yml` at the repo root:
> ```yaml
> services:
>   postgres:
>     ports:
>       - "5433:5432"
> ```
> Then update `Port=5432` to `Port=5433` in `backend/Yaam.API/appsettings.Development.json`.

### 2 — Run the backend

```bash
cd backend
dotnet restore
dotnet run --project Yaam.API
```

API available at:
- HTTP: `http://localhost:5231`
- HTTPS: `https://localhost:7131`
- Swagger UI: `https://localhost:7131/swagger`

### 3 — Run the frontend

```bash
cd frontend
npm install
npm start
```

App available at `http://localhost:4200`.

### 4 — Run tests

```bash
# Backend
dotnet test backend/

# Frontend lint + format check
cd frontend && npx ng lint && npm run format:check
```

---

## Code formatting

Formatting is enforced in CI. To run locally:

```bash
# Backend — check
dotnet format backend/ --verify-no-changes

# Backend — fix
dotnet format backend/

# Frontend — check
cd frontend && npm run format:check

# Frontend — fix
cd frontend && npm run format
```

---

## Project layout

```
yaam/
├── backend/          .NET 10 solution (API / Application / Domain / Infrastructure)
├── frontend/         Angular 22 app
├── ai-adapters/      Provider-agnostic AI abstraction (future)
└── docs/
    ├── architecture/ Stack decisions, patterns, ADRs
    ├── business/     Business plan, pricing, license
    ├── specs/        MVP feature specs
    ├── marketing/    Blog outline, landing page
    └── operations/   Domain, CD setup
```

### Backend architecture

```
Yaam.API                .NET 10 — HTTP layer, controllers, middleware, DI wiring
Yaam.Application        Use cases — CQRS commands/queries, MediatR handlers, validators
Yaam.Domain             Entities, value objects, repository interfaces (no infra deps)
Yaam.Infrastructure     EF Core, PostgreSQL, AI adapters (implements Domain interfaces)
Yaam.Tests.Unit         Unit tests (domain logic)
Yaam.Tests.Integration  Integration tests (API + real DB)
```

### Frontend architecture

```
src/app/
  features/     One folder per feature area, each lazy-loaded
    applications/
    profile/
    cover-letters/
    reminders/
    auth/
  shared/       Reusable components, pipes, directives
  core/         App-wide services, interceptors, guards
```

---

## Docs

| Area | Path |
|------|------|
| Architecture & patterns | [docs/architecture/](docs/architecture/) |
| MVP & future feature specs | [docs/specs/](docs/specs/) |
| Business plan & pricing | [docs/business/](docs/business/) |
| Marketing & blog outline | [docs/marketing/](docs/marketing/) |
| Domain & CD setup | [docs/operations/](docs/operations/) |

## Status

Foundation complete. Feature development in progress.
