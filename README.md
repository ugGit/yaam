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

### 0 — After cloning

```bash
git config core.hooksPath .githooks
```

This activates the pre-push hook that runs CI checks (build, format, lint, unit tests) locally before every push.

### 1 — Start the database and Ollama

```bash
docker compose up -d
```

This starts PostgreSQL on `localhost:5433`, Ollama on `localhost:11434`, and Mailpit on `localhost:1025` (SMTP) / `http://localhost:8025` (web UI — inspect outbound emails locally).

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
- Scalar UI: `https://localhost:7131/scalar/v1`

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

## IDE setup

| IDE | Open | Run configs |
|-----|------|-------------|
| **WebStorm** | `frontend/` folder | `frontend/.run/` — Dev Server, Lint, Build Production |
| **Rider** | `backend/Yaam.slnx` | use Rider's built-in run/test buttons |
| **IntelliJ IDEA** | repo root | discovers frontend configs automatically |

Any editor works, but only WebStorm has pre-configured run configs checked into the repo.

**WebStorm** — node version is managed via `frontend/.nvmrc`. Point the project Node interpreter at the nvm-managed binary or let WebStorm pick it up automatically.

**Rider** — open `backend/Yaam.slnx` directly (not the folder). The solution file includes all backend projects. Use the built-in run/debug buttons and the test explorer for unit tests.

**IntelliJ IDEA** — useful for cross-cutting work: docs, CI config, the root `docker-compose.yml`. It will surface the WebStorm run configs from the frontend subfolder, but running the backend from here is not tested.

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
├── backend/          .NET 10 solution (API / UseCases / Domain / Infrastructure)
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
Yaam.UseCases          Use cases — CQRS commands/queries, MediatR handlers, validators
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
