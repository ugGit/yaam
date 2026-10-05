# ADR 010: Shared contracts and service defaults

**Date:** 2026-10-05
**Status:** Accepted

## Context

Services must agree on event shapes, and they share cross-cutting plumbing (auth, broker connection, telemetry). Sharing code between services creates build-time coupling; duplicating it creates drift.

## Decision

- **`Yaam.Contracts`** holds only integration-event records, the message envelope, serializer options and topology names.
  - It is **additive-only**: fields are never renamed or removed. A breaking change is a new event type.
  - Accepted trade-off: a shared assembly couples the services at build time. The more independent alternative is per-service contract copies with contract tests.
- **`Yaam.ServiceDefaults`** holds only infrastructure plumbing: JWT authentication, the RabbitMQ connection and OpenTelemetry setup. It never contains business code.
- **Small, business-adjacent helpers** (`ValidationBehavior`, `GlobalExceptionHandler`, email sending) are **copied** into each service rather than shared ("prefer duplication over coupling").

## Consequences

- **A contract change touches both services in one commit.** That is acceptable in a monorepo, but it is the first coupling to remove if the services ever get separate repositories or teams.
- **Copied helpers can drift.** They are small enough to keep in sync manually.
