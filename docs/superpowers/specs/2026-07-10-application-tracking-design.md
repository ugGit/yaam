# Application Tracking — Feature Design

**Date:** 2026-07-10
**Status:** Approved
**Branch:** `feat/application-tracking`
**Spec:** `docs/specs/2026-07-08-application-tracking.md`

---

## Scope

Stories 1–4 and 6 from the application tracking spec: full CRUD for applications, status management, and per-application notes. Story 5 (CV linking) is deferred until the CV Parsing feature is complete.

Out of scope for this plan: authentication/user isolation, reminders cascade delete, cover letter linking.

---

## Domain Model

### `Application` (inherits `Entity`)

| Field | Type | Required |
|---|---|---|
| `CompanyName` | `string` | yes |
| `Role` | `string` | yes |
| `DateApplied` | `DateOnly?` | required unless status is Draft |
| `Status` | `ApplicationStatus` | yes — defaults to Draft |
| `ContactName` | `string?` | no |
| `ContactEmail` | `string?` | no |
| `ContactPhone` | `string?` | no |
| `JobPosting` | `string?` | no — stored as plain text / Markdown |
| `Notes` | `ICollection<ApplicationNote>` | navigation |

### `ApplicationNote` (inherits `Entity`)

| Field | Type |
|---|---|
| `ApplicationId` | `Guid` FK |
| `Body` | `string` |

`Entity` base provides `Id`, `CreatedAt`, `UpdatedAt` for both. `AppDbContext.SaveChangesAsync` already stamps `UpdatedAt` on every `Modified` entry.

### `ApplicationStatus` enum

`Draft → Applied → InterviewScheduled → Interviewed → OfferReceived → Accepted | Rejected | Withdrawn`

Draft represents a role being considered before submitting. `DateApplied` is optional only in this state.

---

## Backend

### Layer responsibilities

- **Domain:** `Application`, `ApplicationNote`, `ApplicationStatus`, `IApplicationRepository`
- **Application:** CQRS handlers + validators, explicit request/response DTOs, no AutoMapper
- **Infrastructure:** `ApplicationRepository` (EF Core), `ApplicationConfiguration` (Fluent API mapping)
- **API:** `ApplicationsController`, `ApplicationNotesController` — thin HTTP in/out only

### CQRS operations

| Operation | Type | Notes |
|---|---|---|
| `GetApplicationsQuery` | Query | accepts `status?`, `sort`, `order`; returns `ApplicationSummaryDto[]` |
| `GetApplicationByIdQuery` | Query | returns `ApplicationDto` incl. notes; 404 if not found |
| `CreateApplicationCommand` | Command | returns `ApplicationDto`; 201 |
| `UpdateApplicationCommand` | Command | full field update; returns `ApplicationDto` |
| `UpdateApplicationStatusCommand` | Command | status-only patch; immediate save |
| `DeleteApplicationCommand` | Command | cascades to notes; 204 |
| `AddApplicationNoteCommand` | Command | returns `ApplicationNoteDto`; 201 |
| `UpdateApplicationNoteCommand` | Command | returns `ApplicationNoteDto` |
| `DeleteApplicationNoteCommand` | Command | 204 |

### API endpoints

```
GET    /api/applications                         → ApplicationSummaryDto[]
POST   /api/applications                         → 201 ApplicationDto
GET    /api/applications/{id}                    → ApplicationDto (+ notes)
PUT    /api/applications/{id}                    → 200 ApplicationDto
PATCH  /api/applications/{id}/status             → 200 ApplicationDto
DELETE /api/applications/{id}                    → 204

POST   /api/applications/{id}/notes              → 201 ApplicationNoteDto
PUT    /api/applications/{id}/notes/{noteId}     → 200 ApplicationNoteDto
DELETE /api/applications/{id}/notes/{noteId}     → 204
```

**Query params on `GET /api/applications`:**
- `status` — optional enum value; omit for all
- `sort` — `dateApplied` (default) | `companyName` | `status`
- `order` — `desc` (default) | `asc`

### DTOs

`ApplicationSummaryDto` — list row, no job posting, no notes:
`id, companyName, role, dateApplied, status`

`ApplicationDto` — full detail:
`id, companyName, role, dateApplied, status, contactName?, contactEmail?, contactPhone?, jobPosting?, createdAt, updatedAt, notes: ApplicationNoteDto[]`

`ApplicationNoteDto`:
`id, body, createdAt, updatedAt`

All error responses use RFC 9457 ProblemDetails (already wired via `GlobalExceptionHandler`).

---

## Frontend

### API client generation

The Angular HTTP service layer is **generated** from the backend's OpenAPI document using `openapi-generator-cli` with the Angular template. This is set up as the first task of the plan.

```json
// openapitools.json at repo root
{
  "generator-cli": {
    "version": "7.x",
    "generators": {
      "yaam-api": {
        "generatorName": "typescript-angular",
        "output": "frontend/src/app/generated/api",
        "inputSpec": "http://localhost:5001/swagger/v1/swagger.json",
        "additionalProperties": {
          "ngVersion": "22",
          "providedIn": "root",
          "withInterfaces": true
        }
      }
    }
  }
}
```

Generated services emit `Observable<T>`. Handwritten code bridges to Signals via `rxResource()` and `toSignal()`. When generators natively emit Signals, the handwritten bridge layer drops away with no further changes to components.

### Feature structure

```
frontend/src/app/features/applications/
├── applications.routes.ts
├── pages/
│   ├── application-list/
│   │   ├── application-list.component.ts
│   │   └── application-list.component.html
│   └── application-detail/
│       ├── application-detail.component.ts
│       └── application-detail.component.html
├── components/
│   ├── application-form/
│   │   ├── application-form.component.ts    ← shared for create + edit
│   │   └── application-form.component.html
│   └── application-notes/
│       ├── application-notes.component.ts
│       └── application-notes.component.html
└── models/
    ├── application.model.ts                  ← re-exports + local types
    └── application-note.model.ts
```

### Routing

```
/applications          → ApplicationListComponent  (lazy)
/applications/new      → ApplicationDetailComponent (create mode)
/applications/:id      → ApplicationDetailComponent (view + edit mode)
```

### State (Signals)

`resource()` (not `rxResource`) is used throughout. The generated client emits Observables; `firstValueFrom()` converts them to Promises inside each loader. `toSignal()` is used to convert route params and other Observables into Signals that drive resource requests.

**`ApplicationListComponent`**
```typescript
filterState = signal({ status: '', sort: 'dateApplied', order: 'desc' });

applications = resource({
  request: () => this.filterState(),
  loader: ({ request }) => firstValueFrom(
    this.apiService.getApplications(request)
  )
});
```

**`ApplicationDetailComponent`**
```typescript
readonly applicationId = toSignal(this.route.paramMap.pipe(map(p => p.get('id'))));

application = resource({
  request: () => this.applicationId(),
  loader: ({ request }) => firstValueFrom(
    this.apiService.getApplication(request!)
  )
});
editMode = signal(false);
```

**`ApplicationNotesComponent`** (receives `applicationId` as input)
```typescript
notes = signal<ApplicationNoteDto[]>([]);
editingNoteId = signal<string | null>(null);
```

### UI (daisyUI + Tailwind)

| Element | daisyUI component |
|---|---|
| Page layout | `container` + Tailwind grid |
| Application list | `table table-zebra` inside `card` |
| Status badge | `badge` with per-status colour class |
| Status filter | `select` or `tabs` |
| Create/edit form | `card` + `form-control` + `label` + `input` / `textarea` |
| Edit / Save / Cancel | `btn btn-primary` / `btn btn-ghost` |
| Delete confirmation | `modal` + `btn btn-error` |
| Notes section | `card` in detail; each note as a `card-body` row |
| Note edit | inline `textarea` replacing the note text |
| Empty state | centred `text-base-content/50` with icon |
| Validation errors | `label-text-alt text-error` below each field |
| Loading | `loading loading-spinner` |
| Error state | `alert alert-error` |

---

## Error Handling

- `GlobalExceptionHandler` maps `ValidationException` → 400 ProblemDetails with `errors` extension, unexpected → 500
- Angular `HttpInterceptor` extracts `errors` from ProblemDetails and pipes them to reactive form controls
- `rxResource` error state (`status() === 'error'`) renders `alert alert-error` in place of the content
- 404 on detail load navigates to `/applications` and shows a toast notification

---

## Testing

| Layer | Scope | Tool |
|---|---|---|
| Unit | Command validators (required fields, DateApplied conditionality) | xUnit |
| Unit | `GetApplicationsQuery` filtering + sorting logic | xUnit, in-memory list |
| Integration | All 9 endpoints — happy path + 400/404 responses | xUnit + `WebApplicationFactory` + real PostgreSQL |
| Angular | Deferred — `rxResource` + generated client pattern still evolving | — |

---

## Future notes

- **`DateApplied` auto-set:** when a user changes status from Draft to Applied, `DateApplied` should be automatically set to today's date. The user should be prompted to confirm or override the suggested date before saving. Not in scope for this plan — requires a UX decision on the confirmation flow.

---

## Architecture decisions referenced

- [ADR 003](../../../architecture/decisions/003-application-list-server-driven.md) — server-driven filtering and sorting for all list endpoints
- [ADR 001](../../../architecture/decisions/001-profile-field-registry.md) — typed entity model pattern (same approach applied here)
