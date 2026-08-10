# CV Parsing Design

**Date:** 2026-08-10
**Status:** Approved
**MVP fit:** In scope

## Summary

A user uploads a PDF CV; the backend extracts text with PdfPig and sends it to an AI model (Ollama locally, provider TBD for production) via an `ICvParser` interface. The parsed result is returned to the frontend where the user reviews it in a section-by-section checklist with inline editing before applying it to their profile. On reupload the user chooses to either add selected items to their existing profile or replace everything.

---

## User Stories

### Story 1: Parse a CV for the first time
As a job seeker,
I want to upload my CV and have the fields extracted automatically,
so that I can seed my profile without typing everything manually.

**Acceptance Criteria:**
- Given I am on the profile page, when I click "Upload CV", then a file picker opens filtered to PDF only.
- Given I select a valid PDF (≤ 2 MB), when I confirm, then a loading state is shown while parsing runs.
- Given parsing completes, when I see the review screen, then each extracted section (Info, Work Experience, Education, Skills, Languages, Certifications) is shown with all items checked by default.
- Given I uncheck an item, when I save, then that item is not added to my profile.
- Given I edit a field inline before saving, when I save, then the edited value is what is stored.
- Given parsing completes, when a section (e.g. Certifications) has no extracted items, then that section is still shown with a notice "No certifications found in your CV" so the user knows the AI looked and found nothing.
- Given the PDF contains no extractable text (scanned), when parsing completes, then a clear error is shown and the user is prompted to try a text-based PDF.
- Given parsing fails for any other reason, when the error is shown, then the user can retry without re-selecting the file.

---

### Story 2: Reupload a CV
As a job seeker,
I want to reupload a new version of my CV,
so that I can update my profile as my experience changes.

**Acceptance Criteria:**
- Given I already have profile data and I upload a new CV, when parsing completes, then I am asked whether to "Add selected to profile" or "Replace entire profile".
- Given I choose "Add selected", when I save, then only the checked items are added; existing profile data is preserved.
- Given I choose "Replace entire profile", when I save, then all existing work experiences, education, languages, certifications, and skills are removed and replaced with the checked items from the new parse. Basic info (name, email, phone, location, summary) is overwritten only if the corresponding fields were extracted.
- Given I choose "Replace entire profile", when I confirm, then a confirmation dialog warns that existing profile data will be permanently replaced.

---

## Architecture

### Backend

#### `ICvParser` (use-case layer — `Yaam.UseCases/Common/Cv/`)

```csharp
public interface ICvParser
{
    Task<ParsedCvDto> ParseAsync(string text, CancellationToken cancellationToken);
}
```

Registered in DI. `OllamaCvParser` is the only implementation for now. Switching providers requires adding a new class and changing the DI registration — no other code changes.

#### `OllamaCvParser` (infrastructure layer — `Yaam.Infrastructure/Cv/`)

- Sends text to the Ollama HTTP API with a structured extraction prompt
- Requests JSON output matching the `ParsedCvDto` schema
- Model configured via `appsettings` (`Cv:OllamaBaseUrl`, `Cv:OllamaModel`)
- Recommended model: `llama3.1` or `mistral` (good instruction-following and JSON output)

#### `ParsedCvDto` (use-case layer)

```
ParsedCvDto
  Info:          ParsedInfoDto?        (firstName, lastName, email, phone, location, summary)
  WorkExperiences: ParsedWorkExperienceDto[]  (company, title, startDate, endDate?, description?)
  Educations:    ParsedEducationDto[]  (institution, degree?, fieldOfStudy?, startDate?, endDate?)
  Skills:        string[]
  Languages:     ParsedLanguageDto[]   (name, proficiency)
  Certifications: ParsedCertificationDto[]  (name, issuer?, date)
```

Dates are returned as strings in ISO format (`yyyy-MM-dd` or `yyyy-MM`); the frontend parses them for display and sends them back as-is.

#### `ParseCvCommand` (`Yaam.UseCases/Profile/Commands/`)

- Input: `byte[] PdfBytes`
- Uses PdfPig to extract text from the PDF
- If extracted text is fewer than 50 characters, throws a validation error (`ScannedOrEmptyPdfError`)
- Calls `ICvParser.ParseAsync(text)`
- Returns `ParsedCvDto`
- No profile mutations — purely a read/parse operation

#### `ApplyParsedCvCommand` (`Yaam.UseCases/Profile/Commands/`)

- Input: `ParsedCvDto SelectedItems`, `CvApplyMode Mode` (`Add` | `Replace`)
- `Add` mode: adds each item in `SelectedItems` to the profile using the existing `Add*` command handlers internally, or directly via the repository
- `Replace` mode: clears `WorkExperiences`, `Educations`, `Languages`, `Certifications`, `Skills` on the profile entity, then adds `SelectedItems`; overwrites `Info` fields only where the parsed value is non-null
- Returns `ProfileDto`

#### API endpoints (`Yaam.API/Profile/`)

| Method | Path | Body | Response |
|--------|------|------|----------|
| POST | `/profile/cv/parse` | multipart/form-data `file` | `ParsedCvViewModel` |
| POST | `/profile/cv/apply` | `ApplyCvInputModel` | `ProfileViewModel` |

`ApplyCvInputModel`:
```
ApplyCvInputModel
  SelectedItems: ParsedCvViewModel   (only checked/edited items sent by frontend)
  Mode: "add" | "replace"
```

`ParsedCvViewModel` wraps a `ParsedCvData` record. `ParsedCvData` is a shared type (defined in the API project) used both as the parse response payload and as the `SelectedItems` field in `ApplyCvInputModel`. Separating it avoids using a ViewModel as an input type.

```
ParsedCvData
  Info:           ParsedInfoData?
  WorkExperiences: ParsedWorkExperienceData[]
  Educations:     ParsedEducationData[]
  Skills:         string[]
  Languages:      ParsedLanguageData[]
  Certifications: ParsedCertificationData[]
```

---

### Frontend

#### Flow

```
Profile page
  → "Upload CV" button
  → [Upload step] file picker + upload button
  → loading spinner (parse in progress)
  → [Review step] section checklist + inline editing
      → (if profile has existing data) merge mode selector: "Add to profile" / "Replace profile"
      → confirm dialog if "Replace profile" chosen
  → POST /profile/cv/apply
  → profile page refreshes
```

#### Components

- **`cv-upload`** — file input (PDF, 2 MB max), triggers parse on submit, shows loading/error states
- **`cv-review`** — receives `ParsedCvDto` as input, renders sections; each item has a checkbox and inline-editable fields; emits the selected/edited `ParsedCvDto` on save; sections with no extracted items show an inline notice ("No [section] found in your CV") rather than being hidden — the user should know the AI looked and found nothing
- **`cv-merge-mode-selector`** — radio group shown only when the profile already has data; controls whether apply uses `add` or `replace` mode

The review component does not call any API itself — it emits upward and the parent page handles the `applyParsedCv` call.

---

## Scope Guard

- **PdfPig only** — text-based PDFs. Scanned PDFs return a validation error. Future enhancement: replace with Docling (see `future-features.md`).
- **No persistence of the raw parse result** — the parsed JSON is never stored on the server. It lives only in the HTTP response and then in Angular component state until the user confirms.
- **`ApplyParsedCvCommand` does not call the existing `Add*` command handlers** — it writes directly via `IProfileRepository` to avoid N mediator dispatches in a loop. The existing `Add*` commands remain for the per-item UI edit flows.
- **Basic info fields (name, email, phone) are overwritten on replace only if non-null in the parse result** — an incomplete CV should not blank out fields the user already filled manually.
- **File size validation** (2 MB) is enforced at the API layer before parsing begins.
- **Ollama setup** is a manual local dev step. The implementation plan includes a setup guide. No Ollama dependency exists in production until a production provider is chosen.
- **Skills** are stored as `string[]` on the profile — the AI returns them as a flat list and they are applied directly.
- **`LanguageProficiency` enum** values must be included in the Ollama prompt so the AI maps proficiency strings to valid enum members.
- **No onboarding trigger yet** — the upload entry point is the profile page only. The post-registration onboarding hook will be added when Account Management is implemented.

---

## Open Questions

- None blocking.

---

## Decisions

- **PDF extraction:** PdfPig (MIT, pure managed .NET). No native dependencies.
- **AI provider:** Ollama (local dev). Production provider deferred.
- **Parse result not persisted server-side** — keeps the backend stateless for MVP.
- **Two endpoints** (`/parse` and `/apply`) rather than one — separates the slow AI step from the profile mutation, making retries and the review UX clean.
- **Apply via repository directly** — avoids N mediator dispatches; consistent with how other batch profile operations work.
