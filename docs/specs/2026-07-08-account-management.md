# Account Management

**Date:** 2026-07-08
**Status:** Draft
**MVP fit:** In scope — prerequisite for all other features

## Summary

Account management covers registration, login, password reset, and account settings (timezone, notification email). It is the auth layer that gates access to all YAAM features. The user's job-search profile is separate from their account.

---

## User Stories

### Story 1: Register an account
As a job seeker,
I want to create an account with my email address and a password,
so that my data is private and accessible only to me.

**Acceptance Criteria:**
- Given I navigate to the registration page, when I enter a valid email and a password meeting minimum requirements, then my account is created and I am logged in automatically.
- Given I try to register with an email that already exists, when I submit, then I see an error message and no duplicate account is created.
- Given I enter a password shorter than 8 characters or missing complexity requirements, when I submit, then I see a field-level validation message listing the requirements.
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
- Given I am on a protected page while unauthenticated, when I try to access it, then I am redirected to the login page.

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
I want to update my account settings — timezone and notification email — from a settings page,
so that reminders are sent at the right time and to the right address.

**Acceptance Criteria:**
- Given I navigate to account settings, when the page loads, then I see my current notification email and a change password option.
- Given I change my notification email, when I save, then future reminder emails are sent to the new address. A confirmation is sent to the old address informing of the change.
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

## Scope Guard

- **Auth implementation:** Use ASP.NET Core Identity with JWT tokens for API authentication. External providers (Google, LinkedIn sign-in) are a future feature.
- **Timezone:** Not stored for MVP. Reminder notifications run on server time.
- **Notification email** defaults to the registration email. It can be changed independently — useful if the user wants reminders on a different address.
- **Session management** in MVP is stateless JWT — no server-side session store. Token expiry handles session termination.
- **GDPR:** Account deletion (Story 5) is a hard requirement, not optional. All data must be deleted, not anonymised, unless retention is legally required.

---

## Decisions

- **Password requirements:** 12 characters minimum, at least one number or special character.
- **JWT token lifetime:** 1 hour access token, 7 day refresh token, silent refresh on activity.
- **Onboarding:** CV upload prompt only after registration. No additional steps.
- **Timezone:** Not stored on the user account for MVP. All server-side operations (reminder notifications) run on server time. Timezone support is a future feature.
