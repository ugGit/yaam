# CV Parsing

**Date:** 2026-07-08
**Status:** Decided
**MVP fit:** In scope

## Summary

A job seeker uploads their CV file and the AI extracts structured data into the typed profile model, eliminating manual data entry as the starting point of the core loop.

---

## User Stories

### Story 1: Upload a CV file
As a job seeker,
I want to upload my CV as a PDF or Word document,
so that I don't have to enter my profile data from scratch.

**Acceptance Criteria:**
- Given I am on the onboarding or profile page, when I click "Upload CV", then I can select a PDF (.pdf) or Word (.docx) file from my device.
- Given I select a file, when the upload starts, then I see a progress indicator.
- Given the upload completes, when the file is received by the server, then the original file is stored and processing begins automatically.
- Given I select a file larger than 2 MB, when I try to upload, then I see a validation error before the upload starts and the file is not sent.
- Given I select a file type other than .pdf, when I try to upload, then I see a clear error message stating only PDF files are accepted.

---

### Story 2: Review extracted data before saving
As a job seeker,
I want to review all data extracted from my CV before it is saved to my profile,
so that I can catch extraction errors before they affect my cover letters.

**Acceptance Criteria:**
- Given extraction completes, when I am shown the review screen, then all extracted fields are displayed grouped by section (contact, experience, education, skills, languages, certifications, links).
- Given a field was extracted, when I review it, then I can edit the value inline before confirming.
- Given a field could not be extracted, when I review it, then it is shown as empty with a visual indicator that it needs manual input.
- Given I confirm the review, when I click "Save to profile", then all reviewed fields are written to my profile and I am navigated to the profile page.
- Given I reject the extraction, when I click "Discard", then no changes are made to my profile and the uploaded file is deleted.

---

### Story 3: Handle extraction failures gracefully
As a job seeker,
I want to be clearly informed when the AI could not extract my CV data,
so that I know what to do next without losing my uploaded file.

**Acceptance Criteria:**
- Given the AI extraction fails entirely, when the error occurs, then I see a message explaining that extraction failed and offering me two options: retry or enter my profile manually.
- Given the AI extraction partially succeeds, when the review screen is shown, then successfully extracted fields are pre-filled and failed fields are shown empty with a "Could not extract" label.
- Given the AI provider is unavailable, when I upload my CV, then I see a message that AI extraction is temporarily unavailable and I am offered the option to fill in my profile manually instead.
- Given extraction fails, when I retry, then the original uploaded file is reused — I do not need to re-select it.

---

### Story 4: Re-upload a new CV version
As a returning user,
I want to upload a new version of my CV,
so that my profile reflects my current experience without manually updating every field.

**Acceptance Criteria:**
- Given I already have a profile, when I upload a new CV, then I am shown a per-field diff: each field displays its current value alongside the value extracted from the new CV.
- Given the diff is shown for a scalar field (e.g., location, summary), when I review it, then I can accept the new value or keep the current one.
- Given the diff is shown for a list field (e.g., workExperience, education), when I review it, then:
  - Entries present in the new CV but not in the current profile are shown as additions (accept/reject per entry).
  - Entries present in the current profile but absent from the new CV are shown as removals, marked for removal by default, and accompanied by an explanatory note ("Not found in your new CV — will be removed unless you keep it") — I can choose to keep them.
  - Entries present in both are shown as unchanged and require no action.
- Given I confirm the diff, when I click "Apply changes", then accepted additions are added, confirmed removals are deleted, and rejected changes keep their current values.
- Given I re-upload, when the process completes, then the new CV file replaces the previous stored version.
- Given I have applications that reference a specific CV version, when I re-upload, then existing application links are not affected — they still point to the previous CV file.

---

## Scope Guard

- **Stories 1–3** depend on the AI adapter (`IAIProvider.ParseCV()`) being implemented before CV parsing can work end-to-end. The upload and review UI can be built first with a stub extractor.
- **Story 4 (diff view)** is the most complex story here — it implies a comparison UI and per-field accept/reject logic. If time is tight, simplify to: re-upload overwrites all fields and the user edits from there.
- **File storage** is a dependency for all stories — the original CV file must be stored somewhere (local filesystem in dev, S3-compatible in prod). This needs to be set up before Story 1 can be completed end-to-end.
- **Story 4** references CV versions linked to applications — this couples CV Parsing to the Application Tracking feature. Do not build Story 4 until Application Tracking is in place.

---

## Decisions

- **File format:** PDF only (.pdf). Word support (.docx) is a future feature.
- **File size limit:** 2 MB maximum.
- **Re-upload strategy:** Per-field diff with per-entry control on list fields. List entries absent from the new CV are flagged for removal by default; user can choose to keep them.
