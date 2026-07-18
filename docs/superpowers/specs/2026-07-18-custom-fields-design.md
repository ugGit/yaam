# User Profile — Story 3: Add and Manage Custom Fields

**Date:** 2026-07-18
**Status:** Approved
**Branch:** feat/user-profile-story-3

## Summary

Implements CRUD for user-defined key-value custom fields on the profile, allowing users to capture job-search-specific context that isn't part of a standard CV (e.g. "Salary expectation: CHF 120k", "Preferred work style: hybrid").

---

## Scope

Covers the full vertical slice: three backend commands, three controller endpoints, integration tests, API client regeneration, and the updated frontend section + new modal component.

Story 4 (AI profile refinement) is explicitly out of scope and remains gated on `IAIProvider`.

---

## Architecture

### Backend

Three MediatR commands under `Yaam.UseCases/Profile/Commands/CustomFields/`:

| Command | Validates | Returns |
|---------|-----------|---------|
| `AddCustomFieldCommand(Label, Value)` | label required, max 250; value required, max 1000 | `CustomFieldDto` |
| `UpdateCustomFieldCommand(Id, Label, Value)` | same as Add | `CustomFieldDto` |
| `DeleteCustomFieldCommand(Id)` | id exists on profile | void |

All three use `IProfileRepository.GetOrCreateAsync` + `UpdateAsync`. Add/Update/Delete throw `NotFoundException` if the id is not found on the user's profile.

Controller additions to `ProfileController`:

| Method | Route | Input Model | Success |
|--------|-------|-------------|---------|
| `POST` | `/api/profile/custom-fields` | `CustomFieldInputModel(Label, Value)` | 200 `CustomFieldViewModel` |
| `PUT` | `/api/profile/custom-fields/{id}` | `CustomFieldInputModel(Label, Value)` | 200 `CustomFieldViewModel` |
| `DELETE` | `/api/profile/custom-fields/{id}` | — | 204 |

`CustomFieldInputModel` is a record defined in `ProfileController.cs`, consistent with all other input models in this file.

Controllers return `CustomFieldViewModel` (not `CustomFieldDto`). `ProfileViewModelMapper.ToViewModel(CustomFieldDto)` already exists on the `refactor/dto-to-viewmodel` branch — controllers call it to map the use-case output before returning.

### Frontend

Two components follow the `ProfileLinkSectionComponent` / `ProfileLinkModalComponent` pattern exactly:

**`CustomFieldModalComponent`**
- Inputs: `item: CustomFieldViewModel | null` (null = add mode, set = edit mode)
- Outputs: `saved: CustomFieldFormData`, `dismissed: void`
- Uses `linkedSignal` to sync form model from `item` input
- Uses `form()` + `[formField]` with `required` validators on both fields
- Label: single-line text input, max 250 chars
- Value: textarea, max 1000 chars
- Exposes `show()` method; section calls it via `viewChild`

**`CustomFieldsSectionComponent`** (replaces current read-only component)
- Inputs: `fields: CustomFieldViewModel[]` (unchanged from Story 1, type name updated)
- Outputs: `changed: ProfileViewModel` (new — emits updated profile after every mutation)
- Signals: `editingItem: CustomFieldDto | null`, `deletingId: string | null`
- Add/edit mutate via `ProfileService` (generated), then re-fetch full profile and emit `changed`
- Delete is two-step in-list: first click shows Confirm/Cancel; confirm calls `deleteCustomField`

---

## Data Flow

**Add:** "+ Add" button → `editingItem.set(null)` → `modal.show()` → user fills label + value → `submit()` validates → `saved` emits → section calls `addCustomField` → re-fetches profile → `changed.emit(profile)`

**Edit:** "Edit" row button → `editingItem.set(item)` → `modal.show()` → `linkedSignal` pre-populates fields → user saves → section calls `updateCustomField(id, payload)` → re-fetches → `changed.emit(profile)`

**Delete:** "Delete" row button → `deletingId.set(id)` → row shows Confirm + Cancel → confirm → `deleteCustomField(id)` → re-fetches → `changed.emit(profile)` → `finally` resets `deletingId`

---

## Error Handling

- **Client-side validation:** `form()` catches empty label or empty value before any API call; `[formField]` renders inline error messages in the modal.
- **API failures:** caught in `onSaved()` try/catch in the section component; error state shown inline (consistent with other sections).
- **Delete failure:** `finally` block always resets `deletingId` so the UI does not lock up.
- **Not found:** backend returns 404 → frontend shows generic save error.

---

## Testing

**Backend integration tests** (added to `ProfileEndpointsTests.cs`):
- `CustomFieldCrudFlow` — add → update → delete → verify gone
- `POST_CustomField_Returns400_WhenLabelEmpty`
- `POST_CustomField_Returns400_WhenValueEmpty`

**Manual verification** — invoke the `verify` skill after frontend implementation. Checklist:
1. Add with blank label → inline validation error, no API call
2. Add with blank value → inline validation error, no API call
3. Add valid → field appears in list, modal closes
4. Edit → modal pre-populated with current values
5. Edit, clear label → validation error, field unchanged
6. Edit, change value → list updates
7. Delete first click → Confirm + Cancel appear in row
8. Delete cancel → row returns to normal, nothing deleted
9. Delete confirm → field removed from list

---

## Files

**Create:**
```
backend/Yaam.UseCases/Profile/Commands/CustomFields/AddCustomFieldCommand.cs
backend/Yaam.UseCases/Profile/Commands/CustomFields/UpdateCustomFieldCommand.cs
backend/Yaam.UseCases/Profile/Commands/CustomFields/DeleteCustomFieldCommand.cs
frontend/src/app/features/profile/components/custom-field-modal/custom-field-modal.component.ts
frontend/src/app/features/profile/components/custom-field-modal/custom-field-modal.component.html
```

**Modify:**
```
backend/Yaam.API/Controllers/ProfileController.cs
backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.ts
frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.html
frontend/src/app/generated/api/  (regenerated — never edit manually)
```

---

## Constraints

- All backend conventions from CLAUDE.md apply: handler parameter named `command`, `cancellationToken` never `= default`, `mediator.Send` three-line format, no type aliases.
- Controllers return `*ViewModel` (never `*Dto` directly). Use `ProfileViewModelMapper.ToViewModel(dto)` inside controller actions.
- `frontend/src/app/generated/` is never edited manually; regenerate after backend endpoints are added.
- daisyUI skill must be invoked before writing any component HTML.
- Angular signal forms only: `form()` / `[formField]`. No `ReactiveFormsModule`, `FormBuilder`, or `FormGroup`.
- Branch based on `origin/refactor/dto-to-viewmodel` (not main) — `CustomFieldViewModel` and its mapper already exist there.
