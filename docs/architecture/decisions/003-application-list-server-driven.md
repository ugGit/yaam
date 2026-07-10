# ADR 003: Server-driven filtering and sorting for list endpoints

**Date:** 2026-07-10
**Status:** Accepted

## Context

Any feature that presents a list of records (applications, reminders, cover letters, etc.) needs to support filtering and sorting. The question is whether this logic lives on the server (API returns a filtered/sorted subset) or the client (API returns everything, the frontend filters in-memory).

A fat-frontend approach — returning the full list and filtering in-memory with Signals — is tempting for small datasets, but:

- It pushes business logic (which records match a filter, what the canonical sort order is) into the frontend, where it is harder to test and tends to drift across features.
- Adding pagination later requires moving the logic server-side anyway — doing it twice wastes time.
- Any list can grow beyond comfortable in-memory size; a personal tracker with hundreds of records is not unusual.

## Decision

Filtering and sorting for all list endpoints are handled **server-side** via query parameters:

```
GET /api/{resource}?{filterParam}={value}&sort={field}&order=asc|desc
```

The frontend is a thin client: it holds the user's current filter/sort state (a Signal of `{ filter, sort, order }`), passes it as query params on every fetch, and renders exactly what the API returns. The frontend holds no derived list state.

Each resource defines its own supported filter and sort params; defaults are applied in the query handler when params are absent. EF Core `.Where()` and `.OrderBy()` are the implementation mechanism.

## Consequences

- Filtering/sorting logic lives entirely in backend query handlers — consistent, testable, and independent of the client.
- Adding server-side pagination to any list requires only appending `page` and `pageSize` params; no frontend state refactor.
- Each filter/sort change triggers a network request. Acceptable for a personal tracker with a fast local API.
- Frontend components stay simple and uniform across features — the same fetch-on-filter-change pattern applies everywhere.
- If offline support or optimistic UI becomes a requirement, this decision can be revisited per feature.
