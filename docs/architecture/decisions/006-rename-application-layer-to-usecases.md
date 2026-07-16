# ADR 006: Rename Application Layer to UseCases

## Status

Accepted

## Context

The backend project previously had a layer named `Yaam.Application` following the common Clean Architecture naming convention. However, the name `Application` conflicted with the `Yaam.Domain.Entities.Application` entity type: C# resolved bare `Application` in any `Yaam.*` namespace to the `Yaam.Application` project namespace (CS0118 — used as a type but is a namespace). This forced every file that referenced the entity to use the fully-qualified `global::Yaam.Domain.Entities.Application` form, which is verbose and unusual.

## Decision

Rename the layer from `Yaam.Application` → `Yaam.UseCases`.

- **Project:** `backend/Yaam.Application/` → `backend/Yaam.UseCases/`
- **Assembly / csproj:** `Yaam.Application.csproj` → `Yaam.UseCases.csproj`
- **Root namespace:** `Yaam.Application` → `Yaam.UseCases`
- All sub-namespaces follow: e.g., `Yaam.UseCases.Applications.Commands`

## Consequences

- All `using Yaam.Application.*` imports replaced with `using Yaam.UseCases.*` across the codebase.
- All `global::Yaam.Domain.Entities.Application` qualifiers removed; bare `Application` now resolves correctly with a standard `using Yaam.Domain.Entities;` directive.
- The name `UseCases` aligns with Clean Architecture literature (use-case layer / application services layer) and is unambiguous within the `Yaam.*` namespace tree.
- `Yaam.Infrastructure` and `Yaam.API` no longer hold type-alias workarounds (`DomainApp`, etc.).
