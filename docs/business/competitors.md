# Competitor Analysis

**Last updated:** 2026-07-17

---

## Market Overview

The job application management space splits into three overlapping categories:

1. **Application trackers** — kanban/spreadsheet pipeline, no AI
2. **AI resume/CV tailors** — rewrite CVs per job posting, no lifecycle tracking
3. **All-in-one job search assistants** — tracker + AI resume + cover letter + browser extension

YAAM targets category 3 but with a distinct privacy and personalisation angle.

---

## Direct Competitors

| Tool | Tracker | AI Cover Letter | AI Resume | Browser Ext | Local AI | Privacy Angle | Notes |
|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| **[Teal HQ](https://tealhq.com)** | ✓ | ✓ | ✓ | ✓ | — | — | Largest player; resume keyword matching; 140k+ Chrome ext users |
| **[Prentus](https://prentus.com)** | ✓ | ✓ | ✓ | — | — | — | Bootcamp/community-focused; voice mock interviews; gamified Career Points |
| **[Huntr](https://huntr.me)** | ✓ | — | — | ✓ | — | — | Kanban-only; web clipper; mobile; no AI |
| **[Simplify](https://simplify.jobs)** | ✓ | — | — | ✓ | — | — | Autofill specialist (Workday, Greenhouse, Lever); high-volume applicants |
| **[JobPilot](https://jobpilotapp.com)** | ✓ | — | — | ✓ | — | — | Chrome ext auto-tracking; AI follow-up timing suggestions |
| **[Four-Leaf](https://four-leaf.io)** | — | ✓ | ✓ | — | — | — | Mock interviews + coaching; no lifecycle tracking |
| **[Trackr Pro](https://trackr.pro)** | ✓ | ✓ | — | — | — | Mentioned | CV scoring + cover letters; weakly privacy-positioned |
| **[Jobscan](https://jobscan.co)** | — | ✓ | ✓ | — | — | — | ATS optimisation focus; resume scanner; premium cover letter |
| **[RoleCatcher](https://rolecatcher.com)** | ✓ | — | — | — | — | — | "Career OS" — contacts, employer research; no AI generation |

### Cover-letter-only tools (partial overlap)

[Kickresume](https://kickresume.com), [Rezi.ai](https://rezi.ai), [Enhancv](https://enhancv.com), [Careered.ai](https://careered.ai) — AI resume/cover letter generation only; no application lifecycle.

---

## Open-Source / Self-Hosted

### JobOps — [github.com/dakheera47/Job-Ops](https://github.com/dakheera47/Job-Ops)

**License:** AGPL-3.0 + Commons Clause (same family as YAAM's plan)
**Stack:** TypeScript / Node.js + React (not reusable at code level for .NET/Angular)
**Traction:** 800+ users, 4,000+ searches run, #3 GitHub Trending TypeScript

**What it does:**
- Searches 10+ job boards from one screen (LinkedIn, Indeed, Glassdoor, Adzuna, Swiss boards via custom extractors)
- AI scores each job 0-100 against your profile
- Rewrites your CV (headline, summary, skills) per job posting
- Exports polished PDF via Reactive Resume integration
- Auto-detects recruiter replies (Gmail) to update status — interview, rejection, offer
- Does **not** generate cover letters — the "ghostwriter" is a per-job chat, not structured generation

**AI providers supported:** OpenAI, Anthropic Claude, Ollama/LM Studio, Google Gemini, OpenRouter, GLM, Codex

**Architectural patterns worth adopting (see below for detail):**
- `WritingStyle` model: `tone`, `formality`, `constraints` (free text), `doNotUse`, `languageMode`, `summaryMaxWords`, `maxKeywordsPerSkill` — separates structured settings from free-text user prose
- `PromptTemplate` pattern: user-editable `{{token}}`-based templates with hardcoded defaults — lets power users customise prompts without exposing raw LLM access
- Multi-provider LLM abstraction: factory pattern with one adapter per provider — directly applicable to YAAM's `IAIProvider` design

**Key gap vs YAAM:** no cover letter generation, no application lifecycle management, no per-job cover letter history.

### Reactive Resume — [rxresu.me](https://rxresu.me)

**License:** MIT
**Stack:** Next.js + NestJS, Prisma, Minio (object storage), Browserless (headless Chrome for PDF)
**Self-hostable:** yes, under 30 seconds with Docker

**What it does:** Free, open-source, privacy-first resume builder. Real-time editing, dozens of templates, drag-and-drop customisation, PDF export, shareable link. OpenAI integration for per-bullet improvements. No tracking, no ads.

**Relevance to YAAM:**
- JobOps uses rxresume as its PDF rendering backend — they map structured data to rxresume's open JSON schema and call the PDF export API
- YAAM could adopt the same approach: store user profiles in the rxresume JSON schema internally and call a self-hosted rxresume instance for PDF generation and CV editing
- This avoids building a CV editor and PDF renderer from scratch
- MIT license is compatible with YAAM's AGPL plan
- Aligns with YAAM's privacy/self-hosting angle

**Recommended integration point:** YAAM maps its `Profile` entity to rxresume's JSON schema on export. User-facing CV editing (templates, drag-and-drop) links to a YAAM-hosted rxresume instance. AI fills in content; rxresume handles presentation.

---

## YAAM's Differentiated Position

### Features with no direct competitor

| Differentiator | Status in YAAM | Gap in market |
|---|---|---|
| Cover letter style presets with curated examples | Spec'd (future-features.md) | Nobody — Teal/Prentus have basic tone toggles, no illustrated presets |
| Deeply personal cover letter (childhood experiences, long-term vision, personal drivers) | Spec'd (future-features.md) | Nobody |
| AI cover letter quality eval tool (score, compare providers) | Spec'd (future-features.md) | Nobody |
| Per-application cover letter versioning with restore | Spec'd (cover-letter-generation.md Story 4) | Nobody |
| Writing style blacklist ("do not use: passionate, synergy, leverage") | To spec | Nobody; JobOps has `doNotUse` internally but not commercial competitors |
| Multi-language generation per cover letter (profile in EN → letter in DE) | To spec | Nobody; JobOps has language mode internally |
| Local AI via Ollama (no data sent to cloud) | Planned | JobOps only OSS peer; no commercial competitor |
| GDPR-first Swiss hosting (Infomaniak) | Planned | Trackr Pro mentions privacy loosely; nobody commits to Swiss jurisdiction |

### Pricing parity with market

Freemium split (tracking free, AI paid) matches Teal and Prentus. No change needed.

### Table-stakes gap

**No browser extension.** Teal, Prentus, Simplify, and JobPilot all have one. One-click job saving from a job board is the primary acquisition hook for the category. YAAM needs this in the medium term (see future-features.md — Browser Plugin).

---

## Sources

- [JobPilot — Best Job Application Tracker Software 2026](https://www.jobpilotapp.com/blog/best-job-application-trackers-2026)
- [Prentus — 7 Free Job Tracker Apps Tested 2026](https://prentus.com/blog/we-found-the-5-best-job-tracker-tools-on-the-market)
- [AlternativeTo — Teal alternatives](https://alternativeto.net/software/teal/)
- [Sprad — Top 5 Teal Alternatives 2026](https://sprad.io/blog/top-5-teal-alternatives-for-smarter-skills-based-job-search-without-spam)
- [JobOps GitHub](https://github.com/dakheera47/Job-Ops)
- [Reactive Resume](https://rxresu.me)
- [Reactive Resume GitHub](https://github.com/AmruthPillai/Reactive-Resume)
