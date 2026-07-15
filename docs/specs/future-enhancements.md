# Future Enhancements

Items noted during code review as valuable but out of scope for the current story.

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
