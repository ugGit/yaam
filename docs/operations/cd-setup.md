# CI/CD Setup

## Plan

- **Source control:** GitHub (single monorepo)
- **CI/CD:** GitHub Actions
- **Hosting:** TBD (Infomaniak, Railway, or Render)

## Pipeline (Draft)

```
Push to main
  └── CI: lint + test (frontend + backend)
      └── Build Docker images (or publish artifacts)
          └── CD: deploy to staging
              └── Manual approval → deploy to production
```

## Environment Strategy

- **Local dev:** Docker Compose (Angular dev server + .NET API + PostgreSQL + Ollama)
- **Staging:** mirrors production, auto-deployed on merge to main
- **Production:** manual trigger or tag-based release

## Renovate

Plan to connect Renovate bot (via agent) for automated dependency update PRs.

## To Do

- Set up GitHub repo
- Configure GitHub Actions workflows (lint, test, build)
- Set up Docker Compose for local dev
- Choose and configure hosting provider
- Set up Renovate
