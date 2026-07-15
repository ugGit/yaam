# ADR 004: Angular signal forms as the sole form API

**Date:** 2026-07-15
**Status:** Accepted

## Context

Angular 22 ships a stable signal-based forms API (`@angular/forms` — `form()`, `schema()`, `FormField`, `formRoot`) that integrates directly with the signal-first component model the project already uses (`input()`, `signal()`, `resource()`).

The codebase contains two competing approaches that must be resolved:

- `application-form.component` uses the classic `ReactiveFormsModule` API (`FormBuilder`, `FormGroup`, `FormControl`).
- `application-notes.component` uses raw `WritableSignal` values bound to textareas via manual `[value]` / `(input)` event pairs, bypassing any form abstraction.

Neither approach fits the signal-first model; both require extra boilerplate (`$any()` casts, explicit `ngOnInit` wiring, `Validators` imports) and do not compose with the rest of the signal graph.

## Decision

**Angular signal forms (`form()` + `schema()`) are the only permitted form API.** Specifically:

- Form state is held in a `WritableSignal<TModel>` (plain object).
- `form(model, schema(...))` produces a `FieldTree` that is passed to the template via `[formRoot]` on the `<form>` element and `[formField]` on each input.
- Validation rules (`required()`, `maxLength()`, etc.) are declared once in the `schema` callback; they are not duplicated in the template.
- `ReactiveFormsModule`, `FormBuilder`, `FormGroup`, and `FormControl` are forbidden in new code and must be removed from existing components.
- Manual `[value]` / `(input)` signal bindings on form inputs are forbidden; use `[formField]` instead.

## Consequences

- Form state, validation, and submission live entirely in the signal graph — no lifecycle hooks needed to wire up a `FormGroup`.
- Components that previously used `ReactiveFormsModule` must be converted (starting with `application-form.component`).
- Components that used raw signal + manual event binding must be converted (starting with `application-notes.component`).
- Error display uses `field.errors()` signals directly, replacing the `control.errors` / `fieldError()` helper pattern.
- The `FormsModule` and `ReactiveFormsModule` imports are removed from all standalone component `imports` arrays as conversions land.
