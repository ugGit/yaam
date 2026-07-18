# Business Plan

## Core Function

YAAM helps job seekers manage the full application lifecycle in one place — from CV import through cover letter generation to follow-up reminders. The value proposition: less time on admin, more time on actual applications.

## Target User

Active job seekers who apply to multiple positions simultaneously and lose track of status, cover letter versions, and follow-up timing.

## How Money Is Made

See [pricing.md](pricing.md) for detailed model analysis. Working assumption: freemium with paid AI features.

- **Free tier:** Basic tracking (applications, CV storage, reminders)
- **Paid tier:** AI cover letter generation, CV profile refinement, advanced analytics

## License Model

See [license.md](license.md). Working assumption: open-source core (AGPL or similar) with a hosted paid service.

## Key Differentiators

See [competitors.md](competitors.md) for the full competitive analysis. Summary:

- **End-to-end lifecycle** — most tools do one piece (tracker or cover letter or resume); YAAM does the full loop
- **Cover letter generation grounded in structured profile data** — not generic prompts; tied to a specific application and job posting
- **Cover letter style control** — user selects tone/style presets illustrated with curated examples before generating; no competitor does this
- **Deeply personal generation inputs** — long-term vision, personal drivers, childhood experiences; differentiates output quality from generic AI letters
- **Provider-agnostic AI** — runs fully local via Ollama (no data sent to cloud); no commercial competitor offers this
- **Swiss/GDPR-aligned hosting** — Infomaniak production deployment; explicit jurisdictional privacy guarantee no competitor makes
- **Per-application cover letter history** — all versions stored, restorable; comparable tools overwrite on regenerate

## Out of Scope (MVP)

- Browser auto-fill plugin
- Automated follow-up emails (spam risk)
- AI job search / job portal integration
