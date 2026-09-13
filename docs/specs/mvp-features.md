# MVP Feature Set

## The Core Loop

```
Account created → CV import → Profile built → Application captured → Cover letter generated → Reminders set
```

Every MVP feature serves this loop. Nothing outside it ships in v1.

## Features

### 0. Account Management (prerequisite)
- Registration with email + password (12 char min, complexity required)
- Login with JWT auth (1h access token, 7d refresh token)
- Password reset via email
- Account settings: change password
- Account deletion (full data wipe — GDPR)
- Detailed spec: `2026-07-08-account-management.md`

### 1. CV Parsing
- User uploads CV (PDF only, 2 MB max)
- AI extracts structured data: work experience, education, skills, languages, certifications
- Predefined fields with schema (see architecture/guidelines.md for data model)
- User can review and edit extracted fields

### 2. User Profile
- Built from CV parse as the baseline
- Predefined fields: work experience, education, skills, contact info, languages
- User-defined custom fields (e.g., "preferred work style", "salary expectation")
- AI periodically asks follow-up questions to sharpen the profile
- Custom fields can be used as AI matching input for cover letter generation

### 3. Application Tracking
- Capture per application:
  - Company name
  - Role / job title
  - Contact person (name, email, phone)
  - Job posting text or URL (store the text as markdown, in case it get's taken down)
  - Date applied
  - Which CV version used
  - Which cover letter used
  - Status (applied / interview / rejected / offer / accepted)
  - Personal notes
- Link to generated cover letter and CV version used

### 4. Cover Letter Generation
- Input: user profile + job posting + optional extra context from user + optional reference cover letters
- User can upload past cover letters (plain text, stored on profile) to preserve their writing style
- AI generates a cover letter draft that matches the user's voice when references are provided
- Output format: plain text (MVP) — Word/LaTeX with templates in v2
- Version history: keep all generated cover letters per application

### 5. Reminders
- One active reminder per application
- Fixed delay options: 7, 14, or 30 days (custom input also available)
- Daily notification job runs at 08:00 server time (no timezone support in MVP)
- Email notification sent to user (never to employer) with pre-filled follow-up template
- Template variables: company, role, contact name (falls back to "Dear Hiring Team"), date applied
- "Follow-up reminder sent on [date]" indicator shown on the application — read-only, derived from reminder state
- Detailed spec: `2026-07-08-reminders.md`

## Out of Scope for MVP

- Browser auto-fill plugin
- Automated sending of follow-up emails
- AI job search / portal integration
- Word/LaTeX cover letter templates
- i18n / multi-language UI
- Analytics
- Interview Protocol Management
- Heavily Personalized Cover Letters
