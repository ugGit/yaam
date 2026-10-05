# ADR 009: Gateway, authentication and API clients

**Date:** 2026-10-05
**Status:** Accepted

## Context

With two backends, the frontend needs a single entry point, each service needs to authenticate requests, and the generated OpenAPI client no longer maps to one backend.

## Decision

- **Routing-only YARP gateway (`Yaam.Gateway`, port 5100).** No auth, aggregation or business logic in it, because logic in a gateway turns into a hidden shared monolith.
- **Zero trust.** Every service validates the Supabase JWT itself. The gateway forwards the `Authorization` header unchanged.
- **URL prefix = owning service.** Everything under `/api/reminders/**` belongs to Reminders; everything else under `/api/**` belongs to the monolith.
- **Two generated OpenAPI clients**, `generated/api` and `generated/reminders-api`. The alternative, a spec merged at the gateway, would put logic into the gateway.
- **TLS terminates at the edge.** Services drop `UseHttpsRedirection`.
- **The frontend uses relative URLs** (`BASE_PATH = ''`) through the Angular dev proxy, so local development is same-origin and needs no CORS.

## Consequences

- **Moving reminder endpoints changed their URLs.** The frontend had to switch clients.
- **`npm run generate:api` needs both services running.**
- **Auth configuration lives in `Yaam.ServiceDefaults`**, so it is not duplicated per service.
