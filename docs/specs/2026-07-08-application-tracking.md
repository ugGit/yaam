# Application Tracking

**Date:** 2026-07-08
**Status:** Decided
**MVP fit:** In scope

## Summary

A job seeker logs each job application with its key details — company, role, contact, status, job posting, notes, and linked CV/cover letter — so they have one place to track where they applied, with what, and what happened.

---

## User Stories

### Story 1: Log a new application
As a job seeker,
I want to create a new application record with the key details of a job I applied to,
so that I have a structured record I can refer back to and act on.

**Acceptance Criteria:**
- Given I am on the applications page, when I click "Add application", then a form opens with the following fields: company name, role/job title, contact person (name, email, phone), job posting (text area stored as Markdown), date applied, status, and personal notes.
- Given I submit the form, when all required fields are filled (company name, role, date applied, status), then the application is saved and appears in my applications list.
- Given I submit the form with a required field empty, when I try to save, then I see a field-level validation error and the form is not submitted.
- Given I paste a job posting into the text area, when I save, then the full text is stored as Markdown so it remains readable even if the original posting is taken down.

---

### Story 2: View and manage the applications list
As a job seeker,
I want to see all my applications in a list with their current status,
so that I can quickly understand where things stand across all my active applications.

**Acceptance Criteria:**
- Given I have at least one application, when I navigate to the applications page, then I see a list of all applications showing company name, role, date applied, and current status.
- Given I have multiple applications, when I view the list, then applications are sorted by date applied descending by default.
- Given I want to filter, when I select a status filter, then only applications with that status are shown.
- Given I click an application in the list, when it opens, then I see the full detail view with all stored fields.

---

### Story 3: Update application status
As a job seeker,
I want to update the status of an application as it progresses,
so that my list always reflects the current state of each application.

**Acceptance Criteria:**
- Given I am viewing an application, when I change the status, then the new status is saved immediately without requiring a separate save action.
- Given the available statuses are: Applied, Interview scheduled, Interviewed, Offer received, Accepted, Rejected, Withdrawn — when I update status, then I can only select from this fixed list.
- Given an application has an active reminder and the reminder notification email has been sent, when I view the application, then a "Follow-up reminder sent on [date]" indicator is shown alongside the status — this is a read-only indicator derived from the reminder state, not a selectable status value.
- Given I change a status, when the change is saved, then the status change is reflected immediately in the applications list without a page reload.

---

### Story 4: Edit application details
As a job seeker,
I want to edit any field of an existing application,
so that I can correct mistakes or add information that wasn't available when I first logged it.

**Acceptance Criteria:**
- Given I am viewing an application detail, when I click a field, then it becomes editable inline.
- Given I edit a field and save, when the save completes, then the updated value is persisted and displayed immediately.
- Given I edit a field and discard, when I press Escape or click Cancel, then the original value is restored with no change saved.
- Given I want to delete an application, when I click delete and confirm, then the application and all linked reminders are permanently deleted. Linked cover letters are retained in my cover letter history.

---

### Story 5: Link a CV version to an application
As a job seeker,
I want to record which CV version I used for an application,
so that I know exactly what the employer received.

**Acceptance Criteria:**
- Given I am creating or editing an application, when I select "Link CV", then I can choose from my stored CV versions.
- Given I have linked a CV, when I view the application, then I see the CV version name and upload date, with a link to download it.
- Given I re-upload a new CV version, when I view a previously created application, then it still references the CV version that was current at the time of application — not the new one.

---

## Scope Guard

- **Story 5** (link CV) depends on CV Parsing (file storage + versioning) being complete. Build Story 5 only after the CV upload flow is in place.
- **Cover letter linking** is not in this spec — it is handled by the Cover Letter Generation feature, which will add a link back to the application record.
- **Reminders** referenced in Story 4 (delete cascades to reminders) depend on the Reminders feature. Add the cascade delete rule once that feature is built.
- The **job posting stored as Markdown** (Story 1) assumes the frontend provides a plain textarea. A rich Markdown editor is out of scope for MVP.

---

## Decisions

- **Status list:** Applied, Interview scheduled, Interviewed, Offer received, Accepted, Rejected, Withdrawn — fixed for MVP.
- **Required fields:** Company name, role, date applied, status. All other fields (contact person, job posting, notes) are optional — contact person is often unknown at application time.
