# User Profile

**Date:** 2026-07-08
**Status:** Decided
**MVP fit:** In scope

## Summary

A structured, user-maintained profile built from CV parse and extended with custom fields, serving as the primary input for cover letter generation.

---

## User Stories

### Story 1: View profile
As a returning user,
I want to see all my profile data in one structured view,
so that I can verify what was extracted from my CV and understand what the AI will use.

**Acceptance Criteria:**
- Given I have uploaded a CV, when I navigate to my profile, then I see all extracted data grouped by section (contact info, work experience, education, skills, languages).
- Given my profile has custom fields, when I view my profile, then custom fields appear in a separate "Additional Info" section below the predefined sections.
- Given a field was not extracted from my CV, when I view my profile, then the field is shown as empty (not hidden), with a prompt to fill it in.

---

### Story 2: Edit predefined fields
As a returning user,
I want to edit any predefined profile field inline,
so that I can correct extraction errors or add information my CV didn't contain.

**Acceptance Criteria:**
- Given I am on my profile page, when I click a field value, then it becomes editable inline without a full-page reload.
- Given I edit a field and save, when the save completes, then the updated value is persisted and shown immediately.
- Given I edit a field and discard, when I press Escape or click Cancel, then the original value is restored with no change saved.
- Given a predefined field has a defined type (e.g., date, list, text), when I edit it, then the input control matches the field type (date picker, tag input, text area).

---

### Story 3: Add and manage custom fields
As a job seeker,
I want to add custom key-value fields to my profile (e.g., "Salary expectation: CHF 120k", "Preferred work style: hybrid"),
so that I can capture job-search-specific context that isn't part of a standard CV.

**Acceptance Criteria:**
- Given I am on my profile page, when I click "Add custom field", then I can enter a label and a text value.
- Given I have created a custom field, when I view my profile, then the custom field appears in the "Additional Info" section with its label and value.
- Given I have a custom field, when I click its label or value, then I can edit it inline.
- Given I have a custom field, when I click delete and confirm, then the field is removed from my profile and no longer used in cover letter generation.
- Given I add a custom field with an empty label or empty value, when I try to save, then I see a validation error and the field is not saved.

---

### Story 4: AI profile refinement (on demand)
As a returning user,
I want to start an AI-guided refinement session that asks me targeted questions about my experience and goals,
so that my profile becomes richer and more useful for generating personalized cover letters.

**Acceptance Criteria:**
- Given I am on my profile page, when I click "Refine with AI", then the AI asks me one question at a time based on gaps or shallow areas in my current profile.
- Given the AI asks a question and my answer maps to an existing predefined field, when I submit, then the AI shows the suggested update and I must confirm before it is saved.
- Given the AI asks a question and my answer does not map to any predefined field, when I submit, then a new custom field is created directly without requiring additional confirmation.
- Given the AI has no more useful questions, when I complete the session, then I see a summary of what was updated.
- Given I close the refinement session early, when I dismiss it, then all answers given so far are saved and the session can be resumed later.
- Given the AI provider is unavailable, when I click "Refine with AI", then I see a clear error message and my profile data is unchanged.

---

## Scope Guard

- **Story 4** depends on the AI adapter layer being implemented — do not build Story 4 until the `IAIProvider` interface is in place.
- **Custom fields (Story 3)** are referenced by the Cover Letter Generation feature — the data model must be finalized before that feature is built.
- **"Periodically asks questions"** from the MVP spec has been intentionally simplified to on-demand in Story 4. Automatic/scheduled AI prompting is out of scope for MVP (implies background jobs, notification system).
- **Story 4** implies session state (resume a refinement session) — keep this simple: store the last unanswered question, not a full session history.

---

## Decisions

### Predefined fields

Predefined fields are **strongly typed** on the `Profile` domain entity with real DB columns and child tables. Adding a new predefined field requires a model change + migration — acceptable cost for a solo project, and it gives full type safety and compile-time guarantees.

| Field | Storage | Notes |
|-------|---------|-------|
| `firstName`, `lastName` | columns | |
| `email`, `phone` | columns | |
| `location` | column | free text, city/country |
| `summary` | column | textarea |
| `workExperience[]` | child table | company, title, startDate, endDate, description |
| `education[]` | child table | institution, degree, field, startDate, endDate |
| `skills[]` | JSON array column | free-form strings |
| `languages[]` | child table | language, proficiency (A1–C2 or Beginner / Intermediate / Fluent / Native) |
| `certifications[]` | child table | name, issuer, date |
| `links[]` | child table | label, url |

### Custom fields

User-defined custom fields are stored as a **key-value JSON column** on the profile — they are genuinely dynamic (unknowable at compile time) and warrant the flexible storage. Plain text values only for MVP.

### Custom field value types
Plain text only for MVP. Typed values (number, date, list) are a future feature.

### AI refinement — predefined field updates
AI may suggest updates to existing predefined fields, but the user must explicitly confirm before they are saved. For information with no matching predefined field, AI creates a custom field directly without an extra confirmation step.
