# YAAM — Claude Code Guidelines

## Architecture reference

For architectural decisions, patterns, and layer structure consult `docs/architecture/` before writing code.

## Generated files — never edit manually

`frontend/src/app/generated/` is owned by the OpenAPI generator. Never edit any file under this path by hand.

To regenerate after adding or changing backend endpoints:
1. Start the backend: `dotnet run --project backend/Yaam.API`
2. From `frontend/`: `npm run generate:api`

The generator pulls the OpenAPI spec from the backend's live `/openapi/v1.json` endpoint and writes the Angular service + model files to `src/app/generated/api/`. All component imports referencing the generated path must be updated after regeneration if the output structure changes.

## Frontend — verify changes in the browser

After implementing any frontend feature or fix, invoke the `verify` skill to confirm the change works at runtime. The `verifier-browser` skill provides the launch recipe; `playwright-cli` (installed) is used for browser interaction.

## Frontend — use the daisyUI skill

Before writing any frontend HTML or component templates, invoke the `daisyui` skill. daisyUI is the mandatory component library for all UI in this project — never use raw Tailwind utility classes for components that daisyUI covers (buttons, badges, cards, modals, inputs, alerts, etc.).

## Frontend — Angular signal forms only

Use `form()` / `schema()` / `[formField]` from `@angular/forms`. Never use `ReactiveFormsModule`, `FormBuilder`, `FormGroup`, or `FormControl`. Never bind form inputs manually with `[value]` / `(input)`.

## Backend naming

- **Handler parameters:** `command` for `IRequestHandler<TCommand>`, `query` for `IRequestHandler<TQuery>`. Never `request`.
- **No default parameters** unless the caller cannot supply the value. Omit `= default`, `= null`, `= 0`, etc. — callers should be explicit.
- **No abbreviations** in parameter or variable names. Write `cancellationToken`, not `ct`; `application`, not `app`; `exception`, not `ex`.
- **No type aliases** unless genuinely needed to resolve an ambiguity. If an alias is required, use the full class name (e.g., `using Application = Yaam.Domain.Entities.Application`). Prefer restructuring over aliases.
- **`mediator.Send` formatting:** always put the command/query on its own line and `cancellationToken` on a third line — no single-line calls regardless of length:
  ```csharp
  var result = await mediator.Send(
      new MyCommand(arg1, arg2),
      cancellationToken);
  ```

## Backend controllers — input models

Controllers never bind HTTP request bodies directly to Application layer commands. Every endpoint that accepts a body uses a dedicated input model named `<Verb><Resource>InputModel` (e.g., `CreateApplicationInputModel`), defined in the same file as the controller. The controller maps the input model to the command explicitly.

## Backend controllers — response naming (ViewModel suffix)

Controller responses use a two-layer type model:

- **Use-case layer** (`Yaam.UseCases/<Feature>/Dtos/`): `*Dto` records are the output of MediatR handlers. They are internal to the use-case layer and must not be returned directly from a controller.
- **API layer** (`Yaam.API/<Feature>/`): `*ViewModel` records are what controllers return. An API-layer mapper (e.g. `ApplicationViewModelMapper`, `ProfileViewModelMapper`) converts `*Dto` → `*ViewModel` inside the controller method.

This separation means the API shape can evolve independently of the use-case internals.
