# Future Features

Rough sketches only — not committed, not scoped. Revisit after MVP.

## Browser Plugin (Auto-fill)
- Auto-fill job application forms using profile data
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

## i18n / Multi-language
- Translate UI to German, French, Italian (Swiss market)
- AI prompts in user's preferred language

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