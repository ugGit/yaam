# ADR 005: API layer input models

**Date:** 2026-07-15
**Status:** Accepted

## Context

The API layer should not bind HTTP request bodies directly to Application layer commands. Doing so couples HTTP serialization to the command's constructor — any change to the command (adding an internal field, changing a type) risks silently altering the public API contract.

## Decision

Every HTTP endpoint that accepts a request body uses a dedicated **input model** in the API layer. Input models:
- Are named `<Verb><Resource>InputModel` (e.g., `CreateApplicationInputModel`, `UpdateApplicationNoteInputModel`).
- Live in the same file as the controller that uses them (defined at the bottom of the file as `record` types).
- Are mapped to commands/queries explicitly inside the controller action.

The API layer is the only place where input models exist. They do not flow into Application or Domain layers.

## Consequences

- The HTTP surface is decoupled from command signatures.
- Renaming or restructuring a command does not require changing the API contract.
- Controllers grow slightly (mapping code), but the mapping is intentional and visible.
