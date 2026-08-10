# Reminders

**Date:** 2026-07-18
**Status:** Draft
**MVP fit:** In scope

## Summary

A job seeker sets a follow-up reminder on an application; on the due date a daily background job sends an email with a pre-filled follow-up draft so they are prompted to act without having to check the app daily.

---

## User Stories

### Story 1: Set a reminder on an application
As a job seeker,
I want to set a reminder on an application that triggers after a defined number of days,
so that I am prompted to follow up at the right time without having to track it manually.

**Acceptance Criteria:**
- Given I am viewing an application, when I click "Add reminder", then a form opens where I can select a delay (7, 14, or 30 days) or enter a custom number of days, and optionally enter a note about what the reminder is for.
- Given I have selected a delay, when I view the form, then the calculated due date is shown as a preview (e.g., "Reminder due: 1 Aug 2026") before I save.
- Given I save the reminder, when it is created, then the due date is stored as today + N days and the reminder appears on the application with its due date.
- Given I try to save a reminder with no delay entered, when I submit, then I see a validation error and the reminder is not created.
- Given an active reminder already exists on an application, when I try to add another, then I see a message telling me to complete or delete the existing reminder first — the add form does not open.

---

### Story 2: Receive an email notification when a reminder is due
As a job seeker,
I want to receive an email on the day a reminder is due,
so that I am prompted to follow up without having to check the app daily.

**Acceptance Criteria:**
- Given a reminder's due date matches today, when the daily notification job runs at 08:00 server time, then one email is sent per due reminder.
- Given the email is sent, when I receive it, then it contains: company name, role, date applied, and my optional reminder note.
- Given the email is sent, when I receive it, then it also includes a ready-to-use follow-up email draft in plain text, populated with the contact name (if set) or falling back to "Dear Hiring Team,", plus company name, role, and date applied.
- Given the email has been sent for a reminder, when the job runs again on a subsequent day, then no second email is sent for the same reminder.
- Given a reminder has been completed (`CompletedAt` is set), when the notification job runs, then no email is sent for it.
- Given multiple reminders are due on the same day, when the job runs, then one email is sent per reminder — not a combined digest.
- Given the email has been sent for a reminder, when I view the application, then a read-only "Follow-up reminder sent on [date]" indicator is shown on the application detail.

---

### Story 3: View and manage reminders
As a job seeker,
I want to see all my upcoming and overdue reminders in one place,
so that I can act on them, reschedule, or dismiss them without opening each application.

**Acceptance Criteria:**
- Given I navigate to the reminders page, when it loads, then I see all active reminders grouped by: Overdue (DueDate < today), Due today (DueDate = today), Upcoming (DueDate > today) — sorted by due date ascending within each group.
- Given I view a reminder, when I click "Mark as done", then `CompletedAt` is set and the reminder is removed from the active list. The application's reminder section shows no active reminder.
- Given I view a reminder, when I click "Reschedule", then I can enter a new due date; on save the existing reminder's `DueDate` is updated in place, `NotifiedAt` is cleared (so the email re-sends on the new date), and the reminder appears in the correct group on the list.
- Given I view a reminder, when I click "Delete" and confirm, then the reminder is permanently removed and no further notifications are sent for it.
- Given an application has no active reminder, when I view the application detail, then the reminder section shows an empty state with an "Add reminder" prompt.
- Given all reminders are completed or deleted, when I view the reminders page, then I see an empty state message.

---

## Scope Guard

- **Story 2 requires a background job** — implement as a .NET `BackgroundService` (no Hangfire for MVP). The service queries `Active` reminders where `DueDate <= today AND NotifiedAt IS NULL` and sends one email per result.
- **No `Status` enum on the entity.** All reminder states are derived from `DueDate`, `NotifiedAt`, and `CompletedAt`:
  - `CompletedAt IS NULL + DueDate > today` → Upcoming
  - `CompletedAt IS NULL + DueDate = today` → Due today
  - `CompletedAt IS NULL + DueDate < today` → Overdue
  - `CompletedAt IS NOT NULL` → Done
  - `NotifiedAt IS NOT NULL` → email has been sent (independent of done state)
- **`IEmailSender` interface** in the use-case layer (`SendAsync(to, subject, textBody)`). One implementation: `SmtpEmailSender` in Infrastructure using MailKit. SMTP credentials come from configuration — no provider-specific code outside that class. Switching providers means implementing a new class and updating the DI registration only.
- **Local dev email:** Mailpit runs in Docker Compose (SMTP on port 1025, web inbox at `http://localhost:8025`). The backend's `appsettings.Development.json` must point `Email:SmtpHost` to `localhost:1025`.
- **Production email:** Resend SMTP relay (free tier: 3,000 emails/month). Same `SmtpEmailSender`, different config values. No code change required.
- **Notification email address:** Until Account Management ships, reminder emails are sent to the user's registration email. See Account Management Story 4 — once a configurable notification email is introduced, the reminder sender must be updated to read that field. A scope guard in that spec flags this dependency.
- **Automated sending of follow-up emails to employers is explicitly out of scope** — YAAM sends notification emails to the job seeker only, never to the employer.
- **One active reminder per application.** "Active" means `CompletedAt IS NULL` and the record exists (not deleted). Enforced at both API and UI level.
- **"Reminder sent" indicator on application** — derived from `Reminder.NotifiedAt`. The existing `ApplicationDto` → `ApplicationViewModel` mapping must be updated to include `ReminderNotifiedAt DateTime?`. No new endpoint needed.
- **Reschedule resets `NotifiedAt`** — clearing it ensures the email is re-sent on the new due date.
- **Reminder deletion cascade** — deleting an application must also delete its reminder. Verify in the EF cascade configuration on `ApplicationConfiguration`.
- **User-editable email template** is a future feature. Template is rendered server-side; plain text only.

---

## Open Questions

- None blocking. Email provider and notification email address decisions are resolved above.

---

## Decisions

- **Delay options:** 7, 14, or 30 days plus a custom day input. 3-day option removed — too soon to expect a meaningful reply.
- **Notification timing:** Daily job runs at 08:00 server time. Timezone support is a future feature.
- **One active reminder per application.** The user must complete or delete the existing reminder before adding a new one.
- **No `Status` stored field.** Use `CompletedAt DateTime?` and `NotifiedAt DateTime?` — display state is fully derived.
- **Email stack:** `IEmailSender` → `SmtpEmailSender` (MailKit). Local: Mailpit. Prod: Resend SMTP relay.
