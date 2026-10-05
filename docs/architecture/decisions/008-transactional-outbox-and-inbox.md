# ADR 008: Hand-built transactional outbox and idempotent inbox

**Date:** 2026-10-05
**Status:** Accepted

## Context

The monolith must notify Reminders about application changes. Writing to the database and publishing to a broker are two separate systems. Doing both directly (a "dual write") loses events or publishes phantom events when one side fails. RabbitMQ delivers at least once, so consumers will see duplicates and, with competing consumers, reordering.

## Decision

- **Producer (monolith):**
  - Integration events are written to an `OutboxMessages` table in the same `SaveChanges` transaction as the state change.
  - **Transitional:** the monolith derives events from the EF ChangeTracker diff of `Application` entries, because its entities are anemic. Extracted services instead raise **domain events** from entities and map only externally relevant ones to integration events before writing the outbox. Domain events are never published to the broker directly.
  - `Application.Version` is a monotonic version and an EF concurrency token.
- **Relay:**
  - A background service polls pending rows with `FOR UPDATE SKIP LOCKED` and publishes them in `Id` order with publisher confirms and `mandatory: true`.
  - It stops a batch at the first failure, to preserve order.
- **Consumer (Reminders):** in one transaction it:
  - checks the `ProcessedMessages` inbox;
  - applies the event only if `incoming.Version > stored.Version`;
  - tombstones on delete;
  - records the message ID.
  - It acks only after commit.
  - Unknown event types are acknowledged and ignored (tolerant reader); malformed payloads are nacked.
- **Broker:**
  - The topic exchange `yaam.applications` is owned by the producer.
  - The quorum queue `reminders.application-events` is owned by the consumer, with `x-delivery-limit` 5 and a dead-letter queue.
- **Library:** the pattern is hand-built on `RabbitMQ.Client` to learn the mechanics. MassTransit and Wolverine provide the same pattern; they are compared on paper only.

## Consequences

- **Delivery is at least once; effects are exactly once** thanks to idempotency and versions.
- **Polling adds up to one polling interval of latency.** Alternatives: Postgres `LISTEN/NOTIFY` or CDC.
- **`ProcessedMessages` grows without bound.** A retention job is future work.
- **Outbox health must be monitored:** pending count and oldest pending age.
