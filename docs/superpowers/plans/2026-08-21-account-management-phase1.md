# Account Management Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire up Supabase JWT authentication on the backend and deliver the full auth UI (login, register, forgot/reset password, account settings with change password) on the frontend — all existing routes become protected.

**Architecture:** The backend validates Supabase HS256 JWTs locally via `Microsoft.AspNetCore.Authentication.JwtBearer`; no outbound Supabase call per request. The frontend wraps `@supabase/supabase-js` in `AuthService` (signal-based), attaches tokens via an HTTP interceptor, and guards all existing routes. Data is not yet user-scoped (Phase 2).

**Tech Stack:** .NET 10 / ASP.NET Core, `Microsoft.AspNetCore.Authentication.JwtBearer`, Angular 22 signal forms (`@angular/forms/signals`), `@supabase/supabase-js`, daisyUI.

**Spec:** `docs/specs/2026-07-08-account-management.md`

## Global Constraints

- Password minimum: 12 characters, at least one number or special character (enforced client-side; Supabase password strength handles server-side)
- JWT: Supabase HS256, issuer `{Url}/auth/v1`, audience `authenticated`
- Frontend forms: always use `form()` / `[formField]` / `[formRoot]` from `@angular/forms/signals` — never `ReactiveFormsModule` or manual `[value]` binding
- daisyUI mandatory for all HTML — use `btn`, `input`, `alert`, `card`, `form-control` etc.
- Backend naming: handler params named `command`/`query`, no abbreviations, `mediator.Send` on three lines
- Controller bodies: never return DTOs directly — map to ViewModels; every body-accepting endpoint uses a dedicated `*InputModel` record
- Settings classes: `{ get; init; }` only, never `{ get; set; }`
- Commit messages: Conventional Commits (`feat`, `fix`, etc.)

---

## File Map

### Backend — new files
| File | Responsibility |
|------|----------------|
| `backend/Yaam.Domain/Services/ICurrentUserService.cs` | Interface: `Guid UserId { get; }` |
| `backend/Yaam.Infrastructure/Auth/SupabaseSettings.cs` | Config record bound from `"Supabase"` section |
| `backend/Yaam.Infrastructure/Auth/CurrentUserService.cs` | Reads `sub` claim from `IHttpContextAccessor` |

### Backend — modified files
| File | Change |
|------|--------|
| `backend/Yaam.Infrastructure/Yaam.Infrastructure.csproj` | Add `Microsoft.AspNetCore.Authentication.JwtBearer` |
| `backend/Yaam.Infrastructure/DependencyInjection.cs` | Add `AddYaamAuthentication` extension method |
| `backend/Yaam.API/Program.cs` | Call `AddYaamAuthentication`; add `UseAuthentication()` |
| `backend/Yaam.API/appsettings.Development.json` | Add placeholder `Supabase` section |
| `backend/Yaam.API/Applications/ApplicationsController.cs` | `[Authorize]` |
| `backend/Yaam.API/Applications/ApplicationNotesController.cs` | `[Authorize]` |
| `backend/Yaam.API/Applications/ApplicationReminderController.cs` | `[Authorize]` |
| `backend/Yaam.API/Profile/ProfileController.cs` | `[Authorize]` |
| `backend/Yaam.API/Profile/CvController.cs` | `[Authorize]` |
| `backend/Yaam.API/Reminders/RemindersController.cs` | `[Authorize]` |
| `backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj` | Add `System.IdentityModel.Tokens.Jwt` |
| `backend/Yaam.Tests.Integration/appsettings.Test.json` | Add `Supabase` test config |
| `backend/Yaam.Tests.Integration/ApiFactory.cs` | Add `TestTokenHelper`, `CreateAuthenticatedClient` |
| `backend/Yaam.Tests.Integration/Applications/ApplicationsEndpointsTests.cs` | Use authenticated client |
| `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs` | Use authenticated client |
| `backend/Yaam.Tests.Integration/Profile/CvEndpointsTests.cs` | Use authenticated client |
| `backend/Yaam.Tests.Integration/Reminders/RemindersEndpointsTests.cs` | Use authenticated client |

### Frontend — new files
| File | Responsibility |
|------|----------------|
| `frontend/src/app/core/auth/auth.service.ts` | Supabase client wrapper; `session` signal, `isAuthenticated`, auth methods |
| `frontend/src/app/core/auth/auth.guard.ts` | `CanActivateFn` redirecting unauthenticated users to `/auth/login` |
| `frontend/src/app/core/interceptors/auth.interceptor.ts` | Attaches `Authorization: Bearer <token>` to every request |
| `frontend/src/app/features/auth/pages/login/login.component.ts` | Login form |
| `frontend/src/app/features/auth/pages/login/login.component.html` | Login template |
| `frontend/src/app/features/auth/pages/register/register.component.ts` | Register form |
| `frontend/src/app/features/auth/pages/register/register.component.html` | Register template |
| `frontend/src/app/features/auth/pages/forgot-password/forgot-password.component.ts` | Forgot password form |
| `frontend/src/app/features/auth/pages/forgot-password/forgot-password.component.html` | Forgot password template |
| `frontend/src/app/features/auth/pages/update-password/update-password.component.ts` | Code exchange + new password form (reset link landing) |
| `frontend/src/app/features/auth/pages/update-password/update-password.component.html` | Update password template |
| `frontend/src/app/features/auth/pages/account-settings/account-settings.component.ts` | Change password (authenticated) |
| `frontend/src/app/features/auth/pages/account-settings/account-settings.component.html` | Account settings template |

### Frontend — modified files
| File | Change |
|------|--------|
| `frontend/package.json` | Add `@supabase/supabase-js` |
| `frontend/src/environments/environment.ts` | Add `supabaseUrl`, `supabaseAnonKey` |
| `frontend/src/environments/environment.prod.ts` | Add `supabaseUrl`, `supabaseAnonKey` |
| `frontend/src/app/app.config.ts` | Add `authInterceptor` to `provideHttpClient` |
| `frontend/src/app/app.routes.ts` | Add `canActivate: [authGuard]` to protected routes; add `/account` route |
| `frontend/src/app/features/auth/auth.routes.ts` | Add all auth page routes |

---

### Task 1: Backend — auth packages, types, and DI extension

**Files:**
- Modify: `backend/Yaam.Infrastructure/Yaam.Infrastructure.csproj`
- Create: `backend/Yaam.Domain/Services/ICurrentUserService.cs`
- Create: `backend/Yaam.Infrastructure/Auth/SupabaseSettings.cs`
- Create: `backend/Yaam.Infrastructure/Auth/CurrentUserService.cs`
- Modify: `backend/Yaam.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Produces: `ICurrentUserService.UserId: Guid` (Task 2 reads claims; Phase 2 handlers inject this)

- [ ] **Step 1: Add JWT Bearer NuGet package to Infrastructure**

Open `backend/Yaam.Infrastructure/Yaam.Infrastructure.csproj` and add inside the first `<ItemGroup>`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.9" />
```

Run:
```bash
cd backend && dotnet restore
```
Expected: restore succeeds.

- [ ] **Step 2: Create `ICurrentUserService` in Domain**

Create `backend/Yaam.Domain/Services/ICurrentUserService.cs`:

```csharp
namespace Yaam.Domain.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
}
```

- [ ] **Step 3: Create `SupabaseSettings`**

Create `backend/Yaam.Infrastructure/Auth/SupabaseSettings.cs`:

```csharp
namespace Yaam.Infrastructure.Auth;

public record SupabaseSettings
{
    public required string Url { get; init; }
    public required string JwtSecret { get; init; }
}
```

- [ ] **Step 4: Create `CurrentUserService`**

Create `backend/Yaam.Infrastructure/Auth/CurrentUserService.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Yaam.Domain.Services;

namespace Yaam.Infrastructure.Auth;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
                ?? throw new InvalidOperationException("User is not authenticated.");
            return Guid.Parse(sub);
        }
    }
}
```

- [ ] **Step 5: Add `AddYaamAuthentication` to `DependencyInjection.cs`**

Open `backend/Yaam.Infrastructure/DependencyInjection.cs`. Add these usings at the top:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yaam.Domain.Services;
using Yaam.Infrastructure.Auth;
```

Add a second public static method to the `DependencyInjection` class (after `AddInfrastructure`):

```csharp
public static IServiceCollection AddYaamAuthentication(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var settings = configuration.GetSection("Supabase").Get<SupabaseSettings>()
        ?? throw new InvalidOperationException("Supabase configuration section is required.");

    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUserService, CurrentUserService>();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(settings.JwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = $"{settings.Url}/auth/v1",
                ValidateAudience = true,
                ValidAudience = "authenticated",
                ValidateLifetime = true,
            };
        });

    return services;
}
```

- [ ] **Step 6: Verify it compiles**

```bash
cd backend && dotnet build Yaam.Infrastructure/Yaam.Infrastructure.csproj
```
Expected: Build succeeded with 0 errors.

- [ ] **Step 7: Commit**

```bash
git add backend/Yaam.Infrastructure/Yaam.Infrastructure.csproj \
        backend/Yaam.Domain/Services/ICurrentUserService.cs \
        backend/Yaam.Infrastructure/Auth/SupabaseSettings.cs \
        backend/Yaam.Infrastructure/Auth/CurrentUserService.cs \
        backend/Yaam.Infrastructure/DependencyInjection.cs
git commit -m "feat(auth): add Supabase JWT auth infrastructure"
```

---

### Task 2: Backend — enable middleware, protect controllers, fix integration tests

**Files:**
- Modify: `backend/Yaam.API/Program.cs`
- Modify: `backend/Yaam.API/appsettings.Development.json`
- Modify: 6 controller files (all add `[Authorize]`)
- Modify: `backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj`
- Modify: `backend/Yaam.Tests.Integration/appsettings.Test.json`
- Modify: `backend/Yaam.Tests.Integration/ApiFactory.cs`
- Modify: 4 test class files (switch to authenticated client)

**Interfaces:**
- Consumes: `AddYaamAuthentication` from Task 1
- Produces: all API endpoints require valid JWT; `ApiFactory.CreateAuthenticatedClient()` generates valid test tokens

- [ ] **Step 1: Write a failing test for unauthenticated access**

Open `backend/Yaam.Tests.Integration/Applications/ApplicationsEndpointsTests.cs`. Add this test (insert before the first existing test):

```csharp
[Fact]
public async Task GET_Applications_Returns401_WhenNoTokenProvided()
{
    var unauthenticated = factory.CreateClient();
    var response = await unauthenticated.GetAsync("/api/applications");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

- [ ] **Step 2: Run the test — verify it fails**

```bash
cd backend && dotnet test Yaam.Tests.Integration --filter "GET_Applications_Returns401_WhenNoTokenProvided"
```
Expected: FAIL — response is 200 (no [Authorize] yet).

- [ ] **Step 3: Update `Program.cs` to enable auth**

Open `backend/Yaam.API/Program.cs`. 

After `builder.Services.AddInfrastructure(...)`, add:
```csharp
builder.Services.AddYaamAuthentication(builder.Configuration);
```

Change the middleware pipeline line `app.UseAuthorization();` to:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Full updated middleware section (for reference):
```csharp
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

- [ ] **Step 4: Add `Supabase` placeholder to `appsettings.Development.json`**

Open `backend/Yaam.API/appsettings.Development.json`. Add the `Supabase` section (fill in your `yaam-dev` project values):

```json
"Supabase": {
  "Url": "https://<dev-ref>.supabase.co",
  "JwtSecret": "<dev-jwt-secret-from-supabase-dashboard>"
}
```

- [ ] **Step 5: Add `[Authorize]` to all six controllers**

For each file listed below, add `using Microsoft.AspNetCore.Authorization;` if not already present and add `[Authorize]` on the class:

**`ApplicationsController.cs`** — change:
```csharp
[ApiController]
[Route("api/applications")]
public class ApplicationsController(IMediator mediator) : ControllerBase
```
to:
```csharp
[Authorize]
[ApiController]
[Route("api/applications")]
public class ApplicationsController(IMediator mediator) : ControllerBase
```

Repeat the same `[Authorize]` addition for:
- `backend/Yaam.API/Applications/ApplicationNotesController.cs`
- `backend/Yaam.API/Applications/ApplicationReminderController.cs`
- `backend/Yaam.API/Profile/ProfileController.cs`
- `backend/Yaam.API/Profile/CvController.cs`
- `backend/Yaam.API/Reminders/RemindersController.cs`

- [ ] **Step 6: Run the 401 test — verify it now passes**

```bash
cd backend && dotnet test Yaam.Tests.Integration --filter "GET_Applications_Returns401_WhenNoTokenProvided"
```
Expected: PASS.

- [ ] **Step 7: Run all integration tests — observe failures**

```bash
cd backend && dotnet test Yaam.Tests.Integration
```
Expected: many failures with status 401 (existing tests don't send tokens yet). Note how many fail.

- [ ] **Step 8: Add JWT token generation package to test project**

Open `backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj`. Add inside the first `<ItemGroup>`:

```xml
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.12.1" />
```

Run:
```bash
cd backend && dotnet restore Yaam.Tests.Integration
```

- [ ] **Step 9: Add Supabase test config to `appsettings.Test.json`**

Open `backend/Yaam.Tests.Integration/appsettings.Test.json`. The current content is:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=yaam_test;Username=yaam;Password=yaam"
  }
}
```

Add the `Supabase` section:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=yaam_test;Username=yaam;Password=yaam"
  },
  "Supabase": {
    "Url": "https://test.supabase.co",
    "JwtSecret": "test-jwt-secret-minimum-32-chars-long!!"
  }
}
```

The secret must be ≥ 32 characters (256 bits for HMAC-SHA256). The URL only matters for issuer validation in tests.

- [ ] **Step 10: Add `TestTokenHelper` and `CreateAuthenticatedClient` to `ApiFactory.cs`**

Open `backend/Yaam.Tests.Integration/ApiFactory.cs`. Add these usings at the top:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
```

Add `TestUserId` and `CreateAuthenticatedClient` to the `ApiFactory` class:

```csharp
public static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

public HttpClient CreateAuthenticatedClient()
{
    var client = CreateClient();
    var token = TestTokenHelper.GenerateToken(TestUserId);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
}
```

Add `TestTokenHelper` as a new class at the bottom of `ApiFactory.cs` (after `NoOpCvParser`):

```csharp
public static class TestTokenHelper
{
    private const string Secret = "test-jwt-secret-minimum-32-chars-long!!";
    private const string Issuer = "https://test.supabase.co/auth/v1";
    private const string Audience = "authenticated";

    public static string GenerateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("sub", userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 11: Update all existing test classes to use `CreateAuthenticatedClient`**

In each of the four test files, change:
```csharp
private readonly HttpClient _client = factory.CreateClient();
```
to:
```csharp
private readonly HttpClient _client = factory.CreateAuthenticatedClient();
```

Files to update:
- `backend/Yaam.Tests.Integration/Applications/ApplicationsEndpointsTests.cs`
- `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`
- `backend/Yaam.Tests.Integration/Profile/CvEndpointsTests.cs`
- `backend/Yaam.Tests.Integration/Reminders/RemindersEndpointsTests.cs`

- [ ] **Step 12: Run all integration tests — verify all pass**

```bash
cd backend && dotnet test Yaam.Tests.Integration
```
Expected: all tests pass, including the new `GET_Applications_Returns401_WhenNoTokenProvided` test.

- [ ] **Step 13: Commit**

```bash
git add backend/Yaam.API/Program.cs \
        backend/Yaam.API/appsettings.Development.json \
        backend/Yaam.API/Applications/ApplicationsController.cs \
        backend/Yaam.API/Applications/ApplicationNotesController.cs \
        backend/Yaam.API/Applications/ApplicationReminderController.cs \
        backend/Yaam.API/Profile/ProfileController.cs \
        backend/Yaam.API/Profile/CvController.cs \
        backend/Yaam.API/Reminders/RemindersController.cs \
        backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj \
        backend/Yaam.Tests.Integration/appsettings.Test.json \
        backend/Yaam.Tests.Integration/ApiFactory.cs \
        backend/Yaam.Tests.Integration/Applications/ApplicationsEndpointsTests.cs \
        backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs \
        backend/Yaam.Tests.Integration/Profile/CvEndpointsTests.cs \
        backend/Yaam.Tests.Integration/Reminders/RemindersEndpointsTests.cs
git commit -m "feat(auth): protect API endpoints with JWT authorization"
```

---

### Task 3: Frontend — Supabase client, AuthService, AuthGuard, auth interceptor, route wiring

**Files:**
- Modify: `frontend/package.json`
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`
- Create: `frontend/src/app/core/auth/auth.service.ts`
- Create: `frontend/src/app/core/auth/auth.guard.ts`
- Create: `frontend/src/app/core/interceptors/auth.interceptor.ts`
- Modify: `frontend/src/app/app.config.ts`
- Modify: `frontend/src/app/app.routes.ts`

**Interfaces:**
- Produces:
  - `AuthService.isAuthenticated(): boolean` (computed signal)
  - `AuthService.getAccessToken(): string | null`
  - `authGuard: CanActivateFn` — redirects to `/auth/login?returnUrl=<url>`
  - `authInterceptor: HttpInterceptorFn` — adds `Authorization: Bearer <token>`

- [ ] **Step 1: Install `@supabase/supabase-js`**

```bash
cd frontend && npm install @supabase/supabase-js
```
Expected: package added to `package.json` and `node_modules`.

- [ ] **Step 2: Update environment files**

Replace `frontend/src/environments/environment.ts` with:
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7131/api',
  supabaseUrl: 'https://<dev-ref>.supabase.co',
  supabaseAnonKey: '<dev-anon-key>',
};
```

Replace `frontend/src/environments/environment.prod.ts` with:
```typescript
export const environment = {
  production: true,
  apiUrl: '/api',
  supabaseUrl: 'https://<prod-ref>.supabase.co',
  supabaseAnonKey: '<prod-anon-key>',
};
```

Fill in the Supabase project values from the `yaam-dev` and `yaam-prod` dashboards under **Project Settings → API**.

- [ ] **Step 3: Create `AuthService`**

Create `frontend/src/app/core/auth/auth.service.ts`:

```typescript
import { Injectable, signal, computed } from '@angular/core';
import { createClient, SupabaseClient, Session } from '@supabase/supabase-js';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly supabase: SupabaseClient;
  readonly session = signal<Session | null>(null);
  readonly isAuthenticated = computed(() => this.session() !== null);

  constructor() {
    this.supabase = createClient(environment.supabaseUrl, environment.supabaseAnonKey);
    this.supabase.auth.getSession().then(({ data }) => {
      this.session.set(data.session);
    });
    this.supabase.auth.onAuthStateChange((_, session) => {
      this.session.set(session);
    });
  }

  signIn(email: string, password: string) {
    return this.supabase.auth.signInWithPassword({ email, password });
  }

  signUp(email: string, password: string) {
    return this.supabase.auth.signUp({ email, password });
  }

  signOut() {
    return this.supabase.auth.signOut();
  }

  resetPassword(email: string) {
    return this.supabase.auth.resetPasswordForEmail(email, {
      redirectTo: `${window.location.origin}/auth/update-password`,
    });
  }

  updatePassword(newPassword: string) {
    return this.supabase.auth.updateUser({ password: newPassword });
  }

  exchangeCodeForSession(code: string) {
    return this.supabase.auth.exchangeCodeForSession(code);
  }

  getAccessToken(): string | null {
    return this.session()?.access_token ?? null;
  }
}
```

- [ ] **Step 4: Create `AuthGuard`**

Create `frontend/src/app/core/auth/auth.guard.ts`:

```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url } });
  return false;
};
```

- [ ] **Step 5: Create `auth.interceptor.ts`**

Create `frontend/src/app/core/interceptors/auth.interceptor.ts`:

```typescript
import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '../auth/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getAccessToken();
  if (!token) return next(req);
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
```

- [ ] **Step 6: Register interceptor in `app.config.ts`**

Replace `frontend/src/app/app.config.ts` with:

```typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { problemDetailsInterceptor } from './core/interceptors/problem-details.interceptor';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, problemDetailsInterceptor])),
  ],
};
```

- [ ] **Step 7: Protect existing routes in `app.routes.ts`**

Replace `frontend/src/app/app.routes.ts` with:

```typescript
import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'applications', pathMatch: 'full' },
  {
    path: 'applications',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/applications/applications.routes').then((m) => m.APPLICATIONS_ROUTES),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadChildren: () => import('./features/profile/profile.routes').then((m) => m.PROFILE_ROUTES),
  },
  {
    path: 'cover-letters',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/cover-letters/cover-letters.routes').then((m) => m.COVER_LETTERS_ROUTES),
  },
  {
    path: 'reminders',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/reminders/reminders.routes').then((m) => m.REMINDERS_ROUTES),
  },
  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auth/pages/account-settings/account-settings.component').then(
        (m) => m.AccountSettingsComponent,
      ),
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
];
```

- [ ] **Step 8: Verify the app compiles**

```bash
cd frontend && ng build --configuration development
```
Expected: build succeeds (no component files yet for auth pages, but routes lazy-load so no compile error until those modules are imported).

If `ng build` fails because auth page components are referenced by `auth.routes.ts`, that file is still empty — this step should pass. Proceed to Task 4 to populate it.

- [ ] **Step 9: Commit**

```bash
git add frontend/package.json \
        frontend/package-lock.json \
        frontend/src/environments/environment.ts \
        frontend/src/environments/environment.prod.ts \
        frontend/src/app/core/auth/auth.service.ts \
        frontend/src/app/core/auth/auth.guard.ts \
        frontend/src/app/core/interceptors/auth.interceptor.ts \
        frontend/src/app/app.config.ts \
        frontend/src/app/app.routes.ts
git commit -m "feat(auth): add Supabase auth service, guard, and interceptor"
```

---

### Task 4: Frontend — Login page

**Files:**
- Create: `frontend/src/app/features/auth/pages/login/login.component.ts`
- Create: `frontend/src/app/features/auth/pages/login/login.component.html`
- Modify: `frontend/src/app/features/auth/auth.routes.ts`

**Interfaces:**
- Consumes: `AuthService.signIn(email, password)` from Task 3
- Consumes: `authGuard` — after login, if `isAuthenticated` becomes true the guard lets through

- [ ] **Step 1: Create `login.component.ts`**

Create `frontend/src/app/features/auth/pages/login/login.component.ts`:

```typescript
import { Component, inject, signal } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormField, FormRoot, RouterLink],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly formModel = signal({ email: '', password: '' });

  protected readonly fields = form(this.formModel, (f) => {
    required(f.email, { message: 'Email is required.' });
    required(f.password, { message: 'Password is required.' });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.submitting.set(true);
      this.error.set(null);
      try {
        const model = this.formModel();
        const { error } = await this.auth.signIn(model.email, model.password);
        if (error) {
          this.error.set('Invalid email or password.');
          return;
        }
        const returnUrl =
          this.route.snapshot.queryParamMap.get('returnUrl') ?? '/applications';
        this.router.navigateByUrl(returnUrl);
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
```

- [ ] **Step 2: Create `login.component.html`**

Create `frontend/src/app/features/auth/pages/login/login.component.html`:

```html
<div class="min-h-screen flex items-center justify-center bg-base-200">
  <div class="card w-full max-w-sm bg-base-100 shadow-xl">
    <div class="card-body">
      <h1 class="card-title text-2xl justify-center mb-2">Sign in</h1>

      @if (error()) {
        <div class="alert alert-error mb-2">{{ error() }}</div>
      }

      <form [formRoot]="fields" (ngSubmit)="onSubmit()">
        <div class="form-control mb-3">
          <label class="label" for="email">
            <span class="label-text">Email</span>
          </label>
          <input
            id="email"
            [formField]="fields.email"
            type="email"
            autocomplete="email"
            class="input input-bordered w-full"
          />
          @if (fields.email().touched() && fields.email().errors().length) {
            <div class="label">
              <span class="label-text-alt text-error">{{ fields.email().errors()[0].message }}</span>
            </div>
          }
        </div>

        <div class="form-control mb-1">
          <label class="label" for="password">
            <span class="label-text">Password</span>
          </label>
          <input
            id="password"
            [formField]="fields.password"
            type="password"
            autocomplete="current-password"
            class="input input-bordered w-full"
          />
          @if (fields.password().touched() && fields.password().errors().length) {
            <div class="label">
              <span class="label-text-alt text-error">{{ fields.password().errors()[0].message }}</span>
            </div>
          }
        </div>

        <div class="text-right mb-4">
          <a routerLink="/auth/forgot-password" class="link link-primary text-sm">Forgot password?</a>
        </div>

        <button type="submit" class="btn btn-primary w-full" [disabled]="submitting()">
          @if (submitting()) {
            <span class="loading loading-spinner loading-sm"></span>
          }
          Sign in
        </button>
      </form>

      <div class="divider"></div>
      <p class="text-center text-sm">
        No account?
        <a routerLink="/auth/register" class="link link-primary">Create one</a>
      </p>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Add login route to `auth.routes.ts`**

Replace `frontend/src/app/features/auth/auth.routes.ts` with:

```typescript
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
];
```

- [ ] **Step 4: Build to verify no compile errors**

```bash
cd frontend && ng build --configuration development
```
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/auth/pages/login/ \
        frontend/src/app/features/auth/auth.routes.ts
git commit -m "feat(auth): add login page"
```

---

### Task 5: Frontend — Register page

**Files:**
- Create: `frontend/src/app/features/auth/pages/register/register.component.ts`
- Create: `frontend/src/app/features/auth/pages/register/register.component.html`
- Modify: `frontend/src/app/features/auth/auth.routes.ts`

**Interfaces:**
- Consumes: `AuthService.signUp(email, password)` from Task 3
- On success: navigate to `/profile`

- [ ] **Step 1: Create `register.component.ts`**

Create `frontend/src/app/features/auth/pages/register/register.component.ts`:

```typescript
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormField, FormRoot, form, required, validate, submit } from '@angular/forms/signals';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormField, FormRoot, RouterLink],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly formModel = signal({ email: '', password: '', confirmPassword: '' });

  protected readonly fields = form(this.formModel, (f) => {
    required(f.email, { message: 'Email is required.' });
    required(f.password, { message: 'Password is required.' });
    validate(f.password, (ctx) => {
      const value = ctx.value() as string;
      if (!value) return null;
      if (value.length < 12)
        return { kind: 'minLength', message: 'Password must be at least 12 characters.' };
      if (!/[0-9\W_]/.test(value))
        return {
          kind: 'complexity',
          message: 'Password must contain at least one number or special character.',
        };
      return null;
    });
    required(f.confirmPassword, { message: 'Please confirm your password.' });
    validate(f.confirmPassword, (ctx) => {
      const confirm = ctx.value() as string;
      const password = this.formModel().password;
      return confirm !== password ? { kind: 'mismatch', message: 'Passwords do not match.' } : null;
    });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.submitting.set(true);
      this.error.set(null);
      try {
        const model = this.formModel();
        const { error } = await this.auth.signUp(model.email, model.password);
        if (error) {
          this.error.set(error.message);
          return;
        }
        this.router.navigateByUrl('/profile');
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
```

- [ ] **Step 2: Create `register.component.html`**

Create `frontend/src/app/features/auth/pages/register/register.component.html`:

```html
<div class="min-h-screen flex items-center justify-center bg-base-200">
  <div class="card w-full max-w-sm bg-base-100 shadow-xl">
    <div class="card-body">
      <h1 class="card-title text-2xl justify-center mb-2">Create account</h1>

      @if (error()) {
        <div class="alert alert-error mb-2">{{ error() }}</div>
      }

      <form [formRoot]="fields" (ngSubmit)="onSubmit()">
        <div class="form-control mb-3">
          <label class="label" for="email">
            <span class="label-text">Email</span>
          </label>
          <input
            id="email"
            [formField]="fields.email"
            type="email"
            autocomplete="email"
            class="input input-bordered w-full"
          />
          @if (fields.email().touched() && fields.email().errors().length) {
            <div class="label">
              <span class="label-text-alt text-error">{{ fields.email().errors()[0].message }}</span>
            </div>
          }
        </div>

        <div class="form-control mb-3">
          <label class="label" for="password">
            <span class="label-text">Password</span>
          </label>
          <input
            id="password"
            [formField]="fields.password"
            type="password"
            autocomplete="new-password"
            class="input input-bordered w-full"
          />
          <div class="label">
            <span class="label-text-alt text-base-content/60">
              Min 12 characters, at least one number or special character
            </span>
          </div>
          @if (fields.password().touched() && fields.password().errors().length) {
            <div class="label">
              <span class="label-text-alt text-error">{{ fields.password().errors()[0].message }}</span>
            </div>
          }
        </div>

        <div class="form-control mb-4">
          <label class="label" for="confirmPassword">
            <span class="label-text">Confirm password</span>
          </label>
          <input
            id="confirmPassword"
            [formField]="fields.confirmPassword"
            type="password"
            autocomplete="new-password"
            class="input input-bordered w-full"
          />
          @if (fields.confirmPassword().touched() && fields.confirmPassword().errors().length) {
            <div class="label">
              <span class="label-text-alt text-error">{{ fields.confirmPassword().errors()[0].message }}</span>
            </div>
          }
        </div>

        <button type="submit" class="btn btn-primary w-full" [disabled]="submitting()">
          @if (submitting()) {
            <span class="loading loading-spinner loading-sm"></span>
          }
          Create account
        </button>
      </form>

      <div class="divider"></div>
      <p class="text-center text-sm">
        Already have an account?
        <a routerLink="/auth/login" class="link link-primary">Sign in</a>
      </p>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Add register route to `auth.routes.ts`**

Open `frontend/src/app/features/auth/auth.routes.ts`. Add the register route:

```typescript
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register.component').then((m) => m.RegisterComponent),
  },
];
```

- [ ] **Step 4: Build to verify**

```bash
cd frontend && ng build --configuration development
```
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/auth/pages/register/ \
        frontend/src/app/features/auth/auth.routes.ts
git commit -m "feat(auth): add register page"
```

---

### Task 6: Frontend — Forgot password page

**Files:**
- Create: `frontend/src/app/features/auth/pages/forgot-password/forgot-password.component.ts`
- Create: `frontend/src/app/features/auth/pages/forgot-password/forgot-password.component.html`
- Modify: `frontend/src/app/features/auth/auth.routes.ts`

**Interfaces:**
- Consumes: `AuthService.resetPassword(email)` from Task 3
- Shows generic confirmation regardless of whether email exists (prevents enumeration)

- [ ] **Step 1: Create `forgot-password.component.ts`**

Create `frontend/src/app/features/auth/pages/forgot-password/forgot-password.component.ts`:

```typescript
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [FormField, FormRoot, RouterLink],
  templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent {
  private readonly auth = inject(AuthService);

  protected readonly submitting = signal(false);
  protected readonly submitted = signal(false);

  protected readonly formModel = signal({ email: '' });

  protected readonly fields = form(this.formModel, (f) => {
    required(f.email, { message: 'Email is required.' });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.submitting.set(true);
      try {
        await this.auth.resetPassword(this.formModel().email);
        this.submitted.set(true);
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
```

- [ ] **Step 2: Create `forgot-password.component.html`**

Create `frontend/src/app/features/auth/pages/forgot-password/forgot-password.component.html`:

```html
<div class="min-h-screen flex items-center justify-center bg-base-200">
  <div class="card w-full max-w-sm bg-base-100 shadow-xl">
    <div class="card-body">
      @if (submitted()) {
        <h1 class="card-title text-2xl justify-center mb-2">Check your email</h1>
        <p class="text-center text-sm text-base-content/70">
          If that address is registered, you'll receive a password reset link shortly.
        </p>
        <div class="mt-4 text-center">
          <a routerLink="/auth/login" class="link link-primary text-sm">Back to sign in</a>
        </div>
      } @else {
        <h1 class="card-title text-2xl justify-center mb-2">Reset password</h1>
        <p class="text-sm text-base-content/70 mb-4">
          Enter your email address and we'll send you a reset link.
        </p>

        <form [formRoot]="fields" (ngSubmit)="onSubmit()">
          <div class="form-control mb-4">
            <label class="label" for="email">
              <span class="label-text">Email</span>
            </label>
            <input
              id="email"
              [formField]="fields.email"
              type="email"
              autocomplete="email"
              class="input input-bordered w-full"
            />
            @if (fields.email().touched() && fields.email().errors().length) {
              <div class="label">
                <span class="label-text-alt text-error">{{ fields.email().errors()[0].message }}</span>
              </div>
            }
          </div>

          <button type="submit" class="btn btn-primary w-full" [disabled]="submitting()">
            @if (submitting()) {
              <span class="loading loading-spinner loading-sm"></span>
            }
            Send reset link
          </button>
        </form>

        <div class="mt-4 text-center">
          <a routerLink="/auth/login" class="link link-primary text-sm">Back to sign in</a>
        </div>
      }
    </div>
  </div>
</div>
```

- [ ] **Step 3: Add forgot-password route to `auth.routes.ts`**

Replace `frontend/src/app/features/auth/auth.routes.ts` with:

```typescript
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/forgot-password/forgot-password.component').then(
        (m) => m.ForgotPasswordComponent,
      ),
  },
];
```

- [ ] **Step 4: Build to verify**

```bash
cd frontend && ng build --configuration development
```
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/auth/pages/forgot-password/ \
        frontend/src/app/features/auth/auth.routes.ts
git commit -m "feat(auth): add forgot password page"
```

---

### Task 7: Frontend — Update password page (reset email link landing)

**Files:**
- Create: `frontend/src/app/features/auth/pages/update-password/update-password.component.ts`
- Create: `frontend/src/app/features/auth/pages/update-password/update-password.component.html`
- Modify: `frontend/src/app/features/auth/auth.routes.ts`

**Interfaces:**
- Consumes: `AuthService.exchangeCodeForSession(code)` — exchanges `?code=` query param
- Consumes: `AuthService.updatePassword(newPassword)`
- Consumes: `AuthService.signOut()` — called after update to invalidate all sessions
- On success: navigate to `/auth/login`
- On expired link: show message and link to `/auth/forgot-password`

- [ ] **Step 1: Create `update-password.component.ts`**

Create `frontend/src/app/features/auth/pages/update-password/update-password.component.ts`:

```typescript
import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormField, FormRoot, form, required, validate, submit } from '@angular/forms/signals';
import { AuthService } from '../../../../core/auth/auth.service';

type PageState = 'loading' | 'ready' | 'invalid-link' | 'success';

@Component({
  selector: 'app-update-password',
  standalone: true,
  imports: [FormField, FormRoot, RouterLink],
  templateUrl: './update-password.component.html',
})
export class UpdatePasswordComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly pageState = signal<PageState>('loading');
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly formModel = signal({ password: '', confirmPassword: '' });

  protected readonly fields = form(this.formModel, (f) => {
    required(f.password, { message: 'Password is required.' });
    validate(f.password, (ctx) => {
      const value = ctx.value() as string;
      if (!value) return null;
      if (value.length < 12)
        return { kind: 'minLength', message: 'Password must be at least 12 characters.' };
      if (!/[0-9\W_]/.test(value))
        return {
          kind: 'complexity',
          message: 'Password must contain at least one number or special character.',
        };
      return null;
    });
    required(f.confirmPassword, { message: 'Please confirm your password.' });
    validate(f.confirmPassword, (ctx) => {
      const confirm = ctx.value() as string;
      const password = this.formModel().password;
      return confirm !== password ? { kind: 'mismatch', message: 'Passwords do not match.' } : null;
    });
  });

  async ngOnInit(): Promise<void> {
    const code = this.route.snapshot.queryParamMap.get('code');
    if (!code) {
      this.pageState.set('invalid-link');
      return;
    }
    const { error } = await this.auth.exchangeCodeForSession(code);
    this.pageState.set(error ? 'invalid-link' : 'ready');
  }

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.submitting.set(true);
      this.error.set(null);
      try {
        const { error } = await this.auth.updatePassword(this.formModel().password);
        if (error) {
          this.error.set(error.message);
          return;
        }
        await this.auth.signOut();
        this.router.navigateByUrl('/auth/login');
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
```

- [ ] **Step 2: Create `update-password.component.html`**

Create `frontend/src/app/features/auth/pages/update-password/update-password.component.html`:

```html
<div class="min-h-screen flex items-center justify-center bg-base-200">
  <div class="card w-full max-w-sm bg-base-100 shadow-xl">
    <div class="card-body">

      @switch (pageState()) {
        @case ('loading') {
          <div class="flex justify-center py-8">
            <span class="loading loading-spinner loading-lg"></span>
          </div>
        }

        @case ('invalid-link') {
          <h1 class="card-title text-2xl justify-center mb-2">Link expired</h1>
          <p class="text-sm text-base-content/70 mb-4 text-center">
            This password reset link has expired or is invalid. Request a new one.
          </p>
          <a routerLink="/auth/forgot-password" class="btn btn-primary w-full">
            Request new link
          </a>
        }

        @case ('ready') {
          <h1 class="card-title text-2xl justify-center mb-2">Set new password</h1>

          @if (error()) {
            <div class="alert alert-error mb-2">{{ error() }}</div>
          }

          <form [formRoot]="fields" (ngSubmit)="onSubmit()">
            <div class="form-control mb-3">
              <label class="label" for="password">
                <span class="label-text">New password</span>
              </label>
              <input
                id="password"
                [formField]="fields.password"
                type="password"
                autocomplete="new-password"
                class="input input-bordered w-full"
              />
              <div class="label">
                <span class="label-text-alt text-base-content/60">
                  Min 12 characters, at least one number or special character
                </span>
              </div>
              @if (fields.password().touched() && fields.password().errors().length) {
                <div class="label">
                  <span class="label-text-alt text-error">{{ fields.password().errors()[0].message }}</span>
                </div>
              }
            </div>

            <div class="form-control mb-4">
              <label class="label" for="confirmPassword">
                <span class="label-text">Confirm new password</span>
              </label>
              <input
                id="confirmPassword"
                [formField]="fields.confirmPassword"
                type="password"
                autocomplete="new-password"
                class="input input-bordered w-full"
              />
              @if (fields.confirmPassword().touched() && fields.confirmPassword().errors().length) {
                <div class="label">
                  <span class="label-text-alt text-error">{{ fields.confirmPassword().errors()[0].message }}</span>
                </div>
              }
            </div>

            <button type="submit" class="btn btn-primary w-full" [disabled]="submitting()">
              @if (submitting()) {
                <span class="loading loading-spinner loading-sm"></span>
              }
              Set password
            </button>
          </form>
        }
      }

    </div>
  </div>
</div>
```

- [ ] **Step 3: Add update-password route to `auth.routes.ts`**

Replace `frontend/src/app/features/auth/auth.routes.ts` with:

```typescript
import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/forgot-password/forgot-password.component').then(
        (m) => m.ForgotPasswordComponent,
      ),
  },
  {
    path: 'update-password',
    loadComponent: () =>
      import('./pages/update-password/update-password.component').then(
        (m) => m.UpdatePasswordComponent,
      ),
  },
];
```

- [ ] **Step 4: Build to verify**

```bash
cd frontend && ng build --configuration development
```
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/auth/pages/update-password/ \
        frontend/src/app/features/auth/auth.routes.ts
git commit -m "feat(auth): add update password page for reset flow"
```

---

### Task 8: Frontend — Account settings page (change password)

**Files:**
- Create: `frontend/src/app/features/auth/pages/account-settings/account-settings.component.ts`
- Create: `frontend/src/app/features/auth/pages/account-settings/account-settings.component.html`

**Interfaces:**
- Consumes: `AuthService.updatePassword(newPassword)` from Task 3
- Route is `/account` (already added in Task 3's `app.routes.ts` with `authGuard`)
- On success: stay on page, show confirmation; current session remains valid

- [ ] **Step 1: Create `account-settings.component.ts`**

Create `frontend/src/app/features/auth/pages/account-settings/account-settings.component.ts`:

```typescript
import { Component, inject, signal } from '@angular/core';
import { FormField, FormRoot, form, required, validate, submit } from '@angular/forms/signals';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [FormField, FormRoot],
  templateUrl: './account-settings.component.html',
})
export class AccountSettingsComponent {
  private readonly auth = inject(AuthService);

  protected readonly submitting = signal(false);
  protected readonly success = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly formModel = signal({ password: '', confirmPassword: '' });

  protected readonly fields = form(this.formModel, (f) => {
    required(f.password, { message: 'Password is required.' });
    validate(f.password, (ctx) => {
      const value = ctx.value() as string;
      if (!value) return null;
      if (value.length < 12)
        return { kind: 'minLength', message: 'Password must be at least 12 characters.' };
      if (!/[0-9\W_]/.test(value))
        return {
          kind: 'complexity',
          message: 'Password must contain at least one number or special character.',
        };
      return null;
    });
    required(f.confirmPassword, { message: 'Please confirm your password.' });
    validate(f.confirmPassword, (ctx) => {
      const confirm = ctx.value() as string;
      const password = this.formModel().password;
      return confirm !== password ? { kind: 'mismatch', message: 'Passwords do not match.' } : null;
    });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.submitting.set(true);
      this.success.set(false);
      this.error.set(null);
      try {
        const { error } = await this.auth.updatePassword(this.formModel().password);
        if (error) {
          this.error.set(error.message);
          return;
        }
        this.success.set(true);
        this.formModel.set({ password: '', confirmPassword: '' });
        this.fields().reset();
      } finally {
        this.submitting.set(false);
      }
    });
  }
}
```

- [ ] **Step 2: Create `account-settings.component.html`**

Create `frontend/src/app/features/auth/pages/account-settings/account-settings.component.html`:

```html
<div class="container mx-auto p-6 max-w-lg">
  <h1 class="text-2xl font-bold mb-6">Account settings</h1>

  <div class="card bg-base-100 shadow">
    <div class="card-body">
      <h2 class="card-title text-lg mb-4">Change password</h2>

      @if (success()) {
        <div class="alert alert-success mb-4">Password updated. Other sessions have been signed out.</div>
      }

      @if (error()) {
        <div class="alert alert-error mb-4">{{ error() }}</div>
      }

      <form [formRoot]="fields" (ngSubmit)="onSubmit()">
        <div class="form-control mb-3">
          <label class="label" for="password">
            <span class="label-text">New password</span>
          </label>
          <input
            id="password"
            [formField]="fields.password"
            type="password"
            autocomplete="new-password"
            class="input input-bordered w-full"
          />
          <div class="label">
            <span class="label-text-alt text-base-content/60">
              Min 12 characters, at least one number or special character
            </span>
          </div>
          @if (fields.password().touched() && fields.password().errors().length) {
            <div class="label">
              <span class="label-text-alt text-error">{{ fields.password().errors()[0].message }}</span>
            </div>
          }
        </div>

        <div class="form-control mb-4">
          <label class="label" for="confirmPassword">
            <span class="label-text">Confirm new password</span>
          </label>
          <input
            id="confirmPassword"
            [formField]="fields.confirmPassword"
            type="password"
            autocomplete="new-password"
            class="input input-bordered w-full"
          />
          @if (fields.confirmPassword().touched() && fields.confirmPassword().errors().length) {
            <div class="label">
              <span class="label-text-alt text-error">{{ fields.confirmPassword().errors()[0].message }}</span>
            </div>
          }
        </div>

        <button type="submit" class="btn btn-primary" [disabled]="submitting()">
          @if (submitting()) {
            <span class="loading loading-spinner loading-sm"></span>
          }
          Update password
        </button>
      </form>
    </div>
  </div>
</div>
```

- [ ] **Step 3: Build to verify**

```bash
cd frontend && ng build --configuration development
```
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/auth/pages/account-settings/
git commit -m "feat(auth): add account settings page with change password"
```

---

## Self-Review

### Spec coverage check

| Requirement | Covered by |
|------------|-----------|
| Story 1: Register + onboarding redirect to `/profile` | Task 5 |
| Story 1: Duplicate email error | Supabase returns error; shown as `error()` in register page |
| Story 1: Password requirements field validation | Task 5 — `validate()` on `f.password` |
| Story 2: Login + redirect to applications | Task 4 |
| Story 2: Wrong credentials → generic error | Task 4 — always shows "Invalid email or password." |
| Story 2: Session expiry → redirect to login | `onAuthStateChange` fires `SIGNED_OUT`; `authGuard` redirects on next nav |
| Story 2: Protected routes redirect unauthenticated | Task 3 — `authGuard` on all existing routes |
| Story 2: Post-login return to attempted URL | Task 4 — reads `?returnUrl` from query params |
| Story 3: Forgot password → generic confirmation | Task 6 — `submitted()` signal; no email existence revealed |
| Story 3: Reset link → update-password page | Task 7 — exchanges `?code=`, shows form |
| Story 3: Expired link → message + re-request link | Task 7 — `invalid-link` state |
| Story 3: Successful reset → login redirect, all sessions invalidated | Task 7 — `signOut()` after `updateUser`, navigate to login |
| Story 4: Account settings → change password | Task 8 |
| Story 4: Other sessions invalidated, current stays | Supabase `updateUser({ password })` behavior |
| Backend JWT validation | Task 1+2 |
| Existing integration tests still pass | Task 2 — `CreateAuthenticatedClient` |
| `ICurrentUserService` available for Phase 2 | Task 1 |

### Gap check

- **Supabase dashboard setup:** The spec notes "app URL added to allowed redirect URLs for the password reset flow." This is a manual step in the Supabase dashboard, not code. Include in PR description / deployment notes: add `http://localhost:4200/auth/update-password` (dev) and the production URL to Supabase Auth → URL Configuration → Redirect URLs.
- **Session expiry message ("session has ended"):** The spec requires showing a message on the login page when a session expires. This is not implemented — the guard redirects to `/auth/login?returnUrl=...` but no expiry signal is passed. This is a minor UX gap. To cover it: `AuthService.onAuthStateChange` could detect a `SIGNED_OUT` event triggered by token expiry and navigate with a `?reason=expired` param; the login page would show a banner when that param is present. This can be added as a follow-up task if desired.
- **No placeholder** strings or TODOs found in the plan above.

### Type consistency check

- `formModel` signals use consistent shape across all components: `signal({ fieldA: '', fieldB: '' })`
- `fields = form(this.formModel, ...)` pattern matches existing codebase exactly
- `validate(ctx)` returns `{ kind: string, message: string } | null` — matches reminder component usage
- `AuthService` method return types are Supabase's `AuthResponse` — components destructure `{ error }` consistently
