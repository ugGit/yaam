# Reminders Microservice Extraction

**Date:** 2026-10-05
**Status:** Draft
**Branch:** `feat/reminders-microservice`

## Summary

Extract the Reminders capability from the YAAM monolith into its own deployable service with its own database, behind a YARP gateway. Cross-service data flows through integration events over RabbitMQ using a hand-built transactional outbox (producer) and inbox (consumer). The whole flow is traced end-to-end with OpenTelemetry. The migration ends with deliberate failure experiments.

This is a learning project for a technical assessment (target stack: C#/.NET, Kubernetes/AWS, Grafana/Prometheus). It optimises for **(A) a deep understanding of distributed-systems patterns** and **(B) resemblance to an industry stack**. Each core pattern is built by hand once; library equivalents are compared on paper only.

**Budget:** ~10 hours over 9 days. If time runs short, cut Milestone 5 before Milestone 4. Never cut Milestone 3.

---

## Goals

- Reminders runs as a separate process with its own database (`yaam_reminders`) and never reads the monolith's database.
- No foreign key, EF navigation or shared DTO crosses the service boundary.
- Application state reaches Reminders only through integration events, with at-least-once delivery, idempotent handling and tolerance to reordering.
- One distributed trace spans: browser → gateway → monolith → outbox → RabbitMQ → Reminders.
- Each failure mode below can be reproduced and explained: consumer down, duplicate delivery, reordering, poison message, duplicate scheduler.

## Non-goals (explicitly cut)

- Kubernetes deployment, Helm, Dockerfiles for services (stretch only)
- Library-based messaging/outbox (MassTransit, Wolverine). Discussed, not implemented.
- Profile/CV extraction
- `UserId` / multi-tenancy (still deferred, see account management Phase 2)
- Migrating existing reminder data (local data is dev-only; Reminders starts empty)

---

## Target Architecture

```
 Angular (4200) ── ng dev proxy /api ──► Yaam.Gateway (YARP, 5000)
                                            │
             ┌──────────────────────────────┴───────────────┐
             │ /api/reminders/**                             │ /api/** (everything else)
             ▼                                               ▼
   Yaam.Reminders (single project)                 Yaam.API (monolith)
   • reminder CRUD + list                          • Applications, Notes, Profile, CV
   • daily due-reminder email job                  • writes outbox_messages in same tx
   • ApplicationSnapshot read model                • OutboxRelay BackgroundService
   • inbox: processed_messages                               │
             ▲  db: yaam_reminders                           │ db: yaam
             │                                               ▼
             └──────── queue reminders.application-events ◄── exchange yaam.applications (topic)
                                  RabbitMQ
```

### Data ownership

| Data | Owner | Other services get it via |
|---|---|---|
| Application, ApplicationNote | Monolith | `ApplicationUpserted` / `ApplicationDeleted` events |
| Reminder | Reminders | Reminders HTTP API only |
| ApplicationSnapshot | Reminders (a local copy, written only by its consumer) | none |

One Postgres server hosts two databases. Each service has its own connection string and never connects to the other's database.

### Replacing today's couplings

| Today | After |
|---|---|
| FK `Reminders.ApplicationId → Applications` with cascade delete | `ApplicationDeleted` event; Reminders deletes its own reminders |
| `Reminder.Application` navigation (list view, email text) | Read from the local `ApplicationSnapshot` |
| `SetReminderCommand` checks the application exists via `IApplicationRepository` | Checks that the snapshot exists and is not deleted. A brief lag after creation may produce a 404 (accepted trade-off vs. a sync HTTP dependency) |
| `ApplicationDto` embeds reminders | Removed; the frontend detail page calls Reminders separately (client-side API composition) |
| Monolith `ReminderNotificationService` | Removed from the monolith in the same change that enables it in Reminders (never two schedulers, never zero) |

---

## Components

### `Yaam.Contracts` (new class library)

Shared integration-event contracts, referenced by the monolith and Reminders.

```csharp
public sealed record MessageEnvelope(Guid MessageId, string Type, DateTimeOffset OccurredAt,
                                     string? TraceParent, JsonElement Payload);

public sealed record ApplicationUpserted(Guid ApplicationId, long Version, string CompanyName,
                                         string Role, DateOnly? DateApplied, string? ContactName);

public sealed record ApplicationDeleted(Guid ApplicationId, long Version);
```

- Created and updated are a single upsert event, because the consumer treats both the same.
- Events carry state (event-carried state transfer), so the consumer never calls back into the monolith.
- **Known trade-off:** a shared assembly couples the services at build time. It was accepted for speed. The alternative (each service owns its own copy, with contract tests) is the more independent option.
- Contracts are additive-only: never rename or remove fields. Breaking changes need a new event type.

### Monolith changes (`Yaam.API` / `Yaam.Infrastructure` / `Yaam.Domain` / `Yaam.UseCases`)

- `Application` gains `Version` (`long`), incremented on every add or modify.
- New `OutboxMessage` entity/table: `Id` (bigserial, defines publish order), `MessageId` (Guid), `Type`, `Payload` (jsonb), `TraceParent`, `OccurredAt`, `ProcessedAt?`, `Attempts`.
- `AppDbContext.SaveChangesAsync` scans the ChangeTracker for added, modified or deleted `Application` entries before saving. It bumps `Version` and adds one `OutboxMessage` for each. Because it all happens in one `SaveChanges`, it is one transaction, and the dual write is solved.
  - **Transitional choice:** this is used only in the monolith. Extracted services raise **domain events** from entities, map them to integration events, then write the outbox (see Future Direction).
- `OutboxRelay` (`BackgroundService`) polls every 1s:
  - `SELECT … WHERE ProcessedAt IS NULL ORDER BY Id LIMIT 50 FOR UPDATE SKIP LOCKED`
  - publish with publisher confirms;
  - set `ProcessedAt`.
  - A crash between publish and mark causes a republish (at-least-once by design).
- Backfill migration: inserts an `ApplicationUpserted` outbox row for every existing application.
- Removed from the monolith:
  - the `Reminder` entity, repository, configuration and use cases;
  - `ApplicationReminderController`, `RemindersController`, `ReminderNotificationService`;
  - the `Application.Reminders` navigation and the reminders in `ApplicationDto`/ViewModel.
  - An EF migration drops the `Reminders` table.
- CORS config can stay but is no longer needed in dev (same-origin via proxy).

### `Yaam.Reminders` (new single ASP.NET Core project, feature folders)

Follows the existing repo conventions:
- MediatR handlers with `command`/`query` parameters
- `*InputModel` in the controller file, `*Dto` → `*ViewModel` mappers in their own files
- ProblemDetails errors
- `[Authorize]` with the same Supabase JWKS JWT validation as the monolith

**HTTP API (all under `/api/reminders`):**

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/reminders` | active reminders (list page), joined with snapshot data; optional `?applicationId={id}` narrows to one application (detail page) |
| POST | `/api/reminders` | create (`applicationId`, `delayDays`, `note` in body) |
| PATCH | `/api/reminders/{id}/complete` | complete |
| PATCH | `/api/reminders/{id}/reschedule` | reschedule |
| DELETE | `/api/reminders/{id}` | delete |
**Data (`yaam_reminders`):**
- `Reminders`: same columns as today, no foreign key.
- `ApplicationSnapshots`: `ApplicationId` (PK), `Version`, `CompanyName`, `Role`, `DateApplied`, `ContactName`, `IsDeleted`.
- `ProcessedMessages`: `MessageId` (PK), `ProcessedAt`.

**Consumer** (`RabbitMQ.Client`, manual ack, hosted service). For each message, in one DB transaction:
1. If `MessageId` is in `ProcessedMessages`, skip it (idempotency).
2. `ApplicationUpserted`: insert the snapshot, or update it only if `incoming.Version > stored.Version`. Stale versions are ignored (ordering).
3. `ApplicationDeleted`: if `incoming.Version > stored.Version`, set `IsDeleted = true` (tombstone; inserted if no snapshot exists yet) and delete the application's reminders. The tombstone stops a late upsert from bringing the application back.
4. Insert `MessageId` into `ProcessedMessages`, commit, then ack. A crash before the ack causes redelivery, which the inbox absorbs.
5. If processing throws, nack and requeue. The quorum queue's delivery limit (5) dead-letters poison messages to `reminders.application-events.dlq`.

The handler logic is a plain class callable without RabbitMQ (for tests).

**Daily job:** `ReminderNotificationService` moves here unchanged at first. In Milestone 5 it gets an atomic claim:
- `UPDATE … SET NotifiedAt = now() WHERE Id = @id AND NotifiedAt IS NULL`
- send only if one row was affected.

### `Yaam.Gateway` (new, YARP)

- Port 5000. Routes live in config only:
  - `/api/reminders/{**rest}` → Reminders
  - `/api/{**rest}` → monolith
- Forwards the `Authorization` header unchanged.
- No auth, no aggregation, no business logic.

### Frontend

- Provide `BASE_PATH = ''` so the generated clients use relative URLs. The ng dev proxy sends `/api` to the gateway (`http://localhost:5000`).
- The generator runs twice:
  - monolith → `src/app/generated/api/` (as today)
  - Reminders → `src/app/generated/reminders-api/` (new npm script)
- Update `CLAUDE.md` to cover the second folder.
- The application detail page's reminder section loads its reminders from the Reminders API instead of from `ApplicationDto`.
- The reminders page switches to the new client.

### Infrastructure (`docker-compose.yml`)

- Add `rabbitmq:management` (5672, UI on 15672).
- Add `grafana/otel-lgtm` (Grafana 3000, OTLP 4317/4318). Verify the image details before use.
- Create the `yaam_reminders` database through a Postgres init script.
- The .NET services run with `dotnet run`. A second Reminders replica is a second process on another port.

---

## Observability

- OpenTelemetry in all three services:
  - instrumentation for ASP.NET Core, HttpClient and Npgsql;
  - OTLP export to otel-lgtm;
  - `service.name` set for each service.
- **Trace propagation across the outbox:**
  - the current `traceparent` is stored in `OutboxMessage.TraceParent` when the row is written;
  - the relay starts its publish span with that parent and writes `traceparent` into the AMQP headers;
  - the consumer extracts it and continues the trace.
  - Expected result: one trace, with the outbox and queue delay visible as a gap.
- **Metrics:**
  - `outbox.pending` (gauge) and `outbox.oldest_pending_age_seconds` (gauge), the primary health signal of the pattern;
  - consumer counters: `processed`, `duplicates_skipped`, `stale_ignored`, `dead_lettered`.
- Logs are structured and include trace and span IDs.

---

## Testing

- **Reminders integration tests** (new `Yaam.Reminders.Tests.Integration`, WebApplicationFactory, real Postgres): the existing reminder endpoint tests, ported to the new routes.
- **Consumer tests**, calling the handler directly with a real DB:
  - duplicate message → one effect;
  - v3 after v4 → ignored;
  - upsert after delete → stays deleted;
  - delete → the application's reminders are removed;
  - create a reminder for an unknown or deleted application → 404.
- **Monolith tests:**
  - creating, updating or deleting an Application writes exactly one outbox row with the correct type and version;
  - a failed save writes none.
- Existing monolith reminder tests and unit tests move to the new service or are deleted.
- **CI:** the new projects are added to `Yaam.slnx`; the backend workflow provisions the second test database.

---

## Failure Experiments (Milestone 5)

| # | Experiment | Expected observation |
|---|---|---|
| 1 | Stop Reminders, edit applications, restart | queue depth grows, then drains; snapshots converge |
| 2 | Kill the relay between publish and mark | duplicate delivered; `duplicates_skipped` increments |
| 3 | Republish an old version through the RabbitMQ UI | ignored; `stale_ignored` increments |
| 4 | Publish a malformed message | five delivery attempts, then DLQ; the queue keeps flowing |
| 5 | Run two Reminders replicas at job time | duplicate emails reproduced, then fixed with the atomic claim; discuss at-most-once vs at-least-once for side effects |

---

## Milestones

| # | Milestone | Est. |
|---|---|---|
| 0 | Branch, spec, ADRs for the key decisions | 0.5h |
| 1 | YARP gateway; frontend via the proxy with relative base path | 1h |
| 2 | Reminders service: own project + DB + auth; endpoints moved; gateway route; frontend on the second client; monolith reminders removed | 2h |
| 3 | Contracts, monolith outbox + relay + backfill, RabbitMQ topology, consumer with inbox/versioning/tombstones | 3h |
| 4 | OpenTelemetry tracing across the async hop + outbox/consumer metrics in Grafana | 1.5h |
| 5 | Failure experiments + scheduler race fix | 1.5h |
| 6 | Interview notes: decisions and trade-offs | 0.5h |

**Interim state after Milestone 2:** reminders work, but snapshots are empty until Milestone 3. Creating a reminder returns 404 until the events flow. This is acceptable on a feature branch.

---

## Decision Log (become ADRs in Milestone 0)

1. **Extract Reminders first (not Profile).** It is where the coupling is, so it teaches the distributed-data problems.
2. **Asynchronous state transfer over a sync lookup** for application data in Reminders. Accept eventual consistency in exchange for runtime independence.
3. **Hand-built outbox/inbox over RabbitMQ.Client** instead of MassTransit or Wolverine. The goal is learning; the libraries are compared on paper.
4. **Outbox rows from a ChangeTracker diff in the monolith (transitional)**; domain events in extracted services.
5. **Shared `Yaam.Contracts` assembly.** Accepted build-time coupling.
6. **Single-project service structure** for Reminders.
7. **Dumb gateway; every service validates the JWT itself** (zero trust).
8. **URL prefix = owning service** (`/api/reminders/**`).
9. **Two generated OpenAPI clients** instead of a gateway-merged spec.
10. **Observability backend: grafana/otel-lgtm.**

## Future Direction

- Extracted services publish via **domain events**:
  - entities raise domain events;
  - `SaveChanges` collects them;
  - a mapper translates only externally relevant ones into `Yaam.Contracts` integration events;
  - the outbox row is written in the same transaction.
- Domain events are never published to the broker directly, because that leaks the internal model into the contract.
- Later: replace the hand-built outbox with a library, add Kubernetes deployment with probes, extract Profile, and add `UserId` scoping. `UserId` must then flow through event contracts too.
