# Account Management

**Date:** 2026-07-08
**Updated:** 2026-08-21
**Status:** Design approved — Phase 1 ready for implementation
**MVP fit:** In scope — prerequisite for all other features

## Summary

Account management covers registration, login, password reset, and account settings (password change, account deletion). It is the auth layer that gates access to all YAAM features. Auth is delegated to Supabase (cloud, EU region); the backend validates Supabase JWTs locally. Delivered in two phases: Phase 1 wires up auth infrastructure and frontend flows; Phase 2 scopes all existing entities to users and adds account deletion.

---

## User Stories

### Story 1: Register an account
As a job seeker,
I want to create an account with my email address and a password,
so that my data is private and accessible only to me.

**Acceptance Criteria:**
- Given I navigate to the registration page, when I enter a valid email and a password meeting minimum requirements, then my account is created and I am logged in automatically.
- Given I try to register with an email that already exists, when I submit, then I see an error message and no duplicate account is created.
- Given I enter a password shorter than 12 characters or missing complexity requirements, when I submit, then I see a field-level validation message listing the requirements.
- Given my account is created, when I am redirected, then I land on the onboarding flow (CV upload prompt).

---

### Story 2: Log in
As a returning user,
I want to log in with my email and password,
so that I can access my applications, profile, and reminders.

**Acceptance Criteria:**
- Given I navigate to the login page, when I enter correct credentials, then I am authenticated and redirected to my applications list.
- Given I enter incorrect credentials, when I submit, then I see a generic error message ("Invalid email or password") — no indication of which field is wrong.
- Given I am authenticated, when my session expires, then I am redirected to the login page and shown a message that my session has ended.
- Given I am on a protected page while unauthenticated, when I try to access it, then I am redirected to the login page. After logging in, I am returned to the page I was trying to access.

---

### Story 3: Reset password
As a job seeker,
I want to reset my password via email if I forget it,
so that I can regain access to my account without losing my data.

**Acceptance Criteria:**
- Given I click "Forgot password" on the login page, when I enter my email and submit, then I receive a password reset email if the address is registered — no confirmation of whether the address exists (prevents enumeration).
- Given I click the reset link in the email, when the link is valid and not expired, then I can enter and confirm a new password.
- Given the reset link has expired (after 1 hour), when I try to use it, then I see a message that the link has expired and I am prompted to request a new one.
- Given I reset my password successfully, when the change is saved, then I am redirected to the login page and all existing sessions are invalidated.

---

### Story 4: Manage account settings
As a returning user,
I want to change my password from a settings page,
so that my account stays secure.

**Acceptance Criteria:**
- Given I navigate to account settings, when the page loads, then I see a change password option.
- Given I change my password from the settings page, when I confirm the new password, then all other active sessions are invalidated and I remain logged in on the current device.

---

### Story 5: Delete account
As a returning user,
I want to permanently delete my account and all associated data,
so that I can exercise my right to be forgotten.

**Acceptance Criteria:**
- Given I navigate to account settings, when I click "Delete account", then I am shown a confirmation dialog that clearly states all data will be permanently deleted.
- Given I confirm deletion, when it completes, then my account, profile, all applications, cover letters, reminders, and uploaded CV files are permanently and irrecoverably deleted.
- Given deletion completes, when I try to log in with my old credentials, then I see a standard "Invalid email or password" error — no indication that the account ever existed.

---

## Auth Provider

**Supabase** (cloud, EU region) handles all auth operations: registration, login, email/password, password reset emails, and session management. Passkeys are enabled via a Supabase dashboard toggle when Supabase ships production support — no application code changes required.

Two separate cloud projects:
- `yaam-dev` — used for local development
- `yaam-prod` — production

---

## Architecture

### Backend

JWT validation lives entirely in `Yaam.Infrastructure`. Supabase issues HS256 JWTs; the backend validates them locally against the JWT secret — no outbound Supabase call on each request.

**New in `Yaam.Infrastructure`:**
- `SupabaseSettings` record (`{ get; init; }` properties) bound from `"Supabase"` config section
- `AddYaamAuthentication(IConfiguration)` extension in `DependencyInjection.cs` — configures JWT Bearer with Supabase issuer (`{Url}/auth/v1`), audience (`authenticated`), and symmetric signing key
- `CurrentUserService : ICurrentUserService` — reads `sub` claim from `IHttpContextAccessor`, returns `Guid UserId`

**New in `Yaam.Application`:**
- `ICurrentUserService` interface — `Guid UserId { get; }`

**Updated in `Yaam.API`:**
- `Program.cs`: `app.UseAuthentication()` added before `app.UseAuthorization()`
- All existing controllers: `[Authorize]` attribute
- `ApiFactory.cs` (integration tests): configured to issue a test JWT so existing tests continue to pass

**Config shape:**
```json
"Supabase": {
  "Url": "https://<ref>.supabase.co",
  "JwtSecret": "<secret>"
}
```

### Frontend

**`@supabase/supabase-js`** added. `supabaseUrl` and `supabaseAnonKey` added to Angular environment files (dev + prod).

**`core/auth/auth.service.ts`** — wraps the Supabase JS client:
- `session` signal (Supabase handles storage and silent token refresh)
- `isAuthenticated` computed signal
- `signIn`, `signUp`, `signOut`, `resetPassword`, `updatePassword`, `getAccessToken` methods

**`core/auth/auth.guard.ts`** — checks `isAuthenticated`; redirects to `/auth/login` and stores the attempted URL for post-login redirect.

**`core/interceptors/auth.interceptor.ts`** — attaches `Authorization: Bearer <token>` to every outgoing API request. Added to `provideHttpClient` in `app.config.ts`.

**`features/auth/` pages:**
- `login` — email + password; links to register and forgot-password
- `register` — email + password with requirements shown; redirects to `/profile` on success (CV upload onboarding)
- `forgot-password` — email field; generic confirmation on submit (prevents enumeration)
- `update-password` — landing page for the Supabase reset email link; exchanges `?code=` param via `supabase.auth.exchangeCodeForSession`, then shows new password form
- `account-settings` — change password only (calls `supabase.auth.updateUser`); delete account action deferred to Phase 2

**Route updates:** all existing routes (`applications`, `profile`, `cover-letters`, `reminders`) protected with `authGuard`. `/auth/*` routes are public.

**Supabase dashboard setup required:** app URL added to allowed redirect URLs for the password reset flow.

---

## Phased Delivery

### Phase 1 — auth infrastructure PR
Stories 1–4 (registration, login, password reset, change password). All authenticated users can access all data — not yet user-scoped. Acceptable since no real users exist during this window.

### Phase 2 — entity scoping PR
- `UserId` (`Guid`) foreign key added to `Profile`, `Application`, `Reminder`
- All queries and commands filtered/scoped by `ICurrentUserService.UserId`
- `DELETE /account` endpoint: calls Supabase Admin API (`service_role` key, server-side only) to remove the auth user, then cascades deletion of all app data
- Story 5 (account deletion) frontend wired up in account settings page

---

## Scope Guard

- **Auth provider:** Supabase cloud (EU region). External OAuth providers (Google, LinkedIn) are a future feature.
- **TOTP:** Future enhancement — not in MVP.
- **Passkeys:** Supabase dashboard toggle when available. No application code changes required.
- **Session management:** Stateless JWT — Supabase JS SDK handles token storage and silent refresh. No server-side session store.
- **Timezone:** Not stored for MVP. All server-side operations run on server time.
- **GDPR / account deletion:** Hard requirement — delivered in Phase 2.

---

## Decisions

- **Password requirements:** 12 characters minimum, at least one number or special character — enforced via Supabase password strength configuration.
- **JWT token lifetime:** Supabase defaults (1-hour access token, configurable refresh token). Silent refresh handled by the JS SDK.
- **Onboarding:** CV upload prompt only after registration. No additional steps.
- **Timezone:** Not stored on the user account for MVP.
- **Open-source deployment story:** backend is auth-provider agnostic (JWKS URL is the only coupling); frontend `AuthService` wrapper minimises swap surface. Self-hosters point to their own Supabase project via env vars.
