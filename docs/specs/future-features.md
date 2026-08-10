# Future Features

Rough sketches only — not committed, not scoped. Revisit after MVP.

## CV Parsing — Scanned PDF Support (Docling)
- PdfPig (used in MVP) only extracts text from text-based PDFs; scanned/image PDFs yield nothing
- Replace or wrap the extraction step with [Docling](https://github.com/DS4SD/docling) (IBM, MIT), which handles OCR, scanned documents, and complex layouts
- Docling runs as a sidecar service or can be called via its Python SDK; the `ICvParser` abstraction in the use-case layer means the swap is localised to the infrastructure implementation

## Browser Plugin (Auto-fill)
- Auto-fill job application forms using profile data
- One-click job saving from job board pages (LinkedIn, Indeed, jobs.ch, etc.) — saves title, company, URL, and raw job description into a new application draft; standard feature in Teal, Prentus, Simplify, and JobOps
- Key question: how to test across different ATS platforms (Workday, Greenhouse, Lever, etc.)
- Significant complexity — treat as a separate product phase

## AI Job Search
- Search job portals based on profile and preferences
- Note: many tools already do this (LinkedIn, Indeed, etc.) — find the differentiated angle
- Target portals to define (Swiss market: jobs.ch, jobup.ch, etc.)

## Automated Follow-up Emails
- Send follow-up emails automatically after defined delay
- Risk: perceived as spam, could hurt applicant's reputation
- Requires careful opt-in design and rate limiting

## Word / LaTeX Cover Letter Templates
- User uploads their own template
- AI fills it with generated content
- Nice-to-have for users with established personal branding

## CV Export via Reactive Resume
- Use [Reactive Resume](https://rxresu.me) (MIT, self-hostable) as the PDF rendering and CV editing backend rather than building a CV editor from scratch
- YAAM maps its `Profile` entity to the rxresume JSON schema; rxresume handles templates, drag-and-drop, and PDF generation
- User-facing CV editing links to a YAAM-hosted rxresume instance; AI fills content; rxresume handles presentation
- JobOps already uses this pattern — validated approach
- Eliminates need to build a headless PDF renderer (Browserless/puppeteer) in-house

## i18n / Multi-language
- Translate UI to German, French, Italian (Swiss market)
- AI prompts in user's preferred language
- Per-cover-letter output language override — profile stored in English but letter generated in German/French on request; distinct from UI language; high value for Swiss market (DE/FR/IT applications from an EN-language profile)

## Analytics
- Application funnel: applied → interview rate → offer rate
- Response time tracking
- Best-performing cover letter patterns

## Interview Protocol Management
- Allow adding information to each protocol
- Offer restructuring the protocol with user verification
- Summarizes protocols using AI for brief overview

## Heavily Personalized Cover Letters
- Ask personal questions to refine the candidates profile (early experiences in childhood, deeply personal driver, desired impact, longterm vision)

## Writing Style Controls
- **Tone and formality** — structured settings (e.g., professional / conversational, formal / casual) injected into the prompt as system-level constraints rather than user prose
- **"Do not use" blacklist** — user defines a list of words/phrases the AI must avoid (e.g., "passionate", "synergy", "leverage", "proactive"); parsed out of user instructions and applied separately; no commercial competitor has this; JobOps has it internally (`chatStyleDoNotUse`)
- **Constraints free text** — open field for custom writing rules (e.g., "keep under 300 words", "no bullet points"); strip auto-detected directives (language, word count) before passing to the LLM so they drive prompt construction rather than polluting content
- Structured settings and free-text constraints are additive and compose into `CoverLetterContext` (see ADR 002)

## Cover Letter Style Selection
- User selects a formulation style before generating (e.g., Humorous, Serious, Honest, Confident, Understated)
- Each style is illustrated by a curated example cover letter so the user can preview the tone before committing
- Extends the reference letter MVP feature — predefined styles replace or complement user-uploaded references
- Style selection maps to a named `CoverLetterContext.Style` property (see ADR 002)

## AI Cover Letter Eval Tool
- Analyze generated cover letters to evaluate AI output quality
- Metrics could include: relevance to job posting, profile coverage, tone consistency, uniqueness vs. reference letters
- Useful for comparing providers (Ollama vs. Anthropic vs. Infomaniak) and prompting strategies
- Internal developer tool first; could become a user-facing "score" feature later