# License Model

## Goal

Open source the core so others can self-host, while running a paid hosted service on top.

## Candidate Licenses

### AGPL-3.0 (working assumption)
- Copyleft: anyone who modifies and runs it as a service must open-source their changes
- Protects against competitors forking and running a competing hosted service without contributing back
- Standard model for open-core SaaS (Plausible, Gitlab, etc.)

### MIT / Apache 2.0
- Permissive: anyone can fork and build a commercial product
- Better for adoption and contributions, worse for protecting the hosted service

## Decision

Use **AGPL-3.0** for the open-source repo. Offer a **commercial license** for businesses that need to embed YAAM without AGPL obligations.

## To Do

- Add LICENSE file to repo root once decided
- Define what "commercial use" means in the context of self-hosting
