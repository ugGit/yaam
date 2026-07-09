# ADR 001: Typed profile model with JSON custom fields

**Date:** 2026-07-08
**Status:** Accepted

## Context

The user profile has predefined fields (name, work experience, skills, etc.) and user-defined custom fields. The data model must be extensible for new predefined fields while keeping the codebase maintainable for a solo developer.

An earlier draft proposed a config-driven field registry where all fields (predefined and custom) were defined in a JSON config file and stored in a flexible JSON column. This was rejected because:

- Generic renderers inevitably accumulate field-specific special cases, producing the worst of both worlds.
- A JSON column for all profile data loses compile-time type safety and makes queries harder.
- Migrations don't disappear — a registry change still requires updating the CV parser, AI adapter prompts, and query logic. The change is just scattered rather than localized.
- For a solo project with a small, stable set of predefined fields, the indirection adds friction with no meaningful payoff.

## Decision

**Predefined fields** use a strongly-typed `Profile` domain entity with real DB columns and child tables. Each field is a typed property. Adding a new predefined field requires a model change and a migration — this is an acceptable trade-off that preserves type safety, enables relational queries, and keeps rendering explicit.

**Custom fields** (user-defined at runtime) are stored as a key-value JSON column on the profile. They are genuinely dynamic by design — unknowable at compile time — and warrant flexible storage.

```
Profile entity
  ├── firstName, lastName, email, phone, location, summary  → columns
  ├── workExperience[]    → child table (company, title, startDate, endDate, description)
  ├── education[]         → child table (institution, degree, field, startDate, endDate)
  ├── skills[]            → JSON array column
  ├── languages[]         → child table (language, proficiency)
  ├── certifications[]    → child table (name, issuer, date)
  ├── links[]             → child table (label, url)
  └── customFields        → JSON key-value column (user-defined, plain text values)
```

## Consequences

- Full type safety on predefined fields — TypeScript and C# can reason about `profile.workExperience[0].company` at compile time.
- Adding a new predefined field costs a model update + migration (~20 min solo). This is the right trade-off at this scale.
- Custom fields remain flexible and require no migration when users add new ones.
- The CV parser and AI adapter map to typed properties, not config keys — simpler to implement and test.
- If predefined fields grow very large and change frequently in future, this decision can be revisited.
