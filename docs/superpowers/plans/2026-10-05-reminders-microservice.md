# Reminders Microservice Extraction — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move Reminders out of the YAAM monolith into its own service with its own database, behind a YARP gateway. Application data reaches it through a hand-built transactional outbox → RabbitMQ → an idempotent, version-aware consumer. The flow is traced end-to-end with OpenTelemetry.

**Architecture:** This is a strangler-fig extraction.
- The monolith keeps Applications/Notes/Profile/CV. It writes `ApplicationUpserted`/`ApplicationDeleted` integration events into an `OutboxMessages` table in the same transaction as the change, and a relay publishes them to a topic exchange.
- `Yaam.Reminders` keeps a local `ApplicationSnapshots` read model. It deduplicates incoming messages with a `ProcessedMessages` inbox and rejects stale versions.
- The frontend talks only to the gateway.

**Tech Stack:**
- .NET 10, ASP.NET Core, EF Core 10 + Npgsql, MediatR 14, FluentValidation 12
- YARP 2.3.0, RabbitMQ.Client 7.2.2 (RabbitMQ 4 broker)
- OpenTelemetry 1.19.x → `grafana/otel-lgtm`
- Angular 22 with the OpenAPI typescript-angular generator

**Spec:** `docs/specs/2026-10-05-reminders-microservice.md`

## Global Constraints

- All .NET projects: `net10.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.
- **Package versions:**

  | Package | Version |
  |---|---|
  | Yarp.ReverseProxy | 2.3.0 |
  | RabbitMQ.Client | 7.2.2 |
  | OpenTelemetry.Extensions.Hosting | 1.19.1 |
  | OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.19.1 |
  | OpenTelemetry.Instrumentation.AspNetCore | 1.19.0 |
  | OpenTelemetry.Instrumentation.Http | 1.19.0 |
  | Npgsql.OpenTelemetry | 10.0.3 |
  | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 |
  | Microsoft.EntityFrameworkCore.Design | 10.0.9 |
  | Microsoft.AspNetCore.OpenApi | 10.0.9 |
  | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.9 |
  | MediatR | 14.2.0 |
  | FluentValidation (+ .DependencyInjectionExtensions) | 12.1.1 |
  | MailKit | 4.17.0 |

  Test projects use the same package versions as `backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj`.
- **Ports:**

  | Component | Port(s) |
  |---|---|
  | Gateway | **5100** (not 5000: macOS AirPlay Receiver occupies 5000) |
  | Monolith | 5231 |
  | Reminders | 5240 (second replica 5241) |
  | Postgres | 5432 |
  | RabbitMQ | 5672 (management UI 15672) |
  | Grafana | 3000 |
  | OTLP | 4317/4318 |
  | Angular | 4200 |
- **`Yaam.Reminders` boundaries:**
  - It must never reference `Yaam.Domain`, `Yaam.UseCases`, `Yaam.Infrastructure` or `Yaam.API`.
  - It must never connect to the `yaam` database.
  - It may reference only `Yaam.Contracts` and `Yaam.ServiceDefaults`.
- **Backend naming (CLAUDE.md):**
  - Handler parameters are named `command` / `query`.
  - No abbreviations (`cancellationToken`, `exception`, `configuration`).
  - No default parameter values.
  - `mediator.Send(` always takes three lines.
  - `*InputModel` records sit at the bottom of the controller file.
  - Handlers return `*Dto`; controllers return `*ViewModel`, mapped by a mapper in its own file.
  - Settings classes use `{ get; init; }`.
- **Inside `Yaam.Reminders`:** handlers use `RemindersDbContext` directly (no repository layer). The service boundary is the abstraction. This decision is recorded in ADR 007.
- **Out of scope: rich domain models** (spec, Non-goals). `Reminder` stays anemic as ported from the monolith. Do not add entity methods or guard clauses, and do not move business rules out of handlers in this plan. Task 14's notes list it under "What I'd do next".
- **Comments:** sparse; only where the *why* is non-obvious.
- **Frontend:**
  - Never edit `frontend/src/app/generated/**` by hand. Regenerate with `npm run generate:api` (deleting the output folder first is allowed).
  - daisyUI for UI. Invoke the `daisyui` skill before touching templates.
  - Signal forms only.
  - Run the `verify` skill after frontend changes.
- **Commits:**
  - Conventional Commits.
  - No `Co-Authored-By` trailers, no AI attribution.
  - Commit at the end of each task.
- **EF commands** are run from `backend/` and pass `-- --environment Development`, so `Program` finds the connection string.
- **Checkpoints:** stop at every `CHECKPOINT` and wait for the user before continuing. This is a learning project; the user reviews each milestone's diff.

## Review Focus

These are the failure modes most likely to bite. Each one has a pinned test or verification step in the task named.

1. **Broker unavailable at startup.** The monolith and Reminders must start and serve HTTP while RabbitMQ is down; the relay and consumer retry with a log warning. *(Task 9, Step 6; Task 10, Step 8)*
2. **Events published before the Reminders queue exists** are silently dropped by RabbitMQ unless published `mandatory`. Expected: outbox rows stay pending until a queue is bound. *(Task 9, Step 7)*
3. **Two concurrent updates of the same application** would both produce `Version = n+1`, and the consumer would drop one as stale even though its content differs. Expected: `Version` is a concurrency token; the second save returns 409 and writes no outbox row. *(Task 8 test `ConcurrentUpdate_Returns409Conflict_AndWritesNoOutboxMessage`)*
4. **Delete arriving before any upsert, or an upsert arriving after a delete.** Expected: a tombstone prevents the application coming back. *(Task 10 tests)*
5. **An unknown event type** (newer producer) must not poison the queue: it is acknowledged and recorded. **A malformed payload** must throw, so it is nacked and dead-lettered. *(Task 10 tests)*

---

## Milestone 0 — Decisions on record

### Task 0: ADRs and spec refinements

Planning against the real code refined several spec details. Record them in the spec and capture the decisions as ADRs.

**Files:**
- Modify: `docs/specs/2026-10-05-reminders-microservice.md`
- Create: `docs/architecture/decisions/007-extract-reminders-service.md`
- Create: `docs/architecture/decisions/008-transactional-outbox-and-inbox.md`
- Create: `docs/architecture/decisions/009-gateway-auth-and-api-clients.md`
- Create: `docs/architecture/decisions/010-shared-contracts-and-service-defaults.md`

- [ ] **Step 1: Apply spec refinements.** In the spec, change the following:
  - **`MessageEnvelope`:** remove `TraceParent`. Trace context travels in the AMQP `traceparent` header, the W3C convention that tools understand. The envelope is `(Guid MessageId, string Type, DateTimeOffset OccurredAt, JsonElement Payload)`.
  - **Routing keys:** `application.upserted` / `application.deleted`, not created/updated/deleted.
  - **Credentials:** locally both services use the same Postgres user `yaam` with separate databases and connection strings. In production each service would get its own role.
  - **Metrics:** one counter `yaam.consumer.messages` with an `outcome` tag (`processed|duplicate|stale|ignored|failed`). Dead-lettering is observed in the RabbitMQ UI, not counted by the consumer.
  - **Gateway port:** 5100.
  - **New `Yaam.ServiceDefaults` class library:** cross-cutting plumbing shared by all three services. It holds JWT auth, the RabbitMQ connection, and OpenTelemetry.
  - **Unknown event types are acknowledged and ignored** (tolerant reader). **Malformed payloads throw** and are dead-lettered.
  - **The relay publishes with `mandatory: true`.** An unroutable message (no queue bound yet) fails the publish, and the outbox row stays pending.
  - **`Application.Version` is an EF concurrency token.** A conflicting save surfaces as `ConflictException` → 409.
  - **Reminders HTTP API returns a single `ReminderViewModel`** (reminder + snapshot fields) from list, create and reschedule.
  - **Tests:** a single test project `Yaam.Reminders.Tests` with `Unit/` and `Integration/` folders (not `Yaam.Reminders.Tests.Integration`).

- [ ] **Step 2: Write the four ADRs** in the existing ADR format (`# ADR 00X: <title>`, `**Date:** 2026-10-05`, `**Status:** Accepted`, `## Context`, `## Decision`, `## Consequences`). Content:
  - **007 – Extract Reminders as a service.**
    - Decisions: Reminders first, because it is where the coupling is. Asynchronous state transfer (local snapshot) over a sync lookup. Single-project service with feature folders. Handlers use the DbContext directly.
    - Consequences: eventual consistency (a just-created application may 404 for a reminder for a few hundred ms). No cascade FK; deletion is event-driven.
  - **008 – Hand-built transactional outbox and idempotent inbox.**
    - Decisions:
      - The monolith stages outbox rows from a ChangeTracker diff in `SaveChangesAsync`. This is **transitional**: extracted services raise domain events and map them to integration events.
      - The relay uses `FOR UPDATE SKIP LOCKED` + publisher confirms + mandatory.
      - The consumer uses an inbox table + monotonic `Version` + tombstones.
      - Quorum queue with delivery limit 5 → DLQ.
    - Consequences: at-least-once delivery; `ProcessedMessages` grows without bound (future: retention job). MassTransit/Wolverine would replace this.
  - **009 – Gateway, auth and API clients.**
    - Decisions:
      - YARP as a routing-only gateway.
      - Every service validates the Supabase JWT (zero trust).
      - URL prefix = owning service.
      - Two generated OpenAPI clients.
      - TLS terminates at the edge (services drop `UseHttpsRedirection`).
  - **010 – Shared contracts and service defaults.**
    - Decisions:
      - `Yaam.Contracts` holds only event records + topology names. It is additive-only.
      - `Yaam.ServiceDefaults` holds plumbing only (auth, messaging connection, telemetry), never business code.
      - Small things like `ValidationBehavior` and `GlobalExceptionHandler` are **copied** into Reminders instead of shared ("prefer duplication over coupling").

- [ ] **Step 3: Commit**

```bash
git add docs/specs/2026-10-05-reminders-microservice.md docs/architecture/decisions/007-*.md docs/architecture/decisions/008-*.md docs/architecture/decisions/009-*.md docs/architecture/decisions/010-*.md
git commit -m "docs(architecture): ADRs for reminders service extraction"
```

---

## Milestone 1 — Gateway (strangler facade)

### Task 1: YARP gateway in front of the monolith; frontend through the gateway

**Files:**
- Create: `backend/Yaam.Gateway/Yaam.Gateway.csproj`
- Create: `backend/Yaam.Gateway/Program.cs`
- Create: `backend/Yaam.Gateway/appsettings.json`
- Create: `backend/Yaam.Gateway/Properties/launchSettings.json`
- Modify: `backend/Yaam.slnx`
- Modify: `backend/Yaam.API/Program.cs` (remove `app.UseHttpsRedirection();`)
- Modify: `frontend/proxy.conf.json`
- Modify: `frontend/src/app/app.config.ts`

**Interfaces:**
- Produces: the gateway on `http://localhost:5100`, cluster `monolith` → `http://localhost:5231`. Task 6 adds cluster `reminders`.

- [ ] **Step 1: Create the project**

`backend/Yaam.Gateway/Yaam.Gateway.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Yarp.ReverseProxy" Version="2.3.0" />
  </ItemGroup>

</Project>
```

`backend/Yaam.Gateway/Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

app.Run();
```

`backend/Yaam.Gateway/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yarp": "Information"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "monolith": {
        "ClusterId": "monolith",
        "Match": { "Path": "/api/{**catch-all}" }
      }
    },
    "Clusters": {
      "monolith": {
        "Destinations": {
          "monolith-1": { "Address": "http://localhost:5231" }
        }
      }
    }
  }
}
```

`backend/Yaam.Gateway/Properties/launchSettings.json`:
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5100",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Add `<Project Path="Yaam.Gateway/Yaam.Gateway.csproj" />` to `backend/Yaam.slnx`.

- [ ] **Step 2: Remove HTTPS redirection from the monolith.** Delete the line `app.UseHttpsRedirection();` in `backend/Yaam.API/Program.cs`.
  - Reason (ADR 009): TLS terminates at the edge. With the `https` launch profile, the monolith would otherwise answer gateway calls with a 307 to `https://localhost:7131` and bypass the gateway.

- [ ] **Step 3: Point the frontend at the gateway**

`frontend/proxy.conf.json`:
```json
{
  "/api": {
    "target": "http://localhost:5100",
    "secure": false
  }
}
```

In `frontend/src/app/app.config.ts`, add the import and the provider (keep everything else):
```ts
import { BASE_PATH } from './generated/api';
```
```ts
    { provide: BASE_PATH, useValue: '' },
```
- Why: the generated `BaseService` falls back to the hard-coded `http://localhost:5231` unless `BASE_PATH` is a string.
- With `''`, every call is relative (`/api/...`), so it goes through the ng dev proxy to the gateway. Same origin, no CORS.

- [ ] **Step 4: Build**

Run: `cd backend && dotnet build`
Expected: `Build succeeded` with 0 errors.

- [ ] **Step 5: Verify routing with curl.** Start Postgres (`docker compose up -d postgres`), then in two terminals:
  - `dotnet run --project backend/Yaam.API --launch-profile http`
  - `dotnet run --project backend/Yaam.Gateway`

  Run: `curl -i http://localhost:5100/api/applications`
  Expected: `HTTP/1.1 401 Unauthorized`. The 401 comes from the monolith, which proves the gateway forwarded the request.

- [ ] **Step 6: Verify in the browser.** Run `npm start` in `frontend/`, then invoke the `verify` skill.
  - Log in and open the applications list.
  - In devtools Network, requests go to `http://localhost:4200/api/...` (relative), not `localhost:5231`.

- [ ] **Step 7: Commit**

```bash
git add backend/Yaam.Gateway backend/Yaam.slnx backend/Yaam.API/Program.cs frontend/proxy.conf.json frontend/src/app/app.config.ts
git commit -m "feat(gateway): route frontend traffic through YARP gateway"
```

**CHECKPOINT 1:** Stop. Walk the user through the strangler-fig idea: the gateway exists and nothing has moved yet. Wait for questions.

---

## Milestone 2 — Reminders service with its own database

### Task 2: `Yaam.ServiceDefaults` with shared JWT authentication

**Files:**
- Create: `backend/Yaam.ServiceDefaults/Yaam.ServiceDefaults.csproj`
- Create: `backend/Yaam.ServiceDefaults/Authentication/SupabaseSettings.cs`
- Create: `backend/Yaam.ServiceDefaults/Authentication/AuthenticationExtensions.cs`
- Delete: `backend/Yaam.Infrastructure/Auth/SupabaseSettings.cs`
- Modify: `backend/Yaam.Infrastructure/DependencyInjection.cs` (`AddYaamAuthentication`)
- Modify: `backend/Yaam.Infrastructure/Yaam.Infrastructure.csproj`
- Modify: `backend/Yaam.slnx`

**Interfaces:**
- Produces: `Yaam.ServiceDefaults.Authentication.AuthenticationExtensions.AddYaamJwtAuthentication(this IServiceCollection services, IConfiguration configuration) : IServiceCollection`

- [ ] **Step 1: Create the library**

`backend/Yaam.ServiceDefaults/Yaam.ServiceDefaults.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.9" />
  </ItemGroup>

</Project>
```

`backend/Yaam.ServiceDefaults/Authentication/SupabaseSettings.cs`:
```csharp
namespace Yaam.ServiceDefaults.Authentication;

public record SupabaseSettings
{
    public required string Url { get; init; }
}
```

`backend/Yaam.ServiceDefaults/Authentication/AuthenticationExtensions.cs`:
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Yaam.ServiceDefaults.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddYaamJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection("Supabase").Get<SupabaseSettings>()
            ?? throw new InvalidOperationException("Supabase configuration section is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{settings.Url}/auth/v1";
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{settings.Url}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                };
            });

        return services;
    }
}
```

- [ ] **Step 2: Make the monolith use it.**
  - Add `<ProjectReference Include="..\Yaam.ServiceDefaults\Yaam.ServiceDefaults.csproj" />` to `Yaam.Infrastructure.csproj` and remove the JwtBearer `PackageReference` there.
  - Delete `Yaam.Infrastructure/Auth/SupabaseSettings.cs`.
  - In `Yaam.Infrastructure/DependencyInjection.cs`, replace the body of `AddYaamAuthentication` with:

```csharp
    public static IServiceCollection AddYaamAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddYaamJwtAuthentication(configuration);
        return services;
    }
```
  - Fix the usings: add `using Yaam.ServiceDefaults.Authentication;`; remove the now-unused JwtBearer and `Microsoft.IdentityModel.Tokens` usings.
  - Add `<Project Path="Yaam.ServiceDefaults/Yaam.ServiceDefaults.csproj" />` to `Yaam.slnx`.

- [ ] **Step 3: Run the existing tests (the regression net).**

Run: `cd backend && dotnet build && dotnet test Yaam.Tests.Unit && dotnet test Yaam.Tests.Integration`
Expected: all pass. The integration tests' `PostConfigure<JwtBearerOptions>` still applies, because the scheme name is unchanged.

- [ ] **Step 4: Commit**

```bash
git add backend/Yaam.ServiceDefaults backend/Yaam.Infrastructure backend/Yaam.slnx
git commit -m "refactor(backend): extract JWT authentication into ServiceDefaults"
```

### Task 3: Reminders service skeleton, database and test harness

**Files:**
- Create: `backend/Yaam.Reminders/Yaam.Reminders.csproj`
- Create: `backend/Yaam.Reminders/Program.cs`
- Create: `backend/Yaam.Reminders/appsettings.json`
- Create: `backend/Yaam.Reminders/appsettings.Development.json`
- Create: `backend/Yaam.Reminders/Properties/launchSettings.json`
- Create: `backend/Yaam.Reminders/Domain/Reminder.cs`
- Create: `backend/Yaam.Reminders/Domain/ApplicationSnapshot.cs`
- Create: `backend/Yaam.Reminders/Domain/ProcessedMessage.cs`
- Create: `backend/Yaam.Reminders/Persistence/RemindersDbContext.cs`
- Create: `backend/Yaam.Reminders/Persistence/Migrations/*` (generated)
- Create: `backend/Yaam.Reminders/Common/NotFoundException.cs`
- Create: `backend/Yaam.Reminders/Common/ValidationBehavior.cs`
- Create: `backend/Yaam.Reminders/Common/GlobalExceptionHandler.cs`
- Create: `backend/Yaam.Reminders.Tests/Yaam.Reminders.Tests.csproj`
- Create: `backend/Yaam.Reminders.Tests/appsettings.Test.json`
- Create: `backend/Yaam.Reminders.Tests/RemindersApiFactory.cs`
- Create: `backend/Yaam.Reminders.Tests/RemindersCollection.cs`
- Create: `backend/Yaam.Reminders.Tests/Integration/RemindersEndpointsTests.cs`
- Modify: `backend/Yaam.slnx`
- Modify: `.github/workflows/backend-ci.yml`

**Interfaces:**
- Produces:
  - entities `Reminder`, `ApplicationSnapshot`, `ProcessedMessage` (namespace `Yaam.Reminders.Domain`)
  - `RemindersDbContext` with `Reminders`, `ApplicationSnapshots`, `ProcessedMessages`
  - `RemindersApiFactory` with `CreateAuthenticatedClient()`, `SeedSnapshotAsync(ApplicationSnapshot snapshot)`, `EmailSender` (a `CapturingEmailSender`, added in Task 5)
  - xUnit collection `"Reminders"`

- [ ] **Step 1: Project file**

`backend/Yaam.Reminders/Yaam.Reminders.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="12.1.1" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
    <PackageReference Include="MailKit" Version="4.17.0" />
    <PackageReference Include="MediatR" Version="14.2.0" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.9" />
    <PackageReference Include="Microsoft.OpenApi" Version="2.10.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.9">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Yaam.ServiceDefaults\Yaam.ServiceDefaults.csproj" />
  </ItemGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="Yaam.Reminders.Tests" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Domain**

`Domain/Reminder.cs`:
```csharp
namespace Yaam.Reminders.Domain;

public class Reminder
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public int DelayDays { get; set; }
    public DateOnly DueDate { get; set; }
    public string? Note { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

`Domain/ApplicationSnapshot.cs`:
```csharp
namespace Yaam.Reminders.Domain;

// Local, eventually consistent copy of the monolith's Application, written only by the event consumer.
public class ApplicationSnapshot
{
    public Guid ApplicationId { get; init; }
    public long Version { get; set; }
    public required string CompanyName { get; set; }
    public required string Role { get; set; }
    public DateOnly? DateApplied { get; set; }
    public string? ContactName { get; set; }
    public bool IsDeleted { get; set; }
}
```

`Domain/ProcessedMessage.cs`:
```csharp
namespace Yaam.Reminders.Domain;

public class ProcessedMessage
{
    public Guid MessageId { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

- [ ] **Step 3: DbContext**

`Persistence/RemindersDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Domain;

namespace Yaam.Reminders.Persistence;

public class RemindersDbContext(DbContextOptions<RemindersDbContext> options) : DbContext(options)
{
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ApplicationSnapshot> ApplicationSnapshots => Set<ApplicationSnapshot>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reminder>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Note).HasMaxLength(500);
            builder.HasIndex(r => r.ApplicationId);
        });

        modelBuilder.Entity<ApplicationSnapshot>(builder =>
        {
            builder.HasKey(s => s.ApplicationId);
            builder.Property(s => s.ApplicationId).ValueGeneratedNever();
            builder.Property(s => s.CompanyName).IsRequired().HasMaxLength(200);
            builder.Property(s => s.Role).IsRequired().HasMaxLength(350);
            builder.Property(s => s.ContactName).HasMaxLength(200);
        });

        modelBuilder.Entity<ProcessedMessage>(builder =>
        {
            builder.HasKey(m => m.MessageId);
            builder.Property(m => m.MessageId).ValueGeneratedNever();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Reminder>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Common plumbing (copied on purpose, see ADR 010)**

`Common/NotFoundException.cs`:
```csharp
namespace Yaam.Reminders.Common;

public class NotFoundException(string entityName, Guid id)
    : Exception($"{entityName} with id '{id}' was not found.");
```

`Common/ValidationBehavior.cs`:
- Copy `backend/Yaam.UseCases/Common/Behaviors/ValidationBehavior.cs` verbatim.
- Change only the namespace to `Yaam.Reminders.Common`.

`Common/GlobalExceptionHandler.cs`:
- Copy `backend/Yaam.API/GlobalExceptionHandler.cs` verbatim.
- Change the namespace to `Yaam.Reminders.Common`.
- Replace `using Yaam.Domain.Errors;` with nothing (`NotFoundException` is now in the same namespace).
- Delete the `ConflictException` arm of the switch (Reminders has no conflicts).

- [ ] **Step 5: Program, configuration, launch profile**

`Program.cs`:
```csharp
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Common;
using Yaam.Reminders.Persistence;
using Yaam.ServiceDefaults.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RemindersDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.")));

builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddYaamJwtAuthentication(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
```

`appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

`appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=yaam_reminders;Username=yaam;Password=yaam"
  },
  "Supabase": {
    "Url": "https://bayxjmzmvsxyynquxmfi.supabase.co"
  }
}
```

`Properties/launchSettings.json`: same shape as the gateway's, with `"applicationUrl": "http://localhost:5240"`.

Add `<Project Path="Yaam.Reminders/Yaam.Reminders.csproj" />` to `Yaam.slnx`.

- [ ] **Step 6: Initial migration and database**

Run (from `backend/`):
```bash
dotnet ef migrations add InitialCreate --project Yaam.Reminders --output-dir Persistence/Migrations -- --environment Development
dotnet ef database update --project Yaam.Reminders -- --environment Development
```
Expected: the migration files are created, and `database update` creates the `yaam_reminders` database. Npgsql creates a missing database, so no init script is needed.

- [ ] **Step 7: Test project and factory**

`backend/Yaam.Reminders.Tests/Yaam.Reminders.Tests.csproj`:
- Copy `Yaam.Tests.Integration.csproj` and add `<PackageReference Include="NSubstitute" Version="6.0.0" />`.
- Replace the two `ProjectReference`s with `<ProjectReference Include="..\Yaam.Reminders\Yaam.Reminders.csproj" />`.

`appsettings.Test.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=yaam_reminders_test;Username=yaam;Password=yaam"
  },
  "Supabase": {
    "Url": "https://test.supabase.co"
  }
}
```
(Port 5433 matches the monolith's test config. CI overrides it via an environment variable.)

`RemindersCollection.cs`:
```csharp
namespace Yaam.Reminders.Tests;

[CollectionDefinition("Reminders")]
public class RemindersCollection : ICollectionFixture<RemindersApiFactory> { }
```

`RemindersApiFactory.cs`:
- Structure: the same as `backend/Yaam.Tests.Integration/ApiFactory.cs`, with these differences:
  - class name `RemindersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime`
  - it swaps `DbContextOptions<RemindersDbContext>`
  - no CV parser
  - `TestTokenHelper` is copied into this file verbatim
- Additionally add:

```csharp
    public async Task SeedSnapshotAsync(ApplicationSnapshot snapshot)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        db.ApplicationSnapshots.Add(snapshot);
        await db.SaveChangesAsync();
    }

    public static ApplicationSnapshot NewSnapshot(string companyName) => new()
    {
        ApplicationId = Guid.NewGuid(),
        Version = 1,
        CompanyName = companyName,
        Role = "Developer",
        DateApplied = new DateOnly(2026, 9, 1),
        ContactName = "Grace",
    };
```
`InitializeAsync` runs `EnsureDeletedAsync()` then `MigrateAsync()` on `RemindersDbContext`.

- [ ] **Step 8: Write the first failing test**

`Integration/RemindersEndpointsTests.cs`:
```csharp
using System.Net;
using FluentAssertions;

namespace Yaam.Reminders.Tests.Integration;

[Collection("Reminders")]
public class RemindersEndpointsTests(RemindersApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task GET_Reminders_Returns401_WithoutToken()
    {
        var response = await factory.CreateClient().GetAsync("/api/reminders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

Run: `dotnet test Yaam.Reminders.Tests`
Expected: FAIL with 404 instead of 401, because there is no controller yet. Task 4 adds it. If it fails for any other reason (DB connection, DI), fix that now; that is the point of this step.

- [ ] **Step 9: CI.** In `.github/workflows/backend-ci.yml`, add a step after "Integration tests":

```yaml
      - name: Reminders service tests
        run: dotnet test Yaam.Reminders.Tests --no-build --configuration Release
        working-directory: backend
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Port=5432;Database=yaam_reminders_test;Username=yaam;Password=yaam"
```

- [ ] **Step 10: Commit** (the failing test is committed together with Task 4; stage only)

```bash
git add backend/Yaam.Reminders backend/Yaam.Reminders.Tests backend/Yaam.slnx .github/workflows/backend-ci.yml
```

### Task 4: Reminders HTTP API (ported from the monolith)

**Files:**
- Create: `backend/Yaam.Reminders/Reminders/Dtos/ReminderDto.cs`
- Create: `backend/Yaam.Reminders/Reminders/ReminderMapper.cs`
- Create: `backend/Yaam.Reminders/Reminders/Commands/CreateReminderCommand.cs`
- Create: `backend/Yaam.Reminders/Reminders/Commands/CompleteReminderCommand.cs`
- Create: `backend/Yaam.Reminders/Reminders/Commands/RescheduleReminderCommand.cs`
- Create: `backend/Yaam.Reminders/Reminders/Commands/DeleteReminderCommand.cs`
- Create: `backend/Yaam.Reminders/Reminders/Queries/GetRemindersQuery.cs`
- Create: `backend/Yaam.Reminders/Reminders/Api/RemindersController.cs`
- Create: `backend/Yaam.Reminders/Reminders/Api/ReminderViewModel.cs`
- Create: `backend/Yaam.Reminders/Reminders/Api/ReminderViewModelMapper.cs`
- Modify: `backend/Yaam.Reminders.Tests/Integration/RemindersEndpointsTests.cs`
- Create: `backend/Yaam.Reminders.Tests/Unit/CreateReminderCommandValidatorTests.cs`
- Create: `backend/Yaam.Reminders.Tests/Unit/RescheduleReminderCommandValidatorTests.cs`

**Interfaces:**
- Produces (HTTP):

  | Method | Route | Returns |
  |---|---|---|
  | `GET` | `/api/reminders?applicationId=` | `List<ReminderViewModel>` |
  | `POST` | `/api/reminders` | 201 `ReminderViewModel` |
  | `PATCH` | `/api/reminders/{id}/complete` | 204 |
  | `PATCH` | `/api/reminders/{id}/reschedule` | 200 `ReminderViewModel` |
  | `DELETE` | `/api/reminders/{id}` | 204 |

  Endpoint names: `ListReminders`, `CreateReminder`, `CompleteReminder`, `RescheduleReminder`, `DeleteReminder`.
- Produces (C#): `ReminderDto(Guid Id, Guid ApplicationId, string CompanyName, string Role, DateOnly? DateApplied, string? ContactName, int DelayDays, DateOnly DueDate, string? Note, DateTime? NotifiedAt, DateTime? CompletedAt, DateTime CreatedAt)` and `ReminderMapper.ToDto(Reminder reminder, ApplicationSnapshot snapshot)`

- [ ] **Step 1: Write the failing endpoint tests.** Replace the test class body (keep the 401 test):

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Reminders.Api;

namespace Yaam.Reminders.Tests.Integration;

[Collection("Reminders")]
public class RemindersEndpointsTests(RemindersApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task GET_Reminders_Returns401_WithoutToken()
    {
        var response = await factory.CreateClient().GetAsync("/api/reminders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Reminder_Returns404_WhenApplicationSnapshotIsUnknown()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = Guid.NewGuid(), delayDays = 7 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Reminder_Returns404_WhenApplicationIsDeleted()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Deleted Co");
        snapshot.IsDeleted = true;
        await factory.SeedSnapshotAsync(snapshot);

        var response = await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = snapshot.ApplicationId, delayDays = 7 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReminderCrudFlow_CreateRescheduleCompleteDelete()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Flow Co");
        await factory.SeedSnapshotAsync(snapshot);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/reminders",
            new { applicationId = snapshot.ApplicationId, delayDays = 7, note = "Check in on status" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var first = (await createResponse.Content.ReadFromJsonAsync<ReminderViewModel>())!;
        first.CompanyName.Should().Be("Flow Co");
        first.DueDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7));
        first.Note.Should().Be("Check in on status");

        var second = (await (await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = snapshot.ApplicationId, delayDays = 14 }))
            .Content.ReadFromJsonAsync<ReminderViewModel>())!;

        (await ListForAsync(snapshot.ApplicationId)).Should().HaveCount(2);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var rescheduleResponse = await _client.PatchAsJsonAsync(
            $"/api/reminders/{first.Id}/reschedule", new { newDueDate = tomorrow.ToString("yyyy-MM-dd") });
        rescheduleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await rescheduleResponse.Content.ReadFromJsonAsync<ReminderViewModel>())!
            .DueDate.Should().Be(tomorrow);

        var completeResponse = await _client.PatchAsJsonAsync($"/api/reminders/{first.Id}/complete", new { });
        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListForAsync(snapshot.ApplicationId)).Should().ContainSingle(r => r.Id == second.Id);

        var deleteResponse = await _client.DeleteAsync($"/api/reminders/{second.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListForAsync(snapshot.ApplicationId)).Should().BeEmpty();
    }

    [Fact]
    public async Task GET_Reminders_WithoutFilter_ReturnsActiveRemindersAcrossApplications()
    {
        var snapshotA = RemindersApiFactory.NewSnapshot("A Co");
        var snapshotB = RemindersApiFactory.NewSnapshot("B Co");
        await factory.SeedSnapshotAsync(snapshotA);
        await factory.SeedSnapshotAsync(snapshotB);
        await _client.PostAsJsonAsync("/api/reminders", new { applicationId = snapshotA.ApplicationId, delayDays = 3 });
        await _client.PostAsJsonAsync("/api/reminders", new { applicationId = snapshotB.ApplicationId, delayDays = 1 });

        var all = await _client.GetFromJsonAsync<List<ReminderViewModel>>("/api/reminders");

        all!.Should().Contain(r => r.ApplicationId == snapshotA.ApplicationId)
            .And.Contain(r => r.ApplicationId == snapshotB.ApplicationId);
        all.Select(r => r.DueDate).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task POST_Reminder_Returns400_WhenDelayDaysIsZero()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Validation Co");
        await factory.SeedSnapshotAsync(snapshot);

        var response = await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = snapshot.ApplicationId, delayDays = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_Reschedule_Returns400_WhenNewDueDateIsToday()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Reschedule Co");
        await factory.SeedSnapshotAsync(snapshot);
        var reminder = (await (await _client.PostAsJsonAsync(
            "/api/reminders", new { applicationId = snapshot.ApplicationId, delayDays = 7 }))
            .Content.ReadFromJsonAsync<ReminderViewModel>())!;

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PatchAsJsonAsync(
            $"/api/reminders/{reminder.Id}/reschedule", new { newDueDate = today });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<List<ReminderViewModel>> ListForAsync(Guid applicationId)
        => (await _client.GetFromJsonAsync<List<ReminderViewModel>>($"/api/reminders?applicationId={applicationId}"))!;
}
```

Validator unit tests:
- Port `backend/Yaam.Tests.Unit/Reminders/SetReminderCommandValidatorTests.cs` → `Unit/CreateReminderCommandValidatorTests.cs`.
  - Rename the types to `CreateReminderCommand` / `CreateReminderCommandValidator`.
  - Construct the command as `new CreateReminderCommand(Guid.NewGuid(), delayDays, note)`.
- Port `RescheduleReminderCommandValidatorTests.cs` unchanged, apart from the namespace `Yaam.Reminders.Tests.Unit` and the usings `Yaam.Reminders.Reminders.Commands`.

Run: `dotnet test Yaam.Reminders.Tests`
Expected: compile failure (`ReminderViewModel` missing). That is the red state.

- [ ] **Step 2: DTO and mapper**

`Reminders/Dtos/ReminderDto.cs`:
```csharp
namespace Yaam.Reminders.Reminders.Dtos;

public record ReminderDto(
    Guid Id,
    Guid ApplicationId,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    string? ContactName,
    int DelayDays,
    DateOnly DueDate,
    string? Note,
    DateTime? NotifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
```

`Reminders/ReminderMapper.cs`:
```csharp
using Yaam.Reminders.Domain;
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders;

internal static class ReminderMapper
{
    internal static ReminderDto ToDto(Reminder reminder, ApplicationSnapshot snapshot) => new(
        reminder.Id, reminder.ApplicationId,
        snapshot.CompanyName, snapshot.Role, snapshot.DateApplied, snapshot.ContactName,
        reminder.DelayDays, reminder.DueDate, reminder.Note,
        reminder.NotifiedAt, reminder.CompletedAt, reminder.CreatedAt);
}
```

- [ ] **Step 3: Commands and query**

`Reminders/Commands/CreateReminderCommand.cs`:
```csharp
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Common;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders.Commands;

public record CreateReminderCommand(
    Guid ApplicationId,
    int DelayDays,
    string? Note) : IRequest<ReminderDto>;

public class CreateReminderCommandValidator : AbstractValidator<CreateReminderCommand>
{
    public CreateReminderCommandValidator()
    {
        RuleFor(x => x.DelayDays).GreaterThan(0).WithMessage("Delay must be at least 1 day.");
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}

public class CreateReminderCommandHandler(RemindersDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CreateReminderCommand, ReminderDto>
{
    public async Task<ReminderDto> Handle(CreateReminderCommand command, CancellationToken cancellationToken)
    {
        // Existence is checked against the local snapshot, not the monolith: no runtime dependency, but eventually consistent.
        var snapshot = await db.ApplicationSnapshots
            .FirstOrDefaultAsync(s => s.ApplicationId == command.ApplicationId && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Application", command.ApplicationId);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var reminder = new Reminder
        {
            ApplicationId = command.ApplicationId,
            DelayDays = command.DelayDays,
            DueDate = today.AddDays(command.DelayDays),
            Note = command.Note,
        };

        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(cancellationToken);
        return ReminderMapper.ToDto(reminder, snapshot);
    }
}
```

`Reminders/Commands/CompleteReminderCommand.cs`:
```csharp
using MediatR;
using Yaam.Reminders.Common;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Reminders.Commands;

public record CompleteReminderCommand(Guid ReminderId) : IRequest;

public class CompleteReminderCommandHandler(RemindersDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CompleteReminderCommand>
{
    public async Task Handle(CompleteReminderCommand command, CancellationToken cancellationToken)
    {
        var reminder = await db.Reminders.FindAsync([command.ReminderId], cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), command.ReminderId);

        reminder.CompletedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

`Reminders/Commands/RescheduleReminderCommand.cs`:
```csharp
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Common;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders.Commands;

public record RescheduleReminderCommand(
    Guid ReminderId,
    DateOnly NewDueDate) : IRequest<ReminderDto>;

public class RescheduleReminderCommandValidator : AbstractValidator<RescheduleReminderCommand>
{
    public RescheduleReminderCommandValidator()
    {
        RuleFor(x => x.NewDueDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("New due date must be in the future.");
    }
}

public class RescheduleReminderCommandHandler(RemindersDbContext db)
    : IRequestHandler<RescheduleReminderCommand, ReminderDto>
{
    public async Task<ReminderDto> Handle(RescheduleReminderCommand command, CancellationToken cancellationToken)
    {
        var reminder = await db.Reminders.FindAsync([command.ReminderId], cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), command.ReminderId);
        var snapshot = await db.ApplicationSnapshots
            .FirstAsync(s => s.ApplicationId == reminder.ApplicationId, cancellationToken);

        reminder.DueDate = command.NewDueDate;
        reminder.NotifiedAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return ReminderMapper.ToDto(reminder, snapshot);
    }
}
```

`Reminders/Commands/DeleteReminderCommand.cs`:
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Reminders.Commands;

public record DeleteReminderCommand(Guid ReminderId) : IRequest;

public class DeleteReminderCommandHandler(RemindersDbContext db) : IRequestHandler<DeleteReminderCommand>
{
    public async Task Handle(DeleteReminderCommand command, CancellationToken cancellationToken)
        => await db.Reminders
            .Where(r => r.Id == command.ReminderId)
            .ExecuteDeleteAsync(cancellationToken);
}
```

`Reminders/Queries/GetRemindersQuery.cs`:
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Yaam.Reminders.Persistence;
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders.Queries;

public record GetRemindersQuery(Guid? ApplicationId) : IRequest<List<ReminderDto>>;

public class GetRemindersQueryHandler(RemindersDbContext db)
    : IRequestHandler<GetRemindersQuery, List<ReminderDto>>
{
    public async Task<List<ReminderDto>> Handle(GetRemindersQuery query, CancellationToken cancellationToken)
    {
        var active =
            from reminder in db.Reminders
            join snapshot in db.ApplicationSnapshots on reminder.ApplicationId equals snapshot.ApplicationId
            where reminder.CompletedAt == null && !snapshot.IsDeleted
            select new { reminder, snapshot };

        if (query.ApplicationId is { } applicationId)
            active = active.Where(row => row.reminder.ApplicationId == applicationId);

        var rows = await active
            .OrderBy(row => row.reminder.DueDate)
            .ToListAsync(cancellationToken);
        return rows.Select(row => ReminderMapper.ToDto(row.reminder, row.snapshot)).ToList();
    }
}
```

- [ ] **Step 4: Controller, view model, mapper**

`Reminders/Api/ReminderViewModel.cs`:
```csharp
namespace Yaam.Reminders.Reminders.Api;

public record ReminderViewModel(
    Guid Id,
    Guid ApplicationId,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    string? ContactName,
    int DelayDays,
    DateOnly DueDate,
    string? Note,
    DateTime? NotifiedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
```

`Reminders/Api/ReminderViewModelMapper.cs`:
```csharp
using Yaam.Reminders.Reminders.Dtos;

namespace Yaam.Reminders.Reminders.Api;

internal static class ReminderViewModelMapper
{
    internal static ReminderViewModel ToViewModel(ReminderDto dto) => new(
        dto.Id, dto.ApplicationId, dto.CompanyName, dto.Role, dto.DateApplied, dto.ContactName,
        dto.DelayDays, dto.DueDate, dto.Note, dto.NotifiedAt, dto.CompletedAt, dto.CreatedAt);
}
```

`Reminders/Api/RemindersController.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Yaam.Reminders.Reminders.Commands;
using Yaam.Reminders.Reminders.Queries;

namespace Yaam.Reminders.Reminders.Api;

[Authorize]
[ApiController]
[Route("api/reminders")]
public class RemindersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListReminders")]
    public async Task<ActionResult<List<ReminderViewModel>>> List(
        [FromQuery] Guid? applicationId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetRemindersQuery(applicationId),
            cancellationToken);
        return Ok(result.Select(ReminderViewModelMapper.ToViewModel).ToList());
    }

    [HttpPost]
    [EndpointName("CreateReminder")]
    public async Task<ActionResult<ReminderViewModel>> Create(
        [FromBody] CreateReminderInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateReminderCommand(input.ApplicationId, input.DelayDays, input.Note),
            cancellationToken);
        return CreatedAtAction(
            nameof(List), new { applicationId = result.ApplicationId }, ReminderViewModelMapper.ToViewModel(result));
    }

    [HttpPatch("{reminderId:guid}/complete")]
    [EndpointName("CompleteReminder")]
    public async Task<IActionResult> Complete(Guid reminderId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new CompleteReminderCommand(reminderId),
            cancellationToken);
        return NoContent();
    }

    [HttpPatch("{reminderId:guid}/reschedule")]
    [EndpointName("RescheduleReminder")]
    public async Task<ActionResult<ReminderViewModel>> Reschedule(
        Guid reminderId, [FromBody] RescheduleReminderInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RescheduleReminderCommand(reminderId, input.NewDueDate),
            cancellationToken);
        return Ok(ReminderViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("{reminderId:guid}")]
    [EndpointName("DeleteReminder")]
    public async Task<IActionResult> Delete(Guid reminderId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteReminderCommand(reminderId),
            cancellationToken);
        return NoContent();
    }
}

public record CreateReminderInputModel(Guid ApplicationId, int DelayDays, string? Note);
public record RescheduleReminderInputModel(DateOnly NewDueDate);
```

- [ ] **Step 5: Run the tests**

Run: `dotnet test Yaam.Reminders.Tests`
Expected: all PASS.

- [ ] **Step 6: Commit** (includes the staged Task 3 files)

```bash
git add backend/Yaam.Reminders backend/Yaam.Reminders.Tests
git commit -m "feat(reminders): add reminders service with own database and HTTP API"
```

### Task 5: Move the daily notification job and email sending into Reminders

**Files:**
- Create: `backend/Yaam.Reminders/Notifications/EmailSettings.cs`
- Create: `backend/Yaam.Reminders/Notifications/IEmailSender.cs`
- Create: `backend/Yaam.Reminders/Notifications/SmtpEmailSender.cs`
- Create: `backend/Yaam.Reminders/Notifications/SendDueRemindersCommand.cs`
- Create: `backend/Yaam.Reminders/Notifications/ReminderNotificationService.cs`
- Modify: `backend/Yaam.Reminders/Program.cs`
- Modify: `backend/Yaam.Reminders/appsettings.json`
- Modify: `backend/Yaam.Reminders/appsettings.Development.json`
- Modify: `backend/Yaam.Reminders.Tests/RemindersApiFactory.cs`
- Create: `backend/Yaam.Reminders.Tests/Integration/SendDueRemindersTests.cs`
- Create: `backend/Yaam.Reminders.Tests/Unit/ReminderNotificationServiceTests.cs`

**Interfaces:**
- Produces:
  - `SendDueRemindersCommand : IRequest`
  - `ReminderNotificationService.TimeUntilNextRun(DateTime utcNow) : TimeSpan` (internal static)
  - `CapturingEmailSender` in the test factory, with `IReadOnlyList<SentEmail> Sent` and `Clear()`

- [ ] **Step 1: Write the failing tests.**

In `RemindersApiFactory.cs`, add the capturing sender and register it, plus remove the background job so it can't fire during tests:

```csharp
public record SentEmail(string To, string Subject, string Body);

public class CapturingEmailSender : IEmailSender
{
    private readonly List<SentEmail> _sent = [];
    public IReadOnlyList<SentEmail> Sent { get { lock (_sent) return _sent.ToList(); } }
    public void Clear() { lock (_sent) _sent.Clear(); }

    public Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken)
    {
        lock (_sent) _sent.Add(new SentEmail(to, subject, textBody));
        return Task.CompletedTask;
    }
}
```
In the factory, add the property `public CapturingEmailSender EmailSender { get; } = new();`. Inside `ConfigureServices`:
```csharp
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);

            var notificationJob = services.Single(d => d.ImplementationType == typeof(ReminderNotificationService));
            services.Remove(notificationJob);
```

`Integration/SendDueRemindersTests.cs`:
```csharp
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Notifications;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Tests.Integration;

[Collection("Reminders")]
public class SendDueRemindersTests(RemindersApiFactory factory)
{
    [Fact]
    public async Task SendsOneEmailPerDueReminder_AndNotAgainOnNextRun()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Due Co");
        await factory.SeedSnapshotAsync(snapshot);
        var reminderId = await SeedDueReminderAsync(snapshot.ApplicationId);
        factory.EmailSender.Clear();

        await RunJobAsync();
        await RunJobAsync();

        factory.EmailSender.Sent.Where(e => e.Subject.Contains("Due Co")).Should().ContainSingle()
            .Which.Body.Should().Contain("Dear Grace,");
        (await GetReminderAsync(reminderId)).NotifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SkipsRemindersOfDeletedApplications()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Gone Co");
        snapshot.IsDeleted = true;
        await factory.SeedSnapshotAsync(snapshot);
        await SeedDueReminderAsync(snapshot.ApplicationId);
        factory.EmailSender.Clear();

        await RunJobAsync();

        factory.EmailSender.Sent.Should().NotContain(e => e.Subject.Contains("Gone Co"));
    }

    private async Task RunJobAsync()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(
            new SendDueRemindersCommand(),
            CancellationToken.None);
    }

    private async Task<Guid> SeedDueReminderAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        var reminder = new Reminder
        {
            ApplicationId = applicationId,
            DelayDays = 7,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync();
        return reminder.Id;
    }

    private async Task<Reminder> GetReminderAsync(Guid reminderId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        return await db.Reminders.AsNoTracking().SingleAsync(r => r.Id == reminderId);
    }
}
```

`Unit/ReminderNotificationServiceTests.cs`: copy `backend/Yaam.Tests.Unit/Reminders/ReminderNotificationServiceTests.cs` verbatim, changing only the namespace (`Yaam.Reminders.Tests.Unit`) and the using (`Yaam.Reminders.Notifications`).

Run: `dotnet test Yaam.Reminders.Tests`
Expected: compile failure (the `Notifications` types are missing).

- [ ] **Step 2: Email plumbing (moved from the monolith).**
  - `Notifications/EmailSettings.cs`, `Notifications/IEmailSender.cs` and `Notifications/SmtpEmailSender.cs`: copy verbatim from `backend/Yaam.UseCases/Common/Email/EmailSettings.cs`, `.../IEmailSender.cs` and `backend/Yaam.Infrastructure/Email/SmtpEmailSender.cs`.
  - Change the namespaces to `Yaam.Reminders.Notifications` and remove the cross-namespace usings.

- [ ] **Step 3: `Notifications/SendDueRemindersCommand.cs`**

```csharp
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Notifications;

public record SendDueRemindersCommand : IRequest;

public class SendDueRemindersCommandHandler(
    RemindersDbContext db,
    IEmailSender emailSender,
    IOptions<EmailSettings> emailOptions,
    TimeProvider timeProvider,
    ILogger<SendDueRemindersCommandHandler> logger)
    : IRequestHandler<SendDueRemindersCommand>
{
    public async Task Handle(SendDueRemindersCommand command, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var due = await (
                from reminder in db.Reminders
                join snapshot in db.ApplicationSnapshots on reminder.ApplicationId equals snapshot.ApplicationId
                where reminder.CompletedAt == null && reminder.NotifiedAt == null
                      && reminder.DueDate <= today && !snapshot.IsDeleted
                select new { reminder, snapshot })
            .ToListAsync(cancellationToken);

        foreach (var row in due)
        {
            try
            {
                await emailSender.SendAsync(
                    emailOptions.Value.NotificationEmail,
                    $"Follow-up reminder: {row.snapshot.CompanyName} — {row.snapshot.Role}",
                    BuildEmailBody(row.reminder, row.snapshot),
                    cancellationToken);

                row.reminder.NotifiedAt = timeProvider.GetUtcNow().UtcDateTime;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Failed to send reminder {ReminderId}", row.reminder.Id);
            }
        }
    }

    private static string BuildEmailBody(Reminder reminder, ApplicationSnapshot application)
    {
        // body unchanged from the monolith's SendDueRemindersCommandHandler.BuildEmailBody,
        // reading company/role/date/contact from `application` (the snapshot) instead of reminder.Application
    }
}
```
For the body of `BuildEmailBody`, copy lines verbatim from `backend/Yaam.UseCases/Reminders/Commands/SendDueRemindersCommand.cs` (the `BuildEmailBody` method), deleting its first line `var application = reminder.Application;`. The parameter is already called `application`, so the rest compiles unchanged. Replace the two-line comment above with that code.

- [ ] **Step 4: `Notifications/ReminderNotificationService.cs`.** Copy `backend/Yaam.API/Reminders/ReminderNotificationService.cs` verbatim. Change the namespace to `Yaam.Reminders.Notifications` and the using to the local `SendDueRemindersCommand`.

- [ ] **Step 5: Wire up.** In `Program.cs`, before `var app = builder.Build();`:
```csharp
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<ReminderNotificationService>();
```
(add `using Yaam.Reminders.Notifications;`).

Move the `"Email"` sections:
- from `backend/Yaam.API/appsettings.json` into `backend/Yaam.Reminders/appsettings.json`;
- from `backend/Yaam.API/appsettings.Development.json` into `backend/Yaam.Reminders/appsettings.Development.json`.
- Copy them verbatim, then delete them from the API in Task 6.

- [ ] **Step 6: Run the tests**

Run: `dotnet test Yaam.Reminders.Tests`
Expected: all PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/Yaam.Reminders backend/Yaam.Reminders.Tests
git commit -m "feat(reminders): move due-reminder email job into reminders service"
```

### Task 6: Remove Reminders from the monolith; route `/api/reminders` to the new service

Disabling the monolith's job and enabling the new one land in the same branch state: never two schedulers, never zero.

**Files:**
- Delete: `backend/Yaam.Domain/Entities/Reminder.cs`
- Delete: `backend/Yaam.Domain/Repositories/IReminderRepository.cs`
- Delete: `backend/Yaam.UseCases/Reminders/` (whole folder)
- Delete: `backend/Yaam.UseCases/Common/Email/` (whole folder)
- Delete: `backend/Yaam.Infrastructure/Repositories/ReminderRepository.cs`
- Delete: `backend/Yaam.Infrastructure/Persistence/Configurations/ReminderConfiguration.cs`
- Delete: `backend/Yaam.Infrastructure/Email/` (whole folder)
- Delete: `backend/Yaam.API/Applications/ApplicationReminderController.cs`
- Delete: `backend/Yaam.API/Reminders/` (whole folder)
- Delete: `backend/Yaam.Tests.Integration/Reminders/`
- Delete: `backend/Yaam.Tests.Unit/Reminders/`
- Modify: `backend/Yaam.Domain/Entities/Application.cs` (remove `Reminders`)
- Modify: `backend/Yaam.UseCases/Applications/ApplicationMapper.cs`
- Modify: `backend/Yaam.UseCases/Applications/Dtos/ApplicationDto.cs`
- Modify: `backend/Yaam.API/Applications/ApplicationViewModel.cs`
- Modify: `backend/Yaam.API/Applications/ApplicationViewModelMapper.cs`
- Modify: `backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs` (remove `.Include(a => a.Reminders)`)
- Modify: `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs` (remove `Reminders` DbSet)
- Modify: `backend/Yaam.Infrastructure/DependencyInjection.cs` (remove the reminder repository and email registrations)
- Modify: `backend/Yaam.Infrastructure/Yaam.Infrastructure.csproj` (remove MailKit)
- Modify: `backend/Yaam.API/Program.cs` (remove `AddHostedService<ReminderNotificationService>()` and `using Yaam.API.Reminders;`)
- Modify: `backend/Yaam.API/appsettings.json`, `appsettings.Development.json` (remove the `Email` sections)
- Modify: `backend/Yaam.Tests.Integration/ApiFactory.cs` (remove the `IEmailSender` swap and the `NoOpEmailSender` class)
- Create: `backend/Yaam.Infrastructure/Migrations/*_RemoveReminders.cs` (generated)
- Modify: `backend/Yaam.Gateway/appsettings.json`

- [ ] **Step 1: Delete and edit as listed.**
  - `ApplicationDto` and `ApplicationViewModel` lose their last parameter (`Reminders`).
  - `ApplicationMapper.ToDto` and `ApplicationViewModelMapper.ToViewModel` lose the reminders argument and the `Yaam.UseCases.Reminders` / `Yaam.API.Reminders` usings.
  - After editing, run `grep -rn "Reminder\|IEmailSender\|EmailSettings" backend/Yaam.Domain backend/Yaam.UseCases backend/Yaam.Infrastructure backend/Yaam.API backend/Yaam.Tests.* --include=*.cs | grep -v Migrations`.
  - Expected: no output.

- [ ] **Step 2: Migration**

Run (from `backend/`):
```bash
dotnet ef migrations add RemoveReminders --project Yaam.Infrastructure --startup-project Yaam.API -- --environment Development
```
Expected: the generated `Up` contains `migrationBuilder.DropTable(name: "Reminders");`. Then `dotnet ef database update --project Yaam.Infrastructure --startup-project Yaam.API -- --environment Development`.

- [ ] **Step 3: Gateway route.** In `backend/Yaam.Gateway/appsettings.json`:
  - Add under `Routes`:
```json
      "reminders": {
        "ClusterId": "reminders",
        "Match": { "Path": "/api/reminders/{**catch-all}" }
      },
```
  - Add under `Clusters`:
```json
      "reminders": {
        "Destinations": {
          "reminders-1": { "Address": "http://localhost:5240" }
        }
      },
```
  - Both routes match `/api/reminders/...`. ASP.NET routing precedence prefers the route with more literal segments (`/api/reminders/...` beats `/api/...`), so no explicit `Order` is needed. Step 5 verifies this.

- [ ] **Step 4: Full test run**

Run: `cd backend && dotnet build && dotnet test Yaam.Tests.Unit && dotnet test Yaam.Tests.Integration && dotnet test Yaam.Reminders.Tests && dotnet format --verify-no-changes`
Expected: all pass, no format changes.

- [ ] **Step 5: Verify gateway routing.** Run the monolith, the Reminders service and the gateway.
  - Run `curl -i http://localhost:5100/api/reminders`. Expected: 401.
  - Repeat with the Reminders service stopped. Expected: 502/503 from YARP. This proves the route goes to Reminders and not the monolith, whose 404 would be different.

- [ ] **Step 6: Commit**

```bash
git add -A backend
git commit -m "refactor(backend): remove reminders from monolith and route to reminders service"
```

### Task 7: Frontend on the Reminders client

**Files:**
- Modify: `frontend/openapitools.json`
- Regenerate: `frontend/src/app/generated/api/**`, Create: `frontend/src/app/generated/reminders-api/**`
- Modify: `frontend/src/app/app.config.ts`
- Modify: `frontend/src/app/features/applications/components/reminder-section/reminder-section.component.ts`
- Modify: `frontend/src/app/features/applications/pages/application-detail/application-detail.component.html`
- Modify: `frontend/src/app/features/reminders/pages/reminders-list/reminders-list.component.ts`
- Modify: `CLAUDE.md`

- [ ] **Step 1: Second generator.** In `frontend/openapitools.json`, add next to `"yaam-api"`:
```json
      "yaam-reminders-api": {
        "generatorName": "typescript-angular",
        "output": "src/app/generated/reminders-api",
        "inputSpec": "http://localhost:5240/openapi/v1.json",
        "additionalProperties": {
          "ngVersion": "22",
          "providedIn": "root",
          "withInterfaces": true,
          "enumPropertyNaming": "original"
        }
      }
```

- [ ] **Step 2: Regenerate both clients.** With the monolith (5231) and Reminders (5240) running:
```bash
cd frontend && rm -rf src/app/generated/api src/app/generated/reminders-api && npm run generate:api
```
- Deleting first is required: the generator never removes stale files, so `applicationReminder.service.ts` would otherwise survive.
- Expected: `src/app/generated/reminders-api/api/reminders.service.ts` exists; `src/app/generated/api/api/` no longer contains `applicationReminder.*` or `reminders.*`.

- [ ] **Step 3: Provide the second base path.** In `app.config.ts`:
```ts
import { BASE_PATH as REMINDERS_BASE_PATH } from './generated/reminders-api';
```
```ts
    { provide: REMINDERS_BASE_PATH, useValue: '' },
```

- [ ] **Step 4: Invoke the `daisyui` skill** (template edit below), then update the components.

`reminder-section.component.ts`:
- Imports: replace the generated import with
```ts
import { RemindersService, ReminderViewModel } from '../../../../generated/reminders-api/index';
```
  and add `resource` to the `@angular/core` import.
- Replace the `reminders` input and the `reminderChanged` output with:
```ts
  private readonly api = inject(RemindersService);

  protected readonly remindersResource = resource({
    params: () => this.applicationId(),
    loader: ({ params }) => firstValueFrom(this.api.listReminders(params)),
  });
  protected readonly reminders = computed(() => this.remindersResource.value() ?? []);
```
- Replace every `this.reminderChanged.emit();` with `this.remindersResource.reload();`.
- Change `dueDateDisplay(reminder: ApplicationReminderViewModel)` to `dueDateDisplay(reminder: ReminderViewModel)`.
- API calls:
  - `this.api.createReminder({ applicationId: this.applicationId(), delayDays, note: note || null })`
  - `this.api.completeReminder(reminderId)`
  - `this.api.rescheduleReminder(reminderId, { newDueDate })`
  - `this.api.deleteReminder(reminderId)`

`application-detail.component.html`: in `<app-reminder-section ...>`, delete the `[reminders]="app.reminders"` and `(reminderChanged)="applicationResource.reload()"` attributes. Keep `[applicationId]="app.id"`.

`reminders-list.component.ts`:
- Import `RemindersService, ReminderViewModel` from `'../../../../generated/reminders-api/index'`.
- Delete `applicationReminderApi`.
- Calls:
  - `this.remindersApi.listReminders()`
  - `this.remindersApi.completeReminder(reminder.id)`
  - `this.remindersApi.deleteReminder(reminder.id)`

If a generated method signature differs (e.g. the parameter order for `listReminders`), follow the generated `reminders.serviceInterface.ts`. It is the source of truth.

- [ ] **Step 5: Lint, format, build**

Run: `cd frontend && npx ng lint && npm run format:check && npx ng build`
Expected: all succeed.

- [ ] **Step 6: Update `CLAUDE.md`.** In the "Generated files" section:
  - The protected paths are `frontend/src/app/generated/api/` (monolith, 5231) and `frontend/src/app/generated/reminders-api/` (Reminders service, 5240).
  - `npm run generate:api` needs **both** services running.
  - Delete both folders before regenerating, so stale files go away.

- [ ] **Step 7: Verify in the browser.** Run the full stack (Postgres, monolith, Reminders, gateway, `npm start`) and invoke the `verify` skill.
  - Expected: the application detail and reminders pages load without errors.
  - Creating a reminder returns **404**, because snapshots are still empty until Milestone 3. That is the documented interim state.
  - Seed one snapshot to prove the UI path works:
    - `docker compose exec postgres psql -U yaam -d yaam_reminders -c "INSERT INTO \"ApplicationSnapshots\" VALUES ('<an application id from the UI>', 1, 'Seed Co', 'Dev', NULL, NULL, false);"`
    - Then add, reschedule, complete and delete a reminder in the UI.

- [ ] **Step 8: Commit**

```bash
git add frontend CLAUDE.md
git commit -m "feat(frontend): use reminders service client through the gateway"
```

**CHECKPOINT 2:** Stop. Review with the user:
- What is distributed now?
- Why does create-reminder 404?
- What did the client-side API composition cost?

Wait for questions.

---

## Milestone 3 — Events: outbox → RabbitMQ → inbox

### Task 8: Contracts, `Application.Version`, outbox staging and backfill

**Files:**
- Create: `backend/Yaam.Contracts/Yaam.Contracts.csproj`
- Create: `backend/Yaam.Contracts/MessageEnvelope.cs`
- Create: `backend/Yaam.Contracts/ContractSerializer.cs`
- Create: `backend/Yaam.Contracts/Applications/ApplicationUpserted.cs`
- Create: `backend/Yaam.Contracts/Applications/ApplicationDeleted.cs`
- Create: `backend/Yaam.Contracts/Applications/ApplicationTopology.cs`
- Modify: `backend/Yaam.Domain/Entities/Application.cs`
- Modify: `backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Outbox/OutboxMessage.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Outbox/OutboxMessageConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Outbox/ApplicationOutboxStager.cs`
- Modify: `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `backend/Yaam.Infrastructure/Yaam.Infrastructure.csproj`
- Create: `backend/Yaam.Infrastructure/Migrations/*_AddApplicationOutbox.cs` (generated + hand-edited)
- Create: `backend/Yaam.Tests.Integration/Applications/ApplicationOutboxTests.cs`
- Modify: `backend/Yaam.slnx`

**Interfaces:**
- Produces:
  - `Yaam.Contracts.MessageEnvelope(Guid MessageId, string Type, DateTimeOffset OccurredAt, JsonElement Payload)`
  - `Yaam.Contracts.ContractSerializer.Options` (`JsonSerializerOptions`, web defaults)
  - `Yaam.Contracts.Applications.ApplicationUpserted(Guid ApplicationId, long Version, string CompanyName, string Role, DateOnly? DateApplied, string? ContactName)`
  - `Yaam.Contracts.Applications.ApplicationDeleted(Guid ApplicationId, long Version)`
  - `ApplicationTopology.Exchange = "yaam.applications"`, `ApplicationTopology.RoutingKeyFor(string type) : string`
  - `OutboxMessage { long Id; Guid MessageId; string Type; Guid AggregateId; string Payload; string? TraceParent; DateTime OccurredAt; DateTime? ProcessedAt; int Attempts }`
  - `AppDbContext.OutboxMessages`

- [ ] **Step 1: Contracts library**

`Yaam.Contracts.csproj`: a plain `Microsoft.NET.Sdk` class library with no package references. Add it to `Yaam.slnx`, and reference it from `Yaam.Infrastructure.csproj`.

`MessageEnvelope.cs`:
```csharp
using System.Text.Json;

namespace Yaam.Contracts;

public sealed record MessageEnvelope(
    Guid MessageId,
    string Type,
    DateTimeOffset OccurredAt,
    JsonElement Payload);
```

`ContractSerializer.cs`:
```csharp
using System.Text.Json;

namespace Yaam.Contracts;

public static class ContractSerializer
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
```

`Applications/ApplicationUpserted.cs`:
```csharp
namespace Yaam.Contracts.Applications;

// Additive changes only. Renaming or removing a field breaks every consumer still on the old shape.
public sealed record ApplicationUpserted(
    Guid ApplicationId,
    long Version,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    string? ContactName);
```

`Applications/ApplicationDeleted.cs`:
```csharp
namespace Yaam.Contracts.Applications;

public sealed record ApplicationDeleted(Guid ApplicationId, long Version);
```

`Applications/ApplicationTopology.cs`:
```csharp
namespace Yaam.Contracts.Applications;

public static class ApplicationTopology
{
    public const string Exchange = "yaam.applications";

    public static string RoutingKeyFor(string type) => type switch
    {
        nameof(ApplicationUpserted) => "application.upserted",
        nameof(ApplicationDeleted) => "application.deleted",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown application event type."),
    };
}
```

- [ ] **Step 2: Write the failing outbox tests**

`backend/Yaam.Tests.Integration/Applications/ApplicationOutboxTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.API.Applications;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Domain.Errors;
using Yaam.Infrastructure.Persistence;
using Yaam.Infrastructure.Persistence.Outbox;

namespace Yaam.Tests.Integration.Applications;

[Collection("Integration")]
public class ApplicationOutboxTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task Create_WritesUpsertedMessage_WithVersion1()
    {
        var application = await CreateApplicationAsync("Outbox Co");

        var message = (await GetOutboxMessagesAsync(application.Id)).Should().ContainSingle().Subject;
        message.Type.Should().Be(nameof(ApplicationUpserted));
        message.ProcessedAt.Should().BeNull();
        var payload = JsonSerializer.Deserialize<ApplicationUpserted>(message.Payload, ContractSerializer.Options)!;
        payload.Should().BeEquivalentTo(new ApplicationUpserted(application.Id, 1, "Outbox Co", "Dev", null, null));
    }

    [Fact]
    public async Task Update_WritesUpsertedMessage_WithIncrementedVersion()
    {
        var application = await CreateApplicationAsync("Before Co");

        var response = await _client.PutAsJsonAsync($"/api/applications/{application.Id}",
            new { companyName = "After Co", role = "Dev", status = "Applied" });
        response.EnsureSuccessStatusCode();

        var messages = await GetOutboxMessagesAsync(application.Id);
        messages.Should().HaveCount(2);
        var payload = JsonSerializer.Deserialize<ApplicationUpserted>(messages[1].Payload, ContractSerializer.Options)!;
        payload.Version.Should().Be(2);
        payload.CompanyName.Should().Be("After Co");
    }

    [Fact]
    public async Task Delete_WritesDeletedMessage_WithNextVersion()
    {
        var application = await CreateApplicationAsync("Doomed Co");

        (await _client.DeleteAsync($"/api/applications/{application.Id}")).EnsureSuccessStatusCode();

        var last = (await GetOutboxMessagesAsync(application.Id)).Last();
        last.Type.Should().Be(nameof(ApplicationDeleted));
        JsonSerializer.Deserialize<ApplicationDeleted>(last.Payload, ContractSerializer.Options)!
            .Should().Be(new ApplicationDeleted(application.Id, 2));
    }

    [Fact]
    public async Task ConcurrentUpdate_Returns409Conflict_AndWritesNoOutboxMessage()
    {
        var application = await CreateApplicationAsync("Race Co");
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstCopy = await firstDb.Applications.SingleAsync(a => a.Id == application.Id);
        var secondCopy = await secondDb.Applications.SingleAsync(a => a.Id == application.Id);

        firstCopy.Role = "First writer";
        await firstDb.SaveChangesAsync();
        secondCopy.Role = "Second writer";
        var secondSave = () => secondDb.SaveChangesAsync();

        await secondSave.Should().ThrowAsync<ConflictException>();
        (await GetOutboxMessagesAsync(application.Id)).Should().HaveCount(2);
    }

    private async Task<ApplicationViewModel> CreateApplicationAsync(string companyName)
    {
        var response = await _client.PostAsJsonAsync("/api/applications",
            new { companyName, role = "Dev", status = "Draft" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApplicationViewModel>())!;
    }

    private async Task<List<OutboxMessage>> GetOutboxMessagesAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.OutboxMessages.AsNoTracking()
            .Where(m => m.AggregateId == applicationId)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }
}
```
If `POST /api/applications` returns 200 rather than 201 in the existing controller, adjust the status assertion to match `ApplicationsEndpointsTests`. Check that file first.

Run: `dotnet test Yaam.Tests.Integration --filter ApplicationOutboxTests`
Expected: compile failure (`OutboxMessage` missing).

- [ ] **Step 3: `Version` as a concurrency token.**
  - In `Application.cs`, add `public long Version { get; set; }`.
  - In `ApplicationConfiguration.Configure`, add:
```csharp
        builder.Property(a => a.Version).IsConcurrencyToken();
```

- [ ] **Step 4: Outbox entity and configuration**

`Persistence/Outbox/OutboxMessage.cs`:
```csharp
namespace Yaam.Infrastructure.Persistence.Outbox;

public class OutboxMessage
{
    public long Id { get; init; }
    public Guid MessageId { get; init; }
    public required string Type { get; init; }
    public Guid AggregateId { get; init; }
    public required string Payload { get; init; }
    public string? TraceParent { get; init; }
    public DateTime OccurredAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
}
```

`Persistence/Outbox/OutboxMessageConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Yaam.Infrastructure.Persistence.Outbox;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(m => m.TraceParent).HasMaxLength(55);
        builder.HasIndex(m => m.MessageId).IsUnique();
        // Partial index: the relay only ever scans pending rows.
        builder.HasIndex(m => m.Id).HasFilter("\"ProcessedAt\" IS NULL").HasDatabaseName("IX_OutboxMessages_Pending");
    }
}
```

- [ ] **Step 5: Stager**

`Persistence/Outbox/ApplicationOutboxStager.cs`:
```csharp
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Outbox;

// Transitional (ADR 008): derives integration events from the ChangeTracker diff.
// Extracted services raise domain events instead.
internal static class ApplicationOutboxStager
{
    internal static void Stage(AppDbContext db)
    {
        var occurredAt = DateTime.UtcNow;
        var traceParent = Activity.Current?.Id;
        var messages = new List<OutboxMessage>();

        foreach (var entry in db.ChangeTracker.Entries<Application>())
        {
            var message = ToOutboxMessage(entry, occurredAt, traceParent);
            if (message is not null)
                messages.Add(message);
        }

        db.OutboxMessages.AddRange(messages);
    }

    private static OutboxMessage? ToOutboxMessage(
        EntityEntry<Application> entry, DateTime occurredAt, string? traceParent)
    {
        var application = entry.Entity;
        switch (entry.State)
        {
            case EntityState.Added:
                application.Version = 1;
                return Create(application.Id, ToUpserted(application), occurredAt, traceParent);
            case EntityState.Modified:
                application.Version++;
                return Create(application.Id, ToUpserted(application), occurredAt, traceParent);
            case EntityState.Deleted:
                return Create(
                    application.Id,
                    new ApplicationDeleted(application.Id, application.Version + 1),
                    occurredAt,
                    traceParent);
            default:
                return null;
        }
    }

    private static ApplicationUpserted ToUpserted(Application application) => new(
        application.Id, application.Version, application.CompanyName,
        application.Role, application.DateApplied, application.ContactName);

    private static OutboxMessage Create<TEvent>(
        Guid aggregateId, TEvent integrationEvent, DateTime occurredAt, string? traceParent) => new()
    {
        MessageId = Guid.NewGuid(),
        Type = typeof(TEvent).Name,
        AggregateId = aggregateId,
        Payload = JsonSerializer.Serialize(integrationEvent, ContractSerializer.Options),
        TraceParent = traceParent,
        OccurredAt = occurredAt,
    };
}
```

- [ ] **Step 6: `AppDbContext`.**
  - Add `public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();` and the usings `Yaam.Infrastructure.Persistence.Outbox` and `Yaam.Domain.Errors`.
  - Replace `SaveChangesAsync` with:

```csharp
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        // Same SaveChanges = same transaction: the state change and its event commit or fail together.
        ApplicationOutboxStager.Stage(this);

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The application was changed by someone else. Reload and try again.");
        }
    }
```

- [ ] **Step 7: Migration with backfill**

Run (from `backend/`):
```bash
dotnet ef migrations add AddApplicationOutbox --project Yaam.Infrastructure --startup-project Yaam.API -- --environment Development
```
Open the generated migration and append to the end of `Up`, after the `CreateTable`/`CreateIndex` calls:
```csharp
            migrationBuilder.Sql("""UPDATE "Applications" SET "Version" = 1;""");
            migrationBuilder.Sql("""
                INSERT INTO "OutboxMessages" ("MessageId", "Type", "AggregateId", "Payload", "OccurredAt", "Attempts")
                SELECT gen_random_uuid(), 'ApplicationUpserted', a."Id",
                       jsonb_build_object(
                           'applicationId', a."Id", 'version', a."Version",
                           'companyName', a."CompanyName", 'role', a."Role",
                           'dateApplied', a."DateApplied", 'contactName', a."ContactName"),
                       now(), 0
                FROM "Applications" a;
                """);
```
- Why: the backfill pushes existing applications through the same pipeline as live changes, so there is no special-case import path into Reminders.
- Then run `dotnet ef database update --project Yaam.Infrastructure --startup-project Yaam.API -- --environment Development`.
- Expected: `SELECT count(*) FROM "OutboxMessages"` equals `SELECT count(*) FROM "Applications"`.

- [ ] **Step 8: Run the tests**

Run: `dotnet test Yaam.Tests.Integration && dotnet test Yaam.Tests.Unit`
Expected: all PASS, including the four new outbox tests and every existing Applications test.

- [ ] **Step 9: Commit**

```bash
git add backend
git commit -m "feat(applications): stage integration events in a transactional outbox"
```

### Task 9: RabbitMQ and the outbox relay

**Files:**
- Modify: `docker-compose.yml`
- Modify: `backend/Yaam.ServiceDefaults/Yaam.ServiceDefaults.csproj`
- Create: `backend/Yaam.ServiceDefaults/Messaging/RabbitMqSettings.cs`
- Create: `backend/Yaam.ServiceDefaults/Messaging/RabbitMqConnectionProvider.cs`
- Create: `backend/Yaam.ServiceDefaults/Messaging/MessagingExtensions.cs`
- Create: `backend/Yaam.Infrastructure/Messaging/OutboxSettings.cs`
- Create: `backend/Yaam.Infrastructure/Messaging/OutboxRelay.cs`
- Modify: `backend/Yaam.API/Program.cs`
- Modify: `backend/Yaam.API/appsettings.json`
- Modify: `backend/Yaam.API/appsettings.Development.json`
- Modify: `backend/Yaam.Tests.Integration/ApiFactory.cs`

**Interfaces:**
- Produces:
  - `RabbitMqConnectionProvider.GetConnectionAsync(CancellationToken cancellationToken) : Task<IConnection>`
  - `MessagingExtensions.AddYaamRabbitMq(this IServiceCollection services, IConfiguration configuration)`
  - configuration section `"RabbitMq"` and `OutboxSettings` section `"Outbox"`

- [ ] **Step 1: Broker.** Add to `docker-compose.yml` under `services:`:
```yaml
  rabbitmq:
    image: rabbitmq:4-management
    environment:
      RABBITMQ_DEFAULT_USER: yaam
      RABBITMQ_DEFAULT_PASS: yaam
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 5s
      timeout: 5s
      retries: 10
```
Run: `docker compose up -d rabbitmq` and open `http://localhost:15672` (yaam/yaam).
Expected: the management UI loads.

- [ ] **Step 2: Connection plumbing.** Add `<PackageReference Include="RabbitMQ.Client" Version="7.2.2" />` to `Yaam.ServiceDefaults.csproj`.

`Messaging/RabbitMqSettings.cs`:
```csharp
namespace Yaam.ServiceDefaults.Messaging;

public class RabbitMqSettings
{
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
}
```

`Messaging/RabbitMqConnectionProvider.cs`:
```csharp
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Yaam.ServiceDefaults.Messaging;

public sealed class RabbitMqConnectionProvider(IOptions<RabbitMqSettings> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var settings = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                ClientProvidedName = settings.ClientName,
                // The relay and consumer loops own reconnection; one recovery mechanism is easier to reason about.
                AutomaticRecoveryEnabled = false,
            };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _gate.Dispose();
    }
}
```

`Messaging/MessagingExtensions.cs`:
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Yaam.ServiceDefaults.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddYaamRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnectionProvider>();
        return services;
    }
}
```

- [ ] **Step 3: Relay**

`backend/Yaam.Infrastructure/Messaging/OutboxSettings.cs`:
```csharp
namespace Yaam.Infrastructure.Messaging;

public class OutboxSettings
{
    public int PollingIntervalMilliseconds { get; init; } = 1000;
    public int BatchSize { get; init; } = 50;
    public bool SimulateCrashAfterPublish { get; init; }
}
```

`backend/Yaam.Infrastructure/Messaging/OutboxRelay.cs`:
```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Infrastructure.Persistence;
using Yaam.Infrastructure.Persistence.Outbox;
using Yaam.ServiceDefaults.Messaging;

namespace Yaam.Infrastructure.Messaging;

public sealed class OutboxRelay(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnectionProvider connectionProvider,
    IOptions<OutboxSettings> options,
    ILogger<OutboxRelay> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(options.Value.PollingIntervalMilliseconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Outbox relay iteration failed; retrying in {Interval}", interval);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var batchSize = options.Value.BatchSize;
        // SKIP LOCKED lets several relay instances share the table without publishing the same row concurrently.
        var pending = await db.OutboxMessages
            .FromSql($"""SELECT * FROM "OutboxMessages" WHERE "ProcessedAt" IS NULL ORDER BY "Id" LIMIT {batchSize} FOR UPDATE SKIP LOCKED""")
            .ToListAsync(cancellationToken);
        if (pending.Count == 0)
            return;

        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await channel.ExchangeDeclareAsync(
            ApplicationTopology.Exchange, ExchangeType.Topic, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await PublishAsync(channel, message, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                message.Attempts++;
                logger.LogWarning(exception,
                    "Publishing outbox message {MessageId} failed (attempt {Attempts})", message.MessageId, message.Attempts);
                // Stop the batch: publishing a later message before an earlier one would break per-aggregate ordering.
                break;
            }

            if (options.Value.SimulateCrashAfterPublish)
                throw new InvalidOperationException("Simulated crash after publish, before marking processed.");

            message.ProcessedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task PublishAsync(IChannel channel, OutboxMessage message, CancellationToken cancellationToken)
    {
        using var payload = JsonDocument.Parse(message.Payload);
        var envelope = new MessageEnvelope(
            message.MessageId, message.Type, new DateTimeOffset(message.OccurredAt, TimeSpan.Zero), payload.RootElement);
        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, ContractSerializer.Options);
        var properties = new BasicProperties
        {
            MessageId = message.MessageId.ToString(),
            Type = message.Type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>(),
        };

        // mandatory: an unroutable message (no queue bound yet) fails the publish instead of vanishing.
        await channel.BasicPublishAsync(
            ApplicationTopology.Exchange,
            ApplicationTopology.RoutingKeyFor(message.Type),
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
```

- [ ] **Step 4: Wire up the monolith.**
  - In `backend/Yaam.API/Program.cs`, after `AddYaamAuthentication`:
```csharp
builder.Services.AddYaamRabbitMq(builder.Configuration);
builder.Services.Configure<OutboxSettings>(builder.Configuration.GetSection("Outbox"));
builder.Services.AddHostedService<OutboxRelay>();
```
    (usings `Yaam.Infrastructure.Messaging`, `Yaam.ServiceDefaults.Messaging`).
  - In `appsettings.json`, add `"Outbox": { "PollingIntervalMilliseconds": 1000, "BatchSize": 50, "SimulateCrashAfterPublish": false }`.
  - In `appsettings.Development.json`, add `"RabbitMq": { "HostName": "localhost", "Port": 5672, "UserName": "yaam", "Password": "yaam", "ClientName": "yaam-api" }`.

  In `ApiFactory.ConfigureServices`, remove the relay so tests can assert on pending rows and don't need a broker:
```csharp
            services.Remove(services.Single(d => d.ImplementationType == typeof(OutboxRelay)));
```

- [ ] **Step 5: Build and run the tests**

Run: `cd backend && dotnet build && dotnet test Yaam.Tests.Integration`
Expected: PASS.

- [ ] **Step 6: Review Focus 1, broker down.**
  - Run `docker compose stop rabbitmq`, then start the monolith.
  - Expected:
    - It starts.
    - `curl -i http://localhost:5231/api/applications` returns 401, so HTTP serves normally.
    - The log shows `Outbox relay iteration failed` warnings once per second, and no crash.
  - Run `docker compose start rabbitmq`. Expected: the warnings change (see Step 7).

- [ ] **Step 7: Review Focus 2, nothing bound yet.** With RabbitMQ up and the monolith running (Reminders not yet consuming), look at the pending rows:
  - `docker compose exec postgres psql -U yaam -d yaam -c 'SELECT "Id","Type","Attempts","ProcessedAt" FROM "OutboxMessages" ORDER BY "Id" LIMIT 5;'`
  - Expected:
    - the exchange `yaam.applications` exists in the UI;
    - the rows stay `ProcessedAt = NULL` and `Attempts` climbs;
    - the log says the publish failed because the message was returned (unroutable).
  - **If the rows get `ProcessedAt` set instead, stop and tell the user:** the client did not surface `basic.return`, the messages were dropped, and the design must change (e.g. declare the queue from deployment config).

- [ ] **Step 8: Commit**

```bash
git add docker-compose.yml backend
git commit -m "feat(messaging): publish outbox messages to RabbitMQ with confirms"
```

### Task 10: Reminders consumer — inbox, versioning, tombstones

**Files:**
- Modify: `backend/Yaam.Reminders/Yaam.Reminders.csproj` (add a `Yaam.Contracts` project reference)
- Create: `backend/Yaam.Reminders/ApplicationEvents/ConsumeOutcome.cs`
- Create: `backend/Yaam.Reminders/ApplicationEvents/ApplicationEventHandler.cs`
- Create: `backend/Yaam.Reminders/ApplicationEvents/ApplicationEventsTopology.cs`
- Create: `backend/Yaam.Reminders/ApplicationEvents/ApplicationEventsConsumer.cs`
- Modify: `backend/Yaam.Reminders/Program.cs`
- Modify: `backend/Yaam.Reminders/appsettings.Development.json`
- Modify: `backend/Yaam.Reminders.Tests/RemindersApiFactory.cs`
- Create: `backend/Yaam.Reminders.Tests/Integration/ApplicationEventHandlerTests.cs`

**Interfaces:**
- Consumes: `MessageEnvelope`, `ApplicationUpserted`, `ApplicationDeleted`, `ContractSerializer`, `ApplicationTopology.Exchange` (Task 8); `RabbitMqConnectionProvider`, `AddYaamRabbitMq` (Task 9)
- Produces:
  - `enum ConsumeOutcome { Processed, Duplicate, Stale, Ignored, Failed }`
  - `ApplicationEventHandler.HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken) : Task<ConsumeOutcome>`

- [ ] **Step 1: Write the failing handler tests**

`Integration/ApplicationEventHandlerTests.cs`:
```csharp
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Reminders.ApplicationEvents;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.Tests.Integration;

[Collection("Reminders")]
public class ApplicationEventHandlerTests(RemindersApiFactory factory)
{
    [Fact]
    public async Task Upserted_CreatesSnapshot()
    {
        var applicationId = Guid.NewGuid();

        var outcome = await HandleAsync(Envelope(Upserted(applicationId, 1, "Acme")));

        outcome.Should().Be(ConsumeOutcome.Processed);
        (await GetSnapshotAsync(applicationId))!.CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task SameMessageTwice_IsAppliedOnce()
    {
        var applicationId = Guid.NewGuid();
        await HandleAsync(Envelope(Upserted(applicationId, 1, "Acme")));
        var update = Envelope(Upserted(applicationId, 2, "Acme v2"));

        var first = await HandleAsync(update);
        var second = await HandleAsync(update);

        first.Should().Be(ConsumeOutcome.Processed);
        second.Should().Be(ConsumeOutcome.Duplicate);
    }

    [Fact]
    public async Task OlderVersion_IsIgnoredAsStale()
    {
        var applicationId = Guid.NewGuid();
        await HandleAsync(Envelope(Upserted(applicationId, 3, "Newest")));

        var outcome = await HandleAsync(Envelope(Upserted(applicationId, 2, "Older")));

        outcome.Should().Be(ConsumeOutcome.Stale);
        (await GetSnapshotAsync(applicationId))!.CompanyName.Should().Be("Newest");
    }

    [Fact]
    public async Task Deleted_TombstonesSnapshot_AndDeletesItsReminders()
    {
        var applicationId = Guid.NewGuid();
        await HandleAsync(Envelope(Upserted(applicationId, 1, "Acme")));
        await SeedReminderAsync(applicationId);

        var outcome = await HandleAsync(Envelope(new ApplicationDeleted(applicationId, 2)));

        outcome.Should().Be(ConsumeOutcome.Processed);
        (await GetSnapshotAsync(applicationId))!.IsDeleted.Should().BeTrue();
        (await CountRemindersAsync(applicationId)).Should().Be(0);
    }

    [Fact]
    public async Task UpsertedAfterDeleted_StaysDeleted()
    {
        var applicationId = Guid.NewGuid();
        await HandleAsync(Envelope(Upserted(applicationId, 1, "Acme")));
        await HandleAsync(Envelope(new ApplicationDeleted(applicationId, 3)));

        var outcome = await HandleAsync(Envelope(Upserted(applicationId, 2, "Zombie")));

        outcome.Should().Be(ConsumeOutcome.Stale);
        (await GetSnapshotAsync(applicationId))!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeletedBeforeAnyUpsert_InsertsTombstone()
    {
        var applicationId = Guid.NewGuid();

        await HandleAsync(Envelope(new ApplicationDeleted(applicationId, 2)));
        var late = await HandleAsync(Envelope(Upserted(applicationId, 1, "Late")));

        late.Should().Be(ConsumeOutcome.Stale);
        (await GetSnapshotAsync(applicationId))!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task UnknownType_IsIgnored_AndRecordedAsProcessed()
    {
        var envelope = new MessageEnvelope(
            Guid.NewGuid(), "ApplicationArchived", DateTimeOffset.UtcNow, JsonDocument.Parse("{}").RootElement);

        var first = await HandleAsync(envelope);
        var second = await HandleAsync(envelope);

        first.Should().Be(ConsumeOutcome.Ignored);
        second.Should().Be(ConsumeOutcome.Duplicate);
    }

    [Fact]
    public async Task MalformedPayload_Throws_SoTheMessageIsNackedAndDeadLettered()
    {
        var envelope = new MessageEnvelope(
            Guid.NewGuid(), nameof(ApplicationUpserted), DateTimeOffset.UtcNow,
            JsonDocument.Parse("""{"applicationId":"not-a-guid"}""").RootElement);

        var act = () => HandleAsync(envelope);

        await act.Should().ThrowAsync<JsonException>();
    }

    private static ApplicationUpserted Upserted(Guid applicationId, long version, string companyName)
        => new(applicationId, version, companyName, "Developer", null, null);

    private static MessageEnvelope Envelope<TEvent>(TEvent integrationEvent) => new(
        Guid.NewGuid(), typeof(TEvent).Name, DateTimeOffset.UtcNow,
        JsonSerializer.SerializeToElement(integrationEvent, ContractSerializer.Options));

    private async Task<ConsumeOutcome> HandleAsync(MessageEnvelope envelope)
    {
        using var scope = factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ApplicationEventHandler>();
        return await handler.HandleAsync(envelope, CancellationToken.None);
    }

    private async Task<ApplicationSnapshot?> GetSnapshotAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        return await db.ApplicationSnapshots.AsNoTracking().SingleOrDefaultAsync(s => s.ApplicationId == applicationId);
    }

    private async Task SeedReminderAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        db.Reminders.Add(new Reminder { ApplicationId = applicationId, DelayDays = 7, DueDate = new DateOnly(2030, 1, 1) });
        await db.SaveChangesAsync();
    }

    private async Task<int> CountRemindersAsync(Guid applicationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RemindersDbContext>();
        return await db.Reminders.CountAsync(r => r.ApplicationId == applicationId);
    }
}
```

Run: `dotnet test Yaam.Reminders.Tests --filter ApplicationEventHandlerTests`
Expected: compile failure (handler missing).

- [ ] **Step 2: Outcome and handler**

`ApplicationEvents/ConsumeOutcome.cs`:
```csharp
namespace Yaam.Reminders.ApplicationEvents;

public enum ConsumeOutcome
{
    Processed,
    Duplicate,
    Stale,
    Ignored,
    Failed,
}
```

`ApplicationEvents/ApplicationEventHandler.cs`:
```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Yaam.Contracts;
using Yaam.Contracts.Applications;
using Yaam.Reminders.Domain;
using Yaam.Reminders.Persistence;

namespace Yaam.Reminders.ApplicationEvents;

public sealed class ApplicationEventHandler(RemindersDbContext db, TimeProvider timeProvider)
{
    public async Task<ConsumeOutcome> HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        // Inbox check, state change and inbox insert share one transaction: a redelivery after a crash
        // either finds the message recorded (skip) or finds nothing applied (redo).
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == envelope.MessageId, cancellationToken))
            return ConsumeOutcome.Duplicate;

        var outcome = envelope.Type switch
        {
            nameof(ApplicationUpserted) => await ApplyAsync(Read<ApplicationUpserted>(envelope), cancellationToken),
            nameof(ApplicationDeleted) => await ApplyAsync(Read<ApplicationDeleted>(envelope), cancellationToken),
            // Tolerant reader: a newer producer may emit types this consumer doesn't know yet.
            _ => ConsumeOutcome.Ignored,
        };

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = envelope.MessageId,
            ProcessedAt = timeProvider.GetUtcNow().UtcDateTime,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return outcome;
    }

    private async Task<ConsumeOutcome> ApplyAsync(ApplicationUpserted upserted, CancellationToken cancellationToken)
    {
        var snapshot = await db.ApplicationSnapshots.FindAsync([upserted.ApplicationId], cancellationToken);
        if (snapshot is null)
        {
            db.ApplicationSnapshots.Add(new ApplicationSnapshot
            {
                ApplicationId = upserted.ApplicationId,
                Version = upserted.Version,
                CompanyName = upserted.CompanyName,
                Role = upserted.Role,
                DateApplied = upserted.DateApplied,
                ContactName = upserted.ContactName,
            });
            return ConsumeOutcome.Processed;
        }

        if (upserted.Version <= snapshot.Version)
            return ConsumeOutcome.Stale;

        snapshot.Version = upserted.Version;
        snapshot.CompanyName = upserted.CompanyName;
        snapshot.Role = upserted.Role;
        snapshot.DateApplied = upserted.DateApplied;
        snapshot.ContactName = upserted.ContactName;
        return ConsumeOutcome.Processed;
    }

    private async Task<ConsumeOutcome> ApplyAsync(ApplicationDeleted deleted, CancellationToken cancellationToken)
    {
        var snapshot = await db.ApplicationSnapshots.FindAsync([deleted.ApplicationId], cancellationToken);
        if (snapshot is null)
        {
            // Tombstone even without prior state, so a late upsert can't resurrect the application.
            db.ApplicationSnapshots.Add(new ApplicationSnapshot
            {
                ApplicationId = deleted.ApplicationId,
                Version = deleted.Version,
                CompanyName = string.Empty,
                Role = string.Empty,
                IsDeleted = true,
            });
            return ConsumeOutcome.Processed;
        }

        if (deleted.Version <= snapshot.Version)
            return ConsumeOutcome.Stale;

        snapshot.Version = deleted.Version;
        snapshot.IsDeleted = true;
        await db.Reminders
            .Where(r => r.ApplicationId == deleted.ApplicationId)
            .ExecuteDeleteAsync(cancellationToken);
        return ConsumeOutcome.Processed;
    }

    private static TEvent Read<TEvent>(MessageEnvelope envelope)
        => envelope.Payload.Deserialize<TEvent>(ContractSerializer.Options)
           ?? throw new JsonException($"Empty {typeof(TEvent).Name} payload.");
}
```
Add `<ProjectReference Include="..\Yaam.Contracts\Yaam.Contracts.csproj" />` to `Yaam.Reminders.csproj`. In `Program.cs`, add `builder.Services.AddScoped<ApplicationEventHandler>();`.

- [ ] **Step 3: Run the tests**

Run: `dotnet test Yaam.Reminders.Tests --filter ApplicationEventHandlerTests`
Expected: all 8 PASS.

- [ ] **Step 4: Topology**

`ApplicationEvents/ApplicationEventsTopology.cs`:
```csharp
using RabbitMQ.Client;
using Yaam.Contracts.Applications;

namespace Yaam.Reminders.ApplicationEvents;

// The consumer owns its queue; the producer only knows the exchange.
internal static class ApplicationEventsTopology
{
    internal const string Queue = "reminders.application-events";
    internal const string DeadLetterExchange = "reminders.dead-letter";
    internal const string DeadLetterQueue = "reminders.application-events.dlq";

    internal static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            ApplicationTopology.Exchange, ExchangeType.Topic, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
            DeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            DeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, routingKey: string.Empty,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-delivery-limit"] = 5,
                ["x-dead-letter-exchange"] = DeadLetterExchange,
            },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(Queue, ApplicationTopology.Exchange, routingKey: "application.*",
            cancellationToken: cancellationToken);
    }
}
```

- [ ] **Step 5: Consumer hosted service**

`ApplicationEvents/ApplicationEventsConsumer.cs`:
```csharp
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Yaam.Contracts;
using Yaam.ServiceDefaults.Messaging;

namespace Yaam.Reminders.ApplicationEvents;

public sealed class ApplicationEventsConsumer(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnectionProvider connectionProvider,
    ILogger<ApplicationEventsConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilChannelClosesAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Application event consumer disconnected; reconnecting in {Delay}", ReconnectDelay);
            }

            await Task.Delay(ReconnectDelay, stoppingToken);
        }
    }

    private async Task ConsumeUntilChannelClosesAsync(CancellationToken stoppingToken)
    {
        var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await ApplicationEventsTopology.DeclareAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var closed = new TaskCompletionSource();
        channel.ChannelShutdownAsync += (_, _) =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnReceivedAsync(channel, delivery, stoppingToken);
        await channel.BasicConsumeAsync(ApplicationEventsTopology.Queue, autoAck: false, consumer, stoppingToken);
        logger.LogInformation("Consuming {Queue}", ApplicationEventsTopology.Queue);

        await closed.Task.WaitAsync(stoppingToken);
    }

    private async Task OnReceivedAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<MessageEnvelope>(delivery.Body.Span, ContractSerializer.Options)
                ?? throw new JsonException("Empty message body.");

            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ApplicationEventHandler>();
            var outcome = await handler.HandleAsync(envelope, cancellationToken);

            logger.LogInformation("Message {MessageId} ({Type}) → {Outcome}", envelope.MessageId, envelope.Type, outcome);
            // Ack only after the DB commit: a crash before this line means redelivery, which the inbox absorbs.
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to process message {MessageId}", delivery.BasicProperties.MessageId);
            // requeue: the quorum queue counts deliveries and dead-letters after x-delivery-limit.
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }
}
```
If a RabbitMQ.Client 7.2 member name differs (`ChannelShutdownAsync`, `ReceivedAsync`, `BasicQosAsync` parameter names), fix the call to match the library's IntelliSense. Keep the behaviour: manual ack, ack after commit, nack with requeue.

- [ ] **Step 6: Wire up.**
  - In `Program.cs`, add:
```csharp
builder.Services.AddYaamRabbitMq(builder.Configuration);
builder.Services.AddHostedService<ApplicationEventsConsumer>();
```
    (usings `Yaam.Reminders.ApplicationEvents`, `Yaam.ServiceDefaults.Messaging`).
  - In `appsettings.Development.json`, add `"RabbitMq": { "HostName": "localhost", "Port": 5672, "UserName": "yaam", "Password": "yaam", "ClientName": "yaam-reminders" }`.
  - In `RemindersApiFactory.ConfigureServices`, also remove the consumer:
```csharp
            services.Remove(services.Single(d => d.ImplementationType == typeof(ApplicationEventsConsumer)));
```

- [ ] **Step 7: Full test run**

Run: `cd backend && dotnet build && dotnet test Yaam.Reminders.Tests && dotnet test Yaam.Tests.Integration && dotnet format --verify-no-changes`
Expected: all pass.

- [ ] **Step 8: End-to-end check, including Review Focus 1 for Reminders.**
  - Stop RabbitMQ, start Reminders. Expected: it starts, `curl -i http://localhost:5240/api/reminders` returns 401, and the log shows reconnect warnings.
  - Start RabbitMQ again. Expected:
    - Within about 5 s, the log shows `Consuming reminders.application-events`.
    - The RabbitMQ UI shows the queue, the DLQ and the binding.
    - The monolith's pending outbox rows (the backfill from Task 8) drain: all have `ProcessedAt` set.
    - `SELECT count(*) FROM "ApplicationSnapshots"` in `yaam_reminders` equals the number of applications.
  - Then, in the UI:
    - Create a new application, and add a reminder to it. Expected: works, no 404.
    - Rename the application. Expected: the reminders page shows the new company name.
    - Delete the application. Expected: its reminders disappear.

- [ ] **Step 9: Commit**

```bash
git add backend
git commit -m "feat(reminders): consume application events with inbox, versioning and tombstones"
```

**CHECKPOINT 3:** Stop. This is the core milestone. Walk the user through:
- one message's life (DB tx → outbox row → relay → exchange → queue → inbox tx → ack);
- every place a crash could happen, and why each one is safe.

Ask them to explain it back. That is interview rehearsal.

---

## Milestone 4 — Observability

### Task 11: OpenTelemetry tracing across the async hop, plus messaging metrics

**Files:**
- Modify: `docker-compose.yml`
- Modify: `backend/Yaam.ServiceDefaults/Yaam.ServiceDefaults.csproj`
- Create: `backend/Yaam.ServiceDefaults/Telemetry/TelemetryExtensions.cs`
- Create: `backend/Yaam.ServiceDefaults/Telemetry/MessagingTelemetry.cs`
- Create: `backend/Yaam.Infrastructure/Messaging/OutboxMetrics.cs`
- Modify: `backend/Yaam.Infrastructure/Messaging/OutboxRelay.cs`
- Create: `backend/Yaam.Reminders/ApplicationEvents/ConsumerMetrics.cs`
- Modify: `backend/Yaam.Reminders/ApplicationEvents/ApplicationEventsConsumer.cs`
- Modify: `backend/Yaam.API/Program.cs`, `backend/Yaam.Reminders/Program.cs`, `backend/Yaam.Gateway/Program.cs`, `backend/Yaam.Gateway/Yaam.Gateway.csproj`

**Interfaces:**
- Produces:
  - `TelemetryExtensions.AddYaamTelemetry(this WebApplicationBuilder builder, string serviceName) : WebApplicationBuilder`
  - `MessagingTelemetry.StartPublishActivity(string? traceParent, string messageType) : Activity?`
  - `MessagingTelemetry.StartConsumeActivity(IDictionary<string, object?>? headers, string? messageType) : Activity?`
  - `MessagingTelemetry.TraceParentHeader = "traceparent"`

- [ ] **Step 1: Backend for telemetry.** Add to `docker-compose.yml`:
```yaml
  otel-lgtm:
    image: grafana/otel-lgtm:latest
    ports:
      - "3000:3000"
      - "4317:4317"
      - "4318:4318"
```
Run: `docker compose up -d otel-lgtm` and open `http://localhost:3000` (default login admin/admin).
Expected: Grafana loads, with Tempo, Prometheus and Loki data sources preconfigured. If the image name or ports differ from the current `grafana/otel-lgtm` README, follow the README and update this step.

- [ ] **Step 2: Packages.** Add to `Yaam.ServiceDefaults.csproj`:
```xml
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.19.1" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.19.1" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.19.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.19.0" />
    <PackageReference Include="Npgsql.OpenTelemetry" Version="10.0.3" />
```
Add `<ProjectReference Include="..\Yaam.ServiceDefaults\Yaam.ServiceDefaults.csproj" />` to `Yaam.Gateway.csproj`.

- [ ] **Step 3: Telemetry helpers**

`Telemetry/MessagingTelemetry.cs`:
```csharp
using System.Diagnostics;
using System.Text;

namespace Yaam.ServiceDefaults.Telemetry;

public static class MessagingTelemetry
{
    public const string SourceName = "Yaam.Messaging";
    public const string TraceParentHeader = "traceparent";

    private static readonly ActivitySource Source = new(SourceName);

    // The relay runs outside any request, so the parent is the trace context stored on the outbox row.
    public static Activity? StartPublishActivity(string? traceParent, string messageType)
    {
        ActivityContext.TryParse(traceParent, null, isRemote: true, out var parent);
        var activity = Source.StartActivity($"publish {messageType}", ActivityKind.Producer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.message.type", messageType);
        return activity;
    }

    public static Activity? StartConsumeActivity(IDictionary<string, object?>? headers, string? messageType)
    {
        string? traceParent = null;
        if (headers is not null && headers.TryGetValue(TraceParentHeader, out var value) && value is byte[] bytes)
            traceParent = Encoding.UTF8.GetString(bytes);

        ActivityContext.TryParse(traceParent, null, isRemote: true, out var parent);
        var activity = Source.StartActivity($"process {messageType}", ActivityKind.Consumer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.message.type", messageType);
        return activity;
    }
}
```

`Telemetry/TelemetryExtensions.cs`:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Yaam.ServiceDefaults.Telemetry;

public static class TelemetryExtensions
{
    public static WebApplicationBuilder AddYaamTelemetry(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddSource(MessagingTelemetry.SourceName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(MessagingTelemetry.SourceName))
            .UseOtlpExporter();

        return builder;
    }
}
```
`UseOtlpExporter()` defaults to `http://localhost:4317` (gRPC) and honours `OTEL_EXPORTER_OTLP_ENDPOINT`.

- [ ] **Step 4: Call it in each `Program.cs`, right after `CreateBuilder`:**
  - `builder.AddYaamTelemetry("yaam-gateway");`
  - `builder.AddYaamTelemetry("yaam-api");`
  - `builder.AddYaamTelemetry("yaam-reminders");`

  Add `using Yaam.ServiceDefaults.Telemetry;` to each.

- [ ] **Step 5: Propagate the trace through the relay.** In `OutboxRelay.PublishAsync`, before `BasicPublishAsync`:
```csharp
        using var activity = MessagingTelemetry.StartPublishActivity(message.TraceParent, message.Type);
        if (activity is not null)
            properties.Headers![MessagingTelemetry.TraceParentHeader] = activity.Id;
```

- [ ] **Step 6: Continue the trace in the consumer.** In `ApplicationEventsConsumer.OnReceivedAsync`, wrap the body:
```csharp
        using var activity = MessagingTelemetry.StartConsumeActivity(
            delivery.BasicProperties.Headers, delivery.BasicProperties.Type);
```
After `HandleAsync`, add `activity?.SetTag("yaam.consume.outcome", outcome.ToString());`. In the catch block, add `activity?.SetStatus(ActivityStatusCode.Error, exception.Message);`.

- [ ] **Step 7: Metrics**

`backend/Yaam.Infrastructure/Messaging/OutboxMetrics.cs`:
```csharp
using System.Diagnostics.Metrics;
using Yaam.ServiceDefaults.Telemetry;

namespace Yaam.Infrastructure.Messaging;

// The two numbers that tell you an outbox is healthy: how much is waiting, and for how long.
public sealed class OutboxMetrics
{
    private long _pending;
    private double _oldestPendingAgeSeconds;

    public OutboxMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MessagingTelemetry.SourceName);
        meter.CreateObservableGauge("yaam.outbox.pending", () => Interlocked.Read(ref _pending), unit: "{message}");
        meter.CreateObservableGauge("yaam.outbox.oldest_pending_age", () => Volatile.Read(ref _oldestPendingAgeSeconds), unit: "s");
    }

    public void Update(long pending, double oldestPendingAgeSeconds)
    {
        Interlocked.Exchange(ref _pending, pending);
        Volatile.Write(ref _oldestPendingAgeSeconds, oldestPendingAgeSeconds);
    }
}
```
Register it with `builder.Services.AddSingleton<OutboxMetrics>();` in the API `Program.cs`. Inject `OutboxMetrics metrics` into `OutboxRelay`'s constructor. At the end of each loop iteration in `ExecuteAsync` (inside the `try`, after `PublishPendingAsync`), call `await RecordBacklogAsync(stoppingToken);`:
```csharp
    private async Task RecordBacklogAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pending = db.OutboxMessages.Where(m => m.ProcessedAt == null);
        var count = await pending.LongCountAsync(cancellationToken);
        var oldest = await pending.MinAsync(m => (DateTime?)m.OccurredAt, cancellationToken);
        metrics.Update(count, oldest is null ? 0 : (DateTime.UtcNow - oldest.Value).TotalSeconds);
    }
```

`backend/Yaam.Reminders/ApplicationEvents/ConsumerMetrics.cs`:
```csharp
using System.Diagnostics.Metrics;
using Yaam.ServiceDefaults.Telemetry;

namespace Yaam.Reminders.ApplicationEvents;

public sealed class ConsumerMetrics
{
    private readonly Counter<long> _messages;

    public ConsumerMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MessagingTelemetry.SourceName);
        _messages = meter.CreateCounter<long>("yaam.consumer.messages", unit: "{message}");
    }

    public void Record(ConsumeOutcome outcome)
        => _messages.Add(1, new KeyValuePair<string, object?>("outcome", outcome.ToString().ToLowerInvariant()));
}
```
Register it with `builder.Services.AddSingleton<ConsumerMetrics>();` in the Reminders `Program.cs`. Inject it into `ApplicationEventsConsumer`. Call `metrics.Record(outcome);` after `HandleAsync`, and `metrics.Record(ConsumeOutcome.Failed);` in the catch block.

- [ ] **Step 8: Tests still green**

Run: `cd backend && dotnet build && dotnet test Yaam.Tests.Integration && dotnet test Yaam.Reminders.Tests && dotnet format --verify-no-changes`
Expected: all pass. With no OTLP listener in tests, the exporter fails silently.

- [ ] **Step 9: See the trace.** Run the whole stack. In the UI, rename an application. In Grafana → Explore → Tempo, search service `yaam-gateway`.
  - Expected: **one** trace with these spans:
    - gateway `PUT /api/applications/{id}`
    - → yaam-api `PUT` → Npgsql `UPDATE` + `INSERT OutboxMessages`
    - → `publish ApplicationUpserted` (yaam-api, a gap of up to about 1 s before it: the polling delay)
    - → `process ApplicationUpserted` (yaam-reminders) → Npgsql spans
  - If the gateway → monolith hop is missing, YARP's outgoing call isn't instrumented. Check the YARP docs for distributed tracing and note the result.
  - In Explore → Prometheus, query `yaam_outbox_pending` and `yaam_consumer_messages_total`. Use autocomplete; unit suffixes may be appended.

- [ ] **Step 10: Commit**

```bash
git add docker-compose.yml backend
git commit -m "feat(observability): trace requests across outbox and RabbitMQ with OpenTelemetry"
```

**CHECKPOINT 4:** Stop. Discuss with the user:
- the gap in the trace (polling latency vs LISTEN/NOTIFY or CDC);
- how they would alert on `oldest_pending_age`.

---

## Milestone 5 — Break it on purpose

### Task 12: Failure experiments 1–4 (runbook; the user drives, the agent assists)

**Files:**
- Create: `docs/architecture/experiments/2026-10-failure-experiments.md`

- [ ] **Step 1: Create the runbook file.** It has one section per experiment, each with the headings **Hypothesis / Steps / Observed / Explanation**. Pre-fill Hypothesis and Steps from below. The user fills in Observed and Explanation. That writing is the learning.

  1. **Consumer down.**
     - Steps:
       - stop Reminders (Ctrl+C);
       - rename three applications in the UI;
       - RabbitMQ UI → Queues → `reminders.application-events` shows Ready = 3;
       - Grafana shows `yaam_outbox_pending` back at 0;
       - restart Reminders.
     - Hypothesis: the queue drains to 0 and the snapshots match.
  2. **Duplicate delivery.**
     - Steps:
       - stop the monolith;
       - start it with `Outbox__SimulateCrashAfterPublish=true dotnet run --project backend/Yaam.API --launch-profile http`;
       - rename one application;
       - after 5 s, stop it and restart it normally.
     - Hypothesis: the same `MessageId` is delivered about 5 times; `yaam_consumer_messages_total{outcome="duplicate"}` rises; the snapshot is correct.
  3. **Reordering.**
     - Steps:
       - copy an application id;
       - RabbitMQ UI → Exchanges → `yaam.applications` → Publish message, with routing key `application.upserted` and payload `{"messageId":"<new guid>","type":"ApplicationUpserted","occurredAt":"2026-10-05T10:00:00Z","payload":{"applicationId":"<id>","version":1,"companyName":"STALE","role":"x","dateApplied":null,"contactName":null}}`.
     - Hypothesis: the outcome is `stale`, and the company name is unchanged.
  4. **Poison message.**
     - Steps: publish the body `not json` with routing key `application.upserted`.
     - Hypothesis: the error log repeats until the delivery limit, then the message sits in `reminders.application-events.dlq`, and the next valid message still flows.
     - Also count the attempts exactly, and explain why `x-delivery-limit: 5` gives that number.

- [ ] **Step 2: Run each experiment with the user.** Answer questions; don't fill in Observed for them.

- [ ] **Step 3: Commit**

```bash
git add docs/architecture/experiments
git commit -m "docs(architecture): failure experiment runbook and observations"
```

### Task 13: Two replicas → duplicate emails → atomic claim

**Files:**
- Modify: `backend/Yaam.Reminders/Program.cs` (dev-only trigger endpoint)
- Modify: `backend/Yaam.Reminders/Notifications/SendDueRemindersCommand.cs`
- Modify: `backend/Yaam.Reminders.Tests/RemindersApiFactory.cs`
- Modify: `backend/Yaam.Reminders.Tests/Integration/SendDueRemindersTests.cs`
- Modify: `backend/Yaam.Gateway/appsettings.json`
- Modify: `docs/architecture/experiments/2026-10-failure-experiments.md`

- [ ] **Step 1: Write the failing race test.**

Make the capturing sender slow, so the race window is wide and the test deterministic:
- In `CapturingEmailSender`, add the property `public TimeSpan Delay { get; set; } = TimeSpan.Zero;`.
- Change `SendAsync` to `async Task`. It does `await Task.Delay(Delay, cancellationToken);` before adding to the list.

Add to `SendDueRemindersTests`:
```csharp
    [Fact]
    public async Task TwoReplicasRunningConcurrently_SendEachReminderOnce()
    {
        var snapshot = RemindersApiFactory.NewSnapshot("Race Co");
        await factory.SeedSnapshotAsync(snapshot);
        await SeedDueReminderAsync(snapshot.ApplicationId);
        factory.EmailSender.Clear();
        factory.EmailSender.Delay = TimeSpan.FromMilliseconds(200);

        try
        {
            await Task.WhenAll(RunJobAsync(), RunJobAsync());
        }
        finally
        {
            factory.EmailSender.Delay = TimeSpan.Zero;
        }

        factory.EmailSender.Sent.Where(e => e.Subject.Contains("Race Co")).Should().ContainSingle();
    }
```
Run: `dotnet test Yaam.Reminders.Tests --filter TwoReplicasRunningConcurrently`
Expected: **FAIL**, with 2 emails. This is the race, reproduced in a test.

- [ ] **Step 2: Reproduce it live** (for the runbook).
  - In `Program.cs`, before `app.Run()`:
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/notifications/run", async (IMediator mediator, CancellationToken cancellationToken) =>
    {
        await mediator.Send(
            new SendDueRemindersCommand(),
            cancellationToken);
        return Results.NoContent();
    });
}
```
  - Start a second replica: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/Yaam.Reminders --no-launch-profile --urls http://localhost:5241`.
  - Add the destination `"reminders-2": { "Address": "http://localhost:5241" }` and `"LoadBalancingPolicy": "RoundRobin"` to the gateway's `reminders` cluster.
  - Make the reminders due: `docker compose exec postgres psql -U yaam -d yaam_reminders -c 'UPDATE "Reminders" SET "DueDate" = CURRENT_DATE, "NotifiedAt" = NULL;'`
  - Fire both at once: `curl -X POST localhost:5240/dev/notifications/run & curl -X POST localhost:5241/dev/notifications/run & wait`
  - Expected: Mailpit (`http://localhost:8025`) shows two emails per reminder.
  - Also note in the runbook: both replicas now consume the same queue (competing consumers), so per-application ordering across them is no longer guaranteed. This is why the `Version` check exists.

- [ ] **Step 3: Fix with an atomic claim.** In `SendDueRemindersCommandHandler.Handle`, replace the `foreach` body:
```csharp
        foreach (var reminder in dueReminders)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            // Conditional UPDATE is the lock: exactly one replica sees "1 row affected" and owns this reminder.
            var claimed = await db.Reminders
                .Where(r => r.Id == reminder.Id && r.NotifiedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.NotifiedAt, now), cancellationToken);
            if (claimed == 0)
                continue;

            try
            {
                await emailSender.SendAsync(
                    emailOptions.Value.NotificationEmail,
                    $"Follow-up reminder: {reminder.Application.CompanyName} — {reminder.Application.Role}",
                    BuildEmailBody(reminder),
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Release the claim so the next run retries: at-least-once for failures we observe.
                // A crash between claim and send still loses this email (at-most-once for crashes).
                await db.Reminders
                    .Where(r => r.Id == reminder.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.NotifiedAt, (DateTime?)null), cancellationToken);
                logger.LogError(exception, "Failed to send reminder {ReminderId}", reminder.Id);
            }
        }
```

- [ ] **Step 4: Run the tests**

Run: `dotnet test Yaam.Reminders.Tests`
Expected: all PASS, including the race test and the earlier "not again on next run" test.

- [ ] **Step 5: Re-run the live reproduction.** Expected: one email per reminder. Record in the runbook:
  - the at-most-once vs at-least-once trade-off for side effects;
  - the alternatives: Postgres advisory lock or a single-leader job (Kubernetes CronJob / Hangfire), and their trade-offs.

- [ ] **Step 6: Commit**

```bash
git add backend docs/architecture/experiments
git commit -m "fix(reminders): claim due reminders atomically so replicas never double-send"
```

**CHECKPOINT 5:** Stop. Review the runbook together.

---

## Milestone 6 — Interview story

### Task 14: Interview notes (written by the user, reviewed by the agent)

**Files:**
- Create: `docs/architecture/microservices-interview-notes.md`

- [ ] **Step 1: Create the skeleton** with these headings only:
  1. The 60-second architecture pitch
  2. Why extract Reminders (and why I'd usually start with a modular monolith)
  3. One message's life and every crash point
  4. Exactly-once is a lie: at-least-once + idempotency + versions
  5. The race I reproduced and fixed
  6. What I'd do next (rich `Reminder` domain model as the prerequisite for domain events, library outbox, Kubernetes probes, `UserId` through contracts, inbox retention, contract tests)
  7. Questions I'd ask the team

- [ ] **Step 2: The user writes the answers.** Then review them for technical accuracy only, and point out gaps as questions rather than rewrites.

- [ ] **Step 3: Commit**

```bash
git add docs/architecture/microservices-interview-notes.md
git commit -m "docs(architecture): microservices interview notes"
```
