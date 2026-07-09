# ADR 002: Composable context object for cover letter generation

**Date:** 2026-07-08
**Status:** Accepted

## Context

Cover letter generation currently takes three inputs: the user's profile, the job posting, and optional user instructions. Future features will add more personalisation inputs (deep personal profile, interview notes, etc. — see `docs/specs/future-features.md`).

If the `IAIProvider.GenerateCoverLetter()` method accepts individual parameters, every new input type requires a signature change, which breaks all provider implementations simultaneously.

## Decision

The generation method accepts a single `CoverLetterContext` object rather than individual parameters:

```csharp
// Interface stays stable as inputs grow
Task<string> GenerateCoverLetter(CoverLetterContext context);

class CoverLetterContext
{
    Profile Profile { get; init; }           // always required
    string JobPosting { get; init; }         // always required
    string? UserInstructions { get; init; }  // optional, stored per application
    // Future: DeepProfile? DeepProfile
    // Future: InterviewProtocol? InterviewProtocol
}
```

Adding a new input type = adding a nullable property to `CoverLetterContext`. The interface signature and all provider implementations remain unchanged. Each provider reads only the context fields it knows how to use and ignores the rest.

## Consequences

- New personalisation inputs can be added without touching the `IAIProvider` interface or any provider implementation.
- `CoverLetterContext` is the single place to look for what inputs generation supports.
- Provider implementations must handle nullable context fields gracefully — a provider that doesn't support `DeepProfile` simply ignores it.
- `CoverLetterContext` must not become a dumping ground. Each new field requires a deliberate decision (its own spec + ADR update) before being added.
