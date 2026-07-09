# Reminders

**Date:** 2026-07-08
**Status:** Decided
**MVP fit:** In scope

## Summary

A job seeker sets a follow-up reminder on an application — triggered after a defined delay — and receives an email notification with a pre-filled follow-up template when it is due. The user decides whether and how to act on it; YAAM never sends emails on their behalf.

---

## User Stories

### Story 1: Set a reminder on an application
As a job seeker,
I want to set a reminder on an application that triggers after a defined number of days,
so that I am prompted to follow up at the right time without having to track it manually.

**Acceptance Criteria:**
- Given I am viewing an application, when I click "Add reminder", then I can set a delay (number of days from today) and an optional note describing what the reminder is for.
- Given the delay options are 7, 14, or 30 days with an additional custom day input, when I select an option, then the due date is calculated and shown as a preview before I save.
- Given I save the reminder, when it is created, then the due date is stored as today + N days.
- Given I have set a reminder, when I view the application, then the reminder is shown with its due date and status (upcoming / due / done).
- Given I try to save a reminder with no delay selected, when I submit, then I see a validation error and the reminder is not created.
- Given only one active reminder per application is allowed, when I try to add a second reminder while one is already active, then I see a message indicating I must complete or delete the existing reminder first.

---

### Story 2: Receive an email notification when a reminder is due
As a job seeker,
I want to receive an email on the day a reminder is due,
so that I am prompted to follow up without having to check the app daily.

**Acceptance Criteria:**
- Given a reminder's due date is today, when the daily notification job runs, then I receive one email per due reminder.
- Given the email is sent, when I receive it, then it contains: the company name, role, date applied, and my optional reminder note.
- Given a reminder is already marked as done, when the notification job runs, then no email is sent for it.
- Given multiple reminders are due on the same day, when the job runs, then I receive one email per reminder — not a single combined digest.

---

### Story 3: Pre-filled follow-up email template in the notification
As a job seeker,
I want the reminder email to include a ready-to-use follow-up email draft,
so that I can copy, adjust, and send it to the employer with minimal effort.

**Acceptance Criteria:**
- Given the reminder email is sent, when I receive it, then it includes a follow-up email draft populated with: contact name (if set), company name, role, and date applied.
- Given no contact name is stored on the application, when the template is rendered, then the salutation falls back to a generic opener (e.g., "Dear Hiring Team,").
- Given the template is rendered, when I view it, then it is plain text — no HTML formatting in the draft itself, so it can be pasted directly into any email client.

---

### Story 4: View and manage reminders
As a job seeker,
I want to see all my upcoming and overdue reminders in one place,
so that I can act on them, reschedule them, or dismiss them.

**Acceptance Criteria:**
- Given I navigate to the reminders page, when it loads, then I see all reminders grouped by status: overdue, due today, upcoming — sorted by due date ascending within each group.
- Given I view a reminder, when I click "Mark as done", then its status changes to done and it moves out of the active list.
- Given I view a reminder, when I click "Reschedule", then I can set a new due date and the reminder is updated.
- Given I view a reminder, when I click "Delete", then the reminder is permanently removed and no further notifications are sent for it.
- Given all reminders for an application are done or deleted, when I view the application, then the reminder section shows no active reminders with an option to add a new one.

---

## Scope Guard

- **Story 2** requires a scheduled background job (e.g., a daily cron) — this is infrastructure that must be planned in the CD setup. Do not skip this in the deployment plan.
- **Automated sending of follow-up emails to employers is explicitly out of scope for MVP** — YAAM sends notification emails to the job seeker only, never to the employer.
- **One active reminder per application** is the hard limit for MVP — the data model stores one reminder per application, enforced at both API and UI level.
- **Story 3 template** is plain text rendered server-side from application data. A user-editable template is a future feature — do not add template customisation in MVP.
- Reminder deletion cascades are handled by the Application Tracking spec (deleting an application deletes its reminders).

---

## Decisions

- **Delay options:** Fixed choices of 7, 14, or 30 days, plus a custom day input. The 3-day option was removed — too soon to expect a meaningful reply.
- **Notification timing:** Daily job runs at 08:00 server time. Timezone support is a future feature.
- **Reminders per application:** One active reminder per application. The user must complete or delete the existing reminder before creating a new one.
- **"Reminder sent" on application:** When a reminder notification is sent, a read-only indicator appears on the application — not a status value, derived from reminder state. See Application Tracking spec.
