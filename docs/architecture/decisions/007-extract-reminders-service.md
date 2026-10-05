# ADR 007: Extract Reminders as a separate service

**Date:** 2026-10-05
**Status:** Accepted

## Context

YAAM is a layered monolith with one database. We want hands-on experience with a microservice split. Of the three feature areas, Profile is isolated, while Reminders is tightly coupled to Applications in four ways:
- a foreign key with cascade delete;
- two-way EF navigation;
- a cross-feature existence check in `SetReminderCommand`;
- `ApplicationDto` embedding reminders.

## Decision

- **Reminders is extracted first.** Splitting it forces the distributed-data problems into the open: no cross-database foreign keys, eventual consistency, event-driven deletion.
- **Reminders owns a database (`yaam_reminders`)** and never reads the monolith's database.
- **Application data reaches Reminders by asynchronous state transfer.** Reminders keeps a local `ApplicationSnapshot` read model built from integration events, not a synchronous HTTP lookup. That makes Reminders independent of the monolith's availability at runtime.
- **The service is a single project with feature folders.** Handlers use `RemindersDbContext` directly: inside a small service the service boundary is the abstraction, and a repository layer would add ceremony without isolation. It still follows the repo conventions (MediatR, `*InputModel`, `*Dto` → `*ViewModel`).

## Consequences

- **Eventual consistency.** A just-created application can briefly return 404 when a reminder is added (milliseconds to about a second). This is accepted.
- **Deletion is event-driven.** `ApplicationDeleted` removes reminders. If events stop flowing, orphaned reminders persist until they resume.
- **The application detail page now composes two API calls** (client-side API composition).
- **Two deployables, two databases and a broker make local development heavier.**
