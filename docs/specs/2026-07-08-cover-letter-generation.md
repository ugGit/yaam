# Cover Letter Generation

**Date:** 2026-07-08
**Status:** Decided
**MVP fit:** In scope

## Summary

The AI generates a cover letter draft for a specific application using the job seeker's structured profile, the stored job posting, and optional extra context they provide — producing a strong starting point that the user reviews, edits, and saves linked to that application.

---

## User Stories

### Story 1: Generate a cover letter for an application
As a job seeker,
I want to generate a cover letter draft for a specific application using my profile and the stored job posting,
so that I have a strong, relevant starting point without writing from scratch.

**Acceptance Criteria:**
- Given I am viewing an application that has a job posting stored, when I click "Generate cover letter", then the AI generates a draft using my full profile and the job posting text.
- Given the generation starts, when it is in progress, then I see a loading indicator — generation may take several seconds.
- Given generation completes, when the draft is ready, then I am shown the full text in a readable view before any editing.
- Given the application has no job posting stored, when I try to generate, then I see a message prompting me to add the job posting first, and generation does not start.
- Given the AI provider is unavailable, when I try to generate, then I see a clear error message and no partial draft is shown.

---

### Story 2: Provide extra context before generating
As a job seeker,
I want to give the AI additional instructions before generating (e.g., "emphasise my leadership experience", "keep it under 300 words", "formal tone"),
so that the output is better tailored to this specific application without me having to rewrite it afterward.

**Acceptance Criteria:**
- Given I click "Generate cover letter", when the generation flow opens, then I am shown an optional free-text field labelled "Additional instructions for the AI" before generation starts.
- Given I leave the extra context field empty, when I proceed, then generation runs with profile + job posting only — no error.
- Given I fill in extra context and generate, when the draft is produced, then the output reflects my instructions (e.g., specified tone, length, emphasis).
- Given I have previously generated a cover letter for this application, when I generate again, then my previous extra context is pre-filled so I can reuse or adjust it.

---

### Story 3: Review and edit the generated draft
As a job seeker,
I want to read and edit the generated cover letter in a plain text editor,
so that I can refine the output before saving or using it.

**Acceptance Criteria:**
- Given a draft has been generated, when I view it, then the full text is shown in an editable plain text area.
- Given I edit the text, when I click "Save", then the edited version is saved as the current cover letter for this application.
- Given I edit the text and click "Discard changes", when I confirm, then the text reverts to the last saved version.
- Given I save an edited draft, when I view the cover letter history for this application, then both the original AI-generated version and the edited version are stored separately.

---

### Story 4: Regenerate to get a new version
As a job seeker,
I want to request a new AI-generated draft for the same application,
so that I can compare versions and pick the strongest one.

**Acceptance Criteria:**
- Given I am viewing a saved cover letter, when I click "Regenerate", then the extra context field is pre-filled with the previous value and I can adjust it before regenerating.
- Given I confirm regeneration, when the new draft is ready, then it is shown as a new version — the previous version is not overwritten.
- Given I have multiple versions, when I view the cover letter history, then all versions are listed with their generation timestamp and whether they were AI-generated or manually edited.
- Given I view an older version, when I click "Restore this version", then it becomes the current cover letter for this application.

---

### Story 5: Link cover letter to application
As a job seeker,
I want the generated cover letter to be automatically linked to the application it was created for,
so that I always know which cover letter I used for which application.

**Acceptance Criteria:**
- Given a cover letter is saved, when I view the application it belongs to, then I see a "Cover letter" section showing the current version with a link to open it.
- Given I have multiple cover letter versions for one application, when I view the application, then the most recently saved version is shown as the active one, with a link to view the full history.
- Given I delete an application, when I confirm deletion, then all linked cover letters and their version history are also deleted.

---

### Story 6: Upload reference cover letters to preserve writing style
As a job seeker,
I want to upload examples of cover letters I have written before,
so that the AI generates new letters in my own voice rather than a generic style.

**Acceptance Criteria:**
- Given I navigate to my profile, when I click "Add reference cover letter", then I can paste in the plain text of a cover letter I have previously written.
- Given I have saved at least one reference cover letter, when I view my profile, then all reference letters are listed with a label (e.g., "Reference 1") and a preview of the first 100 characters.
- Given I have saved reference cover letters, when the AI generates a new cover letter, then the reference texts are included in the generation context and the output reflects my writing style.
- Given I want to remove a reference cover letter, when I click delete and confirm, then it is removed and no longer used in future generations.
- Given I have no reference cover letters saved, when I generate a cover letter, then generation proceeds normally without style reference — no error.

---

## Scope Guard

- All stories depend on **User Profile** and **Application Tracking** being complete — generation requires a profile, and cover letters are always scoped to an application.
- **Story 6 (reference letters)** stores plain text only — no PDF parsing for reference letters. The user pastes text directly. This keeps the feature self-contained with no additional file handling infrastructure.
- All stories depend on the **AI adapter** (`IAIProvider.GenerateCoverLetter()`) being implemented.
- **Plain text only** for MVP — no Word, LaTeX, or template upload. The text area is a standard `<textarea>`, not a rich text editor.
- **Story 4 version history** introduces a `CoverLetterVersion` entity — plan for this in the data model from the start, even if version restore UI (Story 4) is built last.
- Cover letter generation without a stored job posting is explicitly blocked (Story 1) — do not add a fallback that generates from profile alone, as output quality would be too low to be useful.

---

## Decisions

- **Extra context:** Stored per application and pre-filled on each generation, so the user can tweak and regenerate for different results without re-entering instructions.
- **Version labels:** Timestamp only (e.g., "2026-07-08 14:32").

## Extensibility

Cover letter generation must be designed for easy extension as personalisation inputs grow (see `docs/specs/future-features.md` — Heavily Personalized Cover Letters). The key constraint: adding a new input source (e.g., deep personal profile, interview notes) must not require changing the `IAIProvider` interface signature.

The generation input is modelled as a single composable context object:

```
CoverLetterContext
  ├── profile                  (always required — typed Profile entity)
  ├── jobPosting               (always required — stored Markdown text)
  ├── userInstructions         (optional — stored per application)
  ├── referenceCoverLetters    (optional — list of user-uploaded style examples, stored on profile)
  └── [future inputs]          (e.g., deepProfile, style, interviewProtocol — added without breaking the interface)
```

`IAIProvider.GenerateCoverLetter(context: CoverLetterContext)` takes this object as a single parameter. New context fields are added to the object; the method signature stays stable. See ADR 002.
