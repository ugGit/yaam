# Future Enhancements

Items noted during code review as valuable but out of scope for the current story.

## Mandatory profile fields (post-onboarding)

`Profile.FirstName`, `Profile.LastName`, and `Profile.Email` are currently optional. Once a proper onboarding flow exists, these should be required fields. Note: `Profile.Email` is a contact/CV email and does not need to match the account login email.

## Job posting enrichment

Currently `JobPosting` is stored as a plain URL string. A future story should:
- Extract structured metadata (company name, role title, salary range) from the URL.
- Store and display extracted metadata alongside the raw URL.
- Possibly support pasting raw job description text as an alternative input.

## Frontend form validation

Form fields currently rely solely on backend validation (FluentValidation returning 400s). A future story should:
- Add client-side validation to the application form (required, max length) using signal forms validators.
- Display field-level errors inline, not just as a generic "Could not save" banner.
- Mirror backend max-length constraints so the UX fails fast without a round-trip.

## Reminder scheduling robustness

`ReminderNotificationService` currently uses `await Task.Delay(delay, stoppingToken)` to fire once daily at 8 AM UTC. This is safe for a single-instance deployment but has one notable failure mode: if the app is down at 8 AM, `TimeUntilNextRun()` recalculates on restart and the job waits until the *next* 8 AM, silently skipping any due reminders.

A future story could replace this with [Hangfire](https://www.hangfire.io/) or [Quartz.NET](https://www.quartz-scheduler.net/) when any of the following become true:
- Missed-run recovery is needed (fire outstanding reminders after a restart)
- Per-reminder delivery times are required (rather than one daily batch)
- Multiple app instances are deployed (both would fire without distributed coordination)
- Job history and a monitoring dashboard are wanted

Until then, the `Task.Delay` approach is the right fit — zero dependencies, auditable, and sufficient for a single-instance tool.

## Authentication and authorization

The API and frontend currently have no auth. This is acceptable for a single-user local tool, but any multi-user or hosted deployment requires:
- An authentication layer (e.g. ASP.NET Core Identity, OAuth2/OIDC, or an API key scheme) on the backend, with all `/api/*` endpoints protected by `[Authorize]`.
- An Angular auth guard protecting the `/applications` route tree and a login flow for the frontend.
- Per-user data isolation at the database query level — all repository queries must filter by `UserId` once user identity is established.
- Note: `JobPosting` is currently stored as a plain text URL. If a future story fetches content from that URL server-side (for enrichment), add a URL allowlist or redirect-chain validation to prevent SSRF.
