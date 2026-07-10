# Application Tracking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full-stack application tracking (Stories 1–4 and 6) on the `feat/application-tracking` branch: .NET backend with CQRS, REST API, and Angular 22 frontend with daisyUI + Tailwind, connected via an auto-generated OpenAPI client.

**Architecture:** Clean Architecture backend (Domain → Application → Infrastructure → API), CQRS with MediatR, EF Core + PostgreSQL. Angular 22 standalone components, `resource()` + `firstValueFrom()` for async data, `toSignal()` for reactive route params. API client generated from the running backend's Swagger endpoint.

**Tech Stack:** .NET 10, MediatR 12, FluentValidation 11, EF Core 9 + Npgsql, xUnit, FluentAssertions, Angular 22, openapi-generator-cli 7, Tailwind CSS v3, daisyUI v3.

## Global Constraints

- Branch: `feat/application-tracking` — never commit directly to `main`
- .NET target: `net10.0`; Nullable enabled; ImplicitUsings enabled
- No AutoMapper — map explicitly in each handler (private static `ToDto` method)
- No `IQueryable` exposed from repositories
- Naming: Commands = `VerbNounCommand`, Queries = `GetNounByXQuery`, Handlers = same + `Handler`
- Angular: `resource()` (not `rxResource`), `firstValueFrom()` bridges Observable→Promise, `toSignal()` for route params
- Comments only for non-obvious WHY — no descriptive comments on self-evident code
- Enums serialized as strings in JSON (backend and OpenAPI spec)
- Story 5 (CV linking) and reminders cascade delete are out of scope

---

## File Map

**New backend files:**
```
backend/Yaam.Domain/Entities/Application.cs
backend/Yaam.Domain/Entities/ApplicationNote.cs
backend/Yaam.Domain/Enums/ApplicationStatus.cs
backend/Yaam.Domain/Repositories/IApplicationRepository.cs
backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationConfiguration.cs
backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationNoteConfiguration.cs
backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs
backend/Yaam.Application/Applications/Dtos/ApplicationSummaryDto.cs
backend/Yaam.Application/Applications/Dtos/ApplicationDto.cs
backend/Yaam.Application/Applications/Dtos/ApplicationNoteDto.cs
backend/Yaam.Application/Applications/Queries/GetApplicationsQuery.cs
backend/Yaam.Application/Applications/Queries/GetApplicationByIdQuery.cs
backend/Yaam.Application/Applications/Commands/CreateApplicationCommand.cs
backend/Yaam.Application/Applications/Commands/UpdateApplicationCommand.cs
backend/Yaam.Application/Applications/Commands/UpdateApplicationStatusCommand.cs
backend/Yaam.Application/Applications/Commands/DeleteApplicationCommand.cs
backend/Yaam.Application/Applications/Commands/Notes/AddApplicationNoteCommand.cs
backend/Yaam.Application/Applications/Commands/Notes/UpdateApplicationNoteCommand.cs
backend/Yaam.Application/Applications/Commands/Notes/DeleteApplicationNoteCommand.cs
backend/Yaam.API/Controllers/ApplicationsController.cs
backend/Yaam.API/Controllers/ApplicationNotesController.cs
backend/Yaam.Tests.Unit/Applications/CreateApplicationCommandValidatorTests.cs
backend/Yaam.Tests.Unit/Applications/UpdateApplicationCommandValidatorTests.cs
backend/Yaam.Tests.Integration/ApiFactory.cs
backend/Yaam.Tests.Integration/Applications/ApplicationsEndpointsTests.cs
```

**Modified backend files:**
```
backend/Yaam.Infrastructure/Persistence/AppDbContext.cs       (add DbSets)
backend/Yaam.Infrastructure/DependencyInjection.cs            (register repository)
backend/Yaam.API/Program.cs                                   (JSON enum + Swagger annotations)
backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj  (add packages)
```

**New frontend files:**
```
frontend/openapitools.json
frontend/tailwind.config.js
frontend/postcss.config.js
frontend/src/app/core/interceptors/problem-details.interceptor.ts
frontend/src/app/features/applications/models/application.model.ts
frontend/src/app/features/applications/models/application-note.model.ts
frontend/src/app/features/applications/pages/application-list/application-list.component.ts
frontend/src/app/features/applications/pages/application-list/application-list.component.html
frontend/src/app/features/applications/pages/application-detail/application-detail.component.ts
frontend/src/app/features/applications/pages/application-detail/application-detail.component.html
frontend/src/app/features/applications/components/application-form/application-form.component.ts
frontend/src/app/features/applications/components/application-form/application-form.component.html
frontend/src/app/features/applications/components/application-notes/application-notes.component.ts
frontend/src/app/features/applications/components/application-notes/application-notes.component.html
.run/Yaam API.run.xml
.run/Frontend Dev Server.run.xml
```

**Modified frontend files:**
```
frontend/src/styles.scss
frontend/src/app/app.config.ts
frontend/src/app/features/applications/applications.routes.ts
frontend/package.json
.gitignore
```

---

## Task 1: Branch and IDE run configs

**Files:**
- Create: `.run/Yaam API.run.xml`
- Create: `.run/Frontend Dev Server.run.xml`

- [ ] **Step 1: Create the feature branch**

```bash
git checkout -b feat/application-tracking
```

Expected: `Switched to a new branch 'feat/application-tracking'`

- [ ] **Step 2: Create Rider run config for the backend API**

Create `.run/Yaam API.run.xml`:

```xml
<component name="ProjectRunConfigurationManager">
  <configuration default="false" name="Yaam API" type="DotNetProject" factoryName=".NET Project">
    <option name="PROJECT_PATH" value="$PROJECT_DIR$/backend/Yaam.API/Yaam.API.csproj" />
    <option name="PROJECT_EFT" value="net10.0" />
    <option name="PASS_PARENT_ENVS" value="1" />
    <method v="2" />
  </configuration>
</component>
```

- [ ] **Step 3: Create WebStorm/Rider run config for the frontend dev server**

Create `.run/Frontend Dev Server.run.xml`:

```xml
<component name="ProjectRunConfigurationManager">
  <configuration default="false" name="Frontend Dev Server" type="js.build_tools.npm" factoryName="npm">
    <package-json value="$PROJECT_DIR$/frontend/package.json" />
    <command value="run" />
    <scripts>
      <script value="start" />
    </scripts>
    <node-interpreter value="project" />
    <envs />
    <method v="2" />
  </configuration>
</component>
```

- [ ] **Step 4: Commit**

```bash
git add .run/
git commit -m "chore: add Rider and WebStorm run configurations"
```

---

## Task 2: Domain layer

**Files:**
- Create: `backend/Yaam.Domain/Entities/Application.cs`
- Create: `backend/Yaam.Domain/Entities/ApplicationNote.cs`
- Create: `backend/Yaam.Domain/Enums/ApplicationStatus.cs`
- Create: `backend/Yaam.Domain/Repositories/IApplicationRepository.cs`

**Interfaces:**
- Produces: `Application`, `ApplicationNote`, `ApplicationStatus`, `IApplicationRepository` — consumed by Tasks 3–7

- [ ] **Step 1: Create ApplicationStatus enum**

Create `backend/Yaam.Domain/Enums/ApplicationStatus.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Yaam.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationStatus
{
    Draft,
    Applied,
    InterviewScheduled,
    Interviewed,
    OfferReceived,
    Accepted,
    Rejected,
    Withdrawn
}
```

- [ ] **Step 2: Create Application entity**

Create `backend/Yaam.Domain/Entities/Application.cs`:

```csharp
using Yaam.Domain.Enums;

namespace Yaam.Domain.Entities;

public class Application : Entity
{
    public string CompanyName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateOnly? DateApplied { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? JobPosting { get; set; }
    public ICollection<ApplicationNote> Notes { get; set; } = new List<ApplicationNote>();
}
```

- [ ] **Step 3: Create ApplicationNote entity**

Create `backend/Yaam.Domain/Entities/ApplicationNote.cs`:

```csharp
namespace Yaam.Domain.Entities;

public class ApplicationNote : Entity
{
    public Guid ApplicationId { get; set; }
    public string Body { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Create IApplicationRepository**

Create `backend/Yaam.Domain/Repositories/IApplicationRepository.cs`:

```csharp
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;

namespace Yaam.Domain.Repositories;

public interface IApplicationRepository
{
    Task<List<Application>> GetAllAsync(ApplicationStatus? status, string sort, string order, CancellationToken ct);
    Task<Application?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Application> AddAsync(Application application, CancellationToken ct);
    Task UpdateAsync(Application application, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<ApplicationNote?> GetNoteByIdAsync(Guid applicationId, Guid noteId, CancellationToken ct);
    Task<ApplicationNote> AddNoteAsync(ApplicationNote note, CancellationToken ct);
    Task UpdateNoteAsync(ApplicationNote note, CancellationToken ct);
    Task DeleteNoteAsync(Guid applicationId, Guid noteId, CancellationToken ct);
}
```

- [ ] **Step 5: Verify domain builds**

```bash
cd /path/to/yaam/backend
dotnet build Yaam.Domain
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add backend/Yaam.Domain/
git commit -m "feat: add Application and ApplicationNote domain entities"
```

---

## Task 3: Infrastructure layer

**Files:**
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationNoteConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs`
- Modify: `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `backend/Yaam.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `Application`, `ApplicationNote`, `ApplicationStatus`, `IApplicationRepository` from Task 2
- Produces: `ApplicationRepository` registered as `IApplicationRepository` in DI

- [ ] **Step 1: Create EF configuration for Application**

Create `backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Role).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Status).IsRequired().HasConversion<string>();
        builder.Property(a => a.ContactName).HasMaxLength(200);
        builder.Property(a => a.ContactEmail).HasMaxLength(200);
        builder.Property(a => a.ContactPhone).HasMaxLength(50);

        builder.HasMany(a => a.Notes)
            .WithOne()
            .HasForeignKey(n => n.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Create EF configuration for ApplicationNote**

Create `backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationNoteConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ApplicationNoteConfiguration : IEntityTypeConfiguration<ApplicationNote>
{
    public void Configure(EntityTypeBuilder<ApplicationNote> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Body).IsRequired();
        builder.Property(n => n.ApplicationId).IsRequired();
    }
}
```

- [ ] **Step 3: Add DbSets to AppDbContext**

Modify `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs` — add two DbSet properties after the class opening brace:

```csharp
using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationNote> ApplicationNotes => Set<ApplicationNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Create ApplicationRepository**

Create `backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;

namespace Yaam.Infrastructure.Repositories;

public class ApplicationRepository(AppDbContext db) : IApplicationRepository
{
    public async Task<List<Application>> GetAllAsync(
        ApplicationStatus? status, string sort, string order, CancellationToken ct)
    {
        var query = db.Applications.AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        query = (sort.ToLower(), order.ToLower()) switch
        {
            ("companyname", "asc")  => query.OrderBy(a => a.CompanyName),
            ("companyname", _)      => query.OrderByDescending(a => a.CompanyName),
            ("status", "asc")       => query.OrderBy(a => a.Status),
            ("status", _)           => query.OrderByDescending(a => a.Status),
            (_, "asc")              => query.OrderBy(a => a.DateApplied),
            _                       => query.OrderByDescending(a => a.DateApplied),
        };

        return await query.ToListAsync(ct);
    }

    public async Task<Application?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Applications
            .Include(a => a.Notes.OrderByDescending(n => n.CreatedAt))
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Application> AddAsync(Application application, CancellationToken ct)
    {
        db.Applications.Add(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    public async Task UpdateAsync(Application application, CancellationToken ct)
    {
        db.Applications.Update(application);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var application = await db.Applications.FindAsync([id], ct);
        if (application is not null)
        {
            db.Applications.Remove(application);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<ApplicationNote?> GetNoteByIdAsync(Guid applicationId, Guid noteId, CancellationToken ct)
        => await db.ApplicationNotes
            .FirstOrDefaultAsync(n => n.ApplicationId == applicationId && n.Id == noteId, ct);

    public async Task<ApplicationNote> AddNoteAsync(ApplicationNote note, CancellationToken ct)
    {
        db.ApplicationNotes.Add(note);
        await db.SaveChangesAsync(ct);
        return note;
    }

    public async Task UpdateNoteAsync(ApplicationNote note, CancellationToken ct)
    {
        db.ApplicationNotes.Update(note);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteNoteAsync(Guid applicationId, Guid noteId, CancellationToken ct)
    {
        var note = await db.ApplicationNotes
            .FirstOrDefaultAsync(n => n.ApplicationId == applicationId && n.Id == noteId, ct);
        if (note is not null)
        {
            db.ApplicationNotes.Remove(note);
            await db.SaveChangesAsync(ct);
        }
    }
}
```

- [ ] **Step 5: Register repository in DI**

Replace `backend/Yaam.Infrastructure/DependencyInjection.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;
using Yaam.Infrastructure.Repositories;

namespace Yaam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationRepository, ApplicationRepository>();

        return services;
    }
}
```

- [ ] **Step 6: Verify build**

```bash
cd /path/to/yaam/backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Create EF Core migration**

Ensure PostgreSQL is running (`docker compose up -d postgres`), then:

```bash
cd /path/to/yaam/backend
dotnet ef migrations add AddApplications \
  --project Yaam.Infrastructure \
  --startup-project Yaam.API
dotnet ef database update \
  --project Yaam.Infrastructure \
  --startup-project Yaam.API
```

Expected: migration files created in `Yaam.Infrastructure/Migrations/`, database updated.

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.Infrastructure/ backend/Yaam.Domain/
git commit -m "feat: add EF Core mapping and repository for Application and ApplicationNote"
```

---

## Task 4: Application layer — DTOs and queries

**Files:**
- Create: `backend/Yaam.Application/Applications/Dtos/ApplicationSummaryDto.cs`
- Create: `backend/Yaam.Application/Applications/Dtos/ApplicationDto.cs`
- Create: `backend/Yaam.Application/Applications/Dtos/ApplicationNoteDto.cs`
- Create: `backend/Yaam.Application/Applications/Queries/GetApplicationsQuery.cs`
- Create: `backend/Yaam.Application/Applications/Queries/GetApplicationByIdQuery.cs`
- Test: `backend/Yaam.Tests.Unit/Applications/GetApplicationsQueryTests.cs`

**Interfaces:**
- Consumes: `IApplicationRepository` from Task 2
- Produces: `GetApplicationsQuery → List<ApplicationSummaryDto>`, `GetApplicationByIdQuery → ApplicationDto?`

- [ ] **Step 1: Create DTOs**

Create `backend/Yaam.Application/Applications/Dtos/ApplicationNoteDto.cs`:

```csharp
namespace Yaam.Application.Applications.Dtos;

public record ApplicationNoteDto(
    Guid Id,
    string Body,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

Create `backend/Yaam.Application/Applications/Dtos/ApplicationSummaryDto.cs`:

```csharp
using Yaam.Domain.Enums;

namespace Yaam.Application.Applications.Dtos;

public record ApplicationSummaryDto(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status);
```

Create `backend/Yaam.Application/Applications/Dtos/ApplicationDto.cs`:

```csharp
using Yaam.Domain.Enums;

namespace Yaam.Application.Applications.Dtos;

public record ApplicationDto(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ApplicationNoteDto> Notes);
```

- [ ] **Step 2: Write the failing query handler tests**

Create `backend/Yaam.Tests.Unit/Applications/GetApplicationsQueryTests.cs`:

```csharp
using FluentAssertions;
using MediatR;
using NSubstitute;
using Yaam.Application.Applications.Dtos;
using Yaam.Application.Applications.Queries;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;

namespace Yaam.Tests.Unit.Applications;

public class GetApplicationsQueryTests
{
    private readonly IApplicationRepository _repo = Substitute.For<IApplicationRepository>();

    [Fact]
    public async Task Handle_ReturnsAllApplicationsAsSummaryDtos()
    {
        var applications = new List<Application>
        {
            new() { CompanyName = "Acme", Role = "Dev", Status = ApplicationStatus.Applied },
            new() { CompanyName = "Beta", Role = "QA",  Status = ApplicationStatus.Draft  },
        };
        _repo.GetAllAsync(null, "dateApplied", "desc", Arg.Any<CancellationToken>())
            .Returns(applications);

        var handler = new GetApplicationsQueryHandler(_repo);
        var result = await handler.Handle(
            new GetApplicationsQuery(null, "dateApplied", "desc"), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task Handle_PassesFilterAndSortToRepository()
    {
        _repo.GetAllAsync(ApplicationStatus.Applied, "companyName", "asc", Arg.Any<CancellationToken>())
            .Returns(new List<Application>());

        var handler = new GetApplicationsQueryHandler(_repo);
        await handler.Handle(
            new GetApplicationsQuery(ApplicationStatus.Applied, "companyName", "asc"),
            CancellationToken.None);

        await _repo.Received(1)
            .GetAllAsync(ApplicationStatus.Applied, "companyName", "asc", Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 3: Run tests to confirm they fail**

```bash
cd /path/to/yaam/backend
dotnet test Yaam.Tests.Unit --filter "GetApplicationsQueryTests"
```

Expected: FAIL — `GetApplicationsQueryHandler` does not exist yet.

- [ ] **Step 4: Create GetApplicationsQuery with handler**

Create `backend/Yaam.Application/Applications/Queries/GetApplicationsQuery.cs`:

```csharp
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Queries;

public record GetApplicationsQuery(
    ApplicationStatus? Status,
    string Sort = "dateApplied",
    string Order = "desc") : IRequest<List<ApplicationSummaryDto>>;

public class GetApplicationsQueryHandler(IApplicationRepository repository)
    : IRequestHandler<GetApplicationsQuery, List<ApplicationSummaryDto>>
{
    public async Task<List<ApplicationSummaryDto>> Handle(
        GetApplicationsQuery request, CancellationToken cancellationToken)
    {
        var applications = await repository.GetAllAsync(
            request.Status, request.Sort, request.Order, cancellationToken);
        return applications.Select(ToDto).ToList();
    }

    private static ApplicationSummaryDto ToDto(Application a) =>
        new(a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status);
}
```

- [ ] **Step 5: Create GetApplicationByIdQuery with handler**

Create `backend/Yaam.Application/Applications/Queries/GetApplicationByIdQuery.cs`:

```csharp
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Queries;

public record GetApplicationByIdQuery(Guid Id) : IRequest<ApplicationDto?>;

public class GetApplicationByIdQueryHandler(IApplicationRepository repository)
    : IRequestHandler<GetApplicationByIdQuery, ApplicationDto?>
{
    public async Task<ApplicationDto?> Handle(
        GetApplicationByIdQuery request, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(request.Id, cancellationToken);
        return application is null ? null : ToDto(application);
    }

    private static ApplicationDto ToDto(Application a) => new(
        a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status,
        a.ContactName, a.ContactEmail, a.ContactPhone, a.JobPosting,
        a.CreatedAt, a.UpdatedAt,
        a.Notes.OrderByDescending(n => n.CreatedAt).Select(ToNoteDto).ToList());

    private static ApplicationNoteDto ToNoteDto(ApplicationNote n) =>
        new(n.Id, n.Body, n.CreatedAt, n.UpdatedAt);
}
```

- [ ] **Step 6: Run tests to confirm they pass**

```bash
cd /path/to/yaam/backend
dotnet test Yaam.Tests.Unit --filter "GetApplicationsQueryTests"
```

Expected: PASS — 2 tests passing.

- [ ] **Step 7: Verify full build**

```bash
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.Application/ backend/Yaam.Tests.Unit/
git commit -m "feat: add application DTOs and query handlers"
```

---

## Task 5: Application commands — CRUD

**Files:**
- Create: `backend/Yaam.Application/Applications/Commands/CreateApplicationCommand.cs`
- Create: `backend/Yaam.Application/Applications/Commands/UpdateApplicationCommand.cs`
- Create: `backend/Yaam.Application/Applications/Commands/UpdateApplicationStatusCommand.cs`
- Create: `backend/Yaam.Application/Applications/Commands/DeleteApplicationCommand.cs`
- Test: `backend/Yaam.Tests.Unit/Applications/CreateApplicationCommandValidatorTests.cs`
- Test: `backend/Yaam.Tests.Unit/Applications/UpdateApplicationCommandValidatorTests.cs`

**Interfaces:**
- Consumes: `IApplicationRepository`, `ApplicationDto` from Tasks 2 and 4
- Produces: four command handlers registered via MediatR assembly scanning

- [ ] **Step 1: Write failing validator tests**

Create `backend/Yaam.Tests.Unit/Applications/CreateApplicationCommandValidatorTests.cs`:

```csharp
using FluentAssertions;
using FluentValidation.TestHelper;
using Yaam.Application.Applications.Commands;
using Yaam.Domain.Enums;

namespace Yaam.Tests.Unit.Applications;

public class CreateApplicationCommandValidatorTests
{
    private readonly CreateApplicationCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_CompanyName_Empty()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("", "Dev", null, ApplicationStatus.Draft,
                null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void Should_Fail_When_Role_Empty()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("Acme", "", null, ApplicationStatus.Draft,
                null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Should_Fail_When_DateApplied_Null_And_Status_Not_Draft()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("Acme", "Dev", null, ApplicationStatus.Applied,
                null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.DateApplied);
    }

    [Fact]
    public void Should_Pass_When_DateApplied_Null_And_Status_Draft()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("Acme", "Dev", null, ApplicationStatus.Draft,
                null, null, null, null));
        result.ShouldNotHaveValidationErrorFor(x => x.DateApplied);
    }

    [Fact]
    public void Should_Pass_With_All_Required_Fields_For_Applied_Status()
    {
        var result = _validator.TestValidate(
            new CreateApplicationCommand("Acme", "Dev", DateOnly.FromDateTime(DateTime.Today),
                ApplicationStatus.Applied, null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

Create `backend/Yaam.Tests.Unit/Applications/UpdateApplicationCommandValidatorTests.cs`:

```csharp
using FluentAssertions;
using FluentValidation.TestHelper;
using Yaam.Application.Applications.Commands;
using Yaam.Domain.Enums;

namespace Yaam.Tests.Unit.Applications;

public class UpdateApplicationCommandValidatorTests
{
    private readonly UpdateApplicationCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_CompanyName_Empty()
    {
        var result = _validator.TestValidate(
            new UpdateApplicationCommand(Guid.NewGuid(), "", "Dev", null,
                ApplicationStatus.Draft, null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void Should_Fail_When_DateApplied_Null_And_Status_Not_Draft()
    {
        var result = _validator.TestValidate(
            new UpdateApplicationCommand(Guid.NewGuid(), "Acme", "Dev", null,
                ApplicationStatus.Interviewed, null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.DateApplied);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd /path/to/yaam/backend
dotnet test Yaam.Tests.Unit --filter "ApplicationCommandValidatorTests"
```

Expected: FAIL — command types do not exist yet.

- [ ] **Step 3: Create CreateApplicationCommand**

Create `backend/Yaam.Application/Applications/Commands/CreateApplicationCommand.cs`:

```csharp
using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands;

public record CreateApplicationCommand(
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting) : IRequest<ApplicationDto>;

public class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateApplied)
            .NotNull()
            .When(x => x.Status != ApplicationStatus.Draft)
            .WithMessage("Date applied is required unless status is Draft.");
    }
}

public class CreateApplicationCommandHandler(IApplicationRepository repository)
    : IRequestHandler<CreateApplicationCommand, ApplicationDto>
{
    public async Task<ApplicationDto> Handle(
        CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = new Application
        {
            CompanyName = request.CompanyName,
            Role = request.Role,
            DateApplied = request.DateApplied,
            Status = request.Status,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            JobPosting = request.JobPosting,
        };

        await repository.AddAsync(application, cancellationToken);

        return new ApplicationDto(
            application.Id, application.CompanyName, application.Role,
            application.DateApplied, application.Status,
            application.ContactName, application.ContactEmail, application.ContactPhone,
            application.JobPosting, application.CreatedAt, application.UpdatedAt, []);
    }
}
```

- [ ] **Step 4: Create UpdateApplicationCommand**

Create `backend/Yaam.Application/Applications/Commands/UpdateApplicationCommand.cs`:

```csharp
using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands;

public record UpdateApplicationCommand(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting) : IRequest<ApplicationDto?>;

public class UpdateApplicationCommandValidator : AbstractValidator<UpdateApplicationCommand>
{
    public UpdateApplicationCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DateApplied)
            .NotNull()
            .When(x => x.Status != ApplicationStatus.Draft)
            .WithMessage("Date applied is required unless status is Draft.");
    }
}

public class UpdateApplicationCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationCommand, ApplicationDto?>
{
    public async Task<ApplicationDto?> Handle(
        UpdateApplicationCommand request, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (application is null) return null;

        application.CompanyName = request.CompanyName;
        application.Role = request.Role;
        application.DateApplied = request.DateApplied;
        application.Status = request.Status;
        application.ContactName = request.ContactName;
        application.ContactEmail = request.ContactEmail;
        application.ContactPhone = request.ContactPhone;
        application.JobPosting = request.JobPosting;

        await repository.UpdateAsync(application, cancellationToken);

        return new ApplicationDto(
            application.Id, application.CompanyName, application.Role,
            application.DateApplied, application.Status,
            application.ContactName, application.ContactEmail, application.ContactPhone,
            application.JobPosting, application.CreatedAt, application.UpdatedAt,
            application.Notes.OrderByDescending(n => n.CreatedAt)
                .Select(n => new ApplicationNoteDto(n.Id, n.Body, n.CreatedAt, n.UpdatedAt))
                .ToList());
    }
}
```

- [ ] **Step 5: Create UpdateApplicationStatusCommand**

Create `backend/Yaam.Application/Applications/Commands/UpdateApplicationStatusCommand.cs`:

```csharp
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands;

public record UpdateApplicationStatusCommand(
    Guid Id,
    ApplicationStatus Status) : IRequest<ApplicationDto?>;

public class UpdateApplicationStatusCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationStatusCommand, ApplicationDto?>
{
    public async Task<ApplicationDto?> Handle(
        UpdateApplicationStatusCommand request, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (application is null) return null;

        application.Status = request.Status;
        await repository.UpdateAsync(application, cancellationToken);

        return new ApplicationDto(
            application.Id, application.CompanyName, application.Role,
            application.DateApplied, application.Status,
            application.ContactName, application.ContactEmail, application.ContactPhone,
            application.JobPosting, application.CreatedAt, application.UpdatedAt,
            application.Notes.OrderByDescending(n => n.CreatedAt)
                .Select(n => new ApplicationNoteDto(n.Id, n.Body, n.CreatedAt, n.UpdatedAt))
                .ToList());
    }
}
```

- [ ] **Step 6: Create DeleteApplicationCommand**

Create `backend/Yaam.Application/Applications/Commands/DeleteApplicationCommand.cs`:

```csharp
using MediatR;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands;

public record DeleteApplicationCommand(Guid Id) : IRequest;

public class DeleteApplicationCommandHandler(IApplicationRepository repository)
    : IRequestHandler<DeleteApplicationCommand>
{
    public async Task Handle(DeleteApplicationCommand request, CancellationToken cancellationToken)
        => await repository.DeleteAsync(request.Id, cancellationToken);
}
```

- [ ] **Step 7: Run validator tests**

```bash
cd /path/to/yaam/backend
dotnet test Yaam.Tests.Unit --filter "ApplicationCommandValidatorTests"
```

Expected: PASS — 7 tests passing.

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.Application/ backend/Yaam.Tests.Unit/
git commit -m "feat: add application CRUD command handlers and validators"
```

---

## Task 6: Application commands — notes

**Files:**
- Create: `backend/Yaam.Application/Applications/Commands/Notes/AddApplicationNoteCommand.cs`
- Create: `backend/Yaam.Application/Applications/Commands/Notes/UpdateApplicationNoteCommand.cs`
- Create: `backend/Yaam.Application/Applications/Commands/Notes/DeleteApplicationNoteCommand.cs`
- Test: `backend/Yaam.Tests.Unit/Applications/ApplicationNoteCommandValidatorTests.cs`

**Interfaces:**
- Consumes: `IApplicationRepository`, `ApplicationNoteDto` from Tasks 2 and 4
- Produces: three note command handlers

- [ ] **Step 1: Write failing validator tests**

Create `backend/Yaam.Tests.Unit/Applications/ApplicationNoteCommandValidatorTests.cs`:

```csharp
using FluentValidation.TestHelper;
using Yaam.Application.Applications.Commands.Notes;

namespace Yaam.Tests.Unit.Applications;

public class ApplicationNoteCommandValidatorTests
{
    [Fact]
    public void AddNote_Should_Fail_When_Body_Empty()
    {
        var validator = new AddApplicationNoteCommandValidator();
        var result = validator.TestValidate(
            new AddApplicationNoteCommand(Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void AddNote_Should_Pass_With_Valid_Body()
    {
        var validator = new AddApplicationNoteCommandValidator();
        var result = validator.TestValidate(
            new AddApplicationNoteCommand(Guid.NewGuid(), "Had a call with recruiter."));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateNote_Should_Fail_When_Body_Empty()
    {
        var validator = new UpdateApplicationNoteCommandValidator();
        var result = validator.TestValidate(
            new UpdateApplicationNoteCommand(Guid.NewGuid(), Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd /path/to/yaam/backend
dotnet test Yaam.Tests.Unit --filter "ApplicationNoteCommandValidatorTests"
```

Expected: FAIL — command types do not exist yet.

- [ ] **Step 3: Create AddApplicationNoteCommand**

Create `backend/Yaam.Application/Applications/Commands/Notes/AddApplicationNoteCommand.cs`:

```csharp
using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands.Notes;

public record AddApplicationNoteCommand(
    Guid ApplicationId,
    string Body) : IRequest<ApplicationNoteDto?>;

public class AddApplicationNoteCommandValidator : AbstractValidator<AddApplicationNoteCommand>
{
    public AddApplicationNoteCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty();
    }
}

public class AddApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<AddApplicationNoteCommand, ApplicationNoteDto?>
{
    public async Task<ApplicationNoteDto?> Handle(
        AddApplicationNoteCommand request, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application is null) return null;

        var note = new ApplicationNote
        {
            ApplicationId = request.ApplicationId,
            Body = request.Body,
        };

        await repository.AddNoteAsync(note, cancellationToken);
        return new ApplicationNoteDto(note.Id, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
```

- [ ] **Step 4: Create UpdateApplicationNoteCommand**

Create `backend/Yaam.Application/Applications/Commands/Notes/UpdateApplicationNoteCommand.cs`:

```csharp
using FluentValidation;
using MediatR;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands.Notes;

public record UpdateApplicationNoteCommand(
    Guid ApplicationId,
    Guid NoteId,
    string Body) : IRequest<ApplicationNoteDto?>;

public class UpdateApplicationNoteCommandValidator : AbstractValidator<UpdateApplicationNoteCommand>
{
    public UpdateApplicationNoteCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty();
    }
}

public class UpdateApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationNoteCommand, ApplicationNoteDto?>
{
    public async Task<ApplicationNoteDto?> Handle(
        UpdateApplicationNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await repository.GetNoteByIdAsync(
            request.ApplicationId, request.NoteId, cancellationToken);
        if (note is null) return null;

        note.Body = request.Body;
        await repository.UpdateNoteAsync(note, cancellationToken);
        return new ApplicationNoteDto(note.Id, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
```

- [ ] **Step 5: Create DeleteApplicationNoteCommand**

Create `backend/Yaam.Application/Applications/Commands/Notes/DeleteApplicationNoteCommand.cs`:

```csharp
using MediatR;
using Yaam.Domain.Repositories;

namespace Yaam.Application.Applications.Commands.Notes;

public record DeleteApplicationNoteCommand(
    Guid ApplicationId,
    Guid NoteId) : IRequest;

public class DeleteApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<DeleteApplicationNoteCommand>
{
    public async Task Handle(
        DeleteApplicationNoteCommand request, CancellationToken cancellationToken)
        => await repository.DeleteNoteAsync(request.ApplicationId, request.NoteId, cancellationToken);
}
```

- [ ] **Step 6: Run all unit tests**

```bash
cd /path/to/yaam/backend
dotnet test Yaam.Tests.Unit
```

Expected: PASS — all tests passing.

- [ ] **Step 7: Commit**

```bash
git add backend/Yaam.Application/ backend/Yaam.Tests.Unit/
git commit -m "feat: add application note command handlers and validators"
```

---

## Task 7: API controllers

**Files:**
- Create: `backend/Yaam.API/Controllers/ApplicationsController.cs`
- Create: `backend/Yaam.API/Controllers/ApplicationNotesController.cs`
- Modify: `backend/Yaam.API/Program.cs`

**Interfaces:**
- Consumes: all commands and queries from Tasks 4–6
- Produces: REST endpoints at `/api/applications` and `/api/applications/{id}/notes`

- [ ] **Step 1: Install Swashbuckle annotations package**

```bash
cd /path/to/yaam/backend
dotnet add Yaam.API/Yaam.API.csproj package Swashbuckle.AspNetCore.Annotations --version 6.*
```

- [ ] **Step 2: Update Program.cs**

Replace `backend/Yaam.API/Program.cs`:

```csharp
using System.Text.Json.Serialization;
using Yaam.API;
using Yaam.Application;
using Yaam.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required."));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "YAAM API", Version = "v1" });
    c.EnableAnnotations();
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("Angular");
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
```

- [ ] **Step 3: Create ApplicationsController**

Create `backend/Yaam.API/Controllers/ApplicationsController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Yaam.Application.Applications.Commands;
using Yaam.Application.Applications.Queries;
using Yaam.Domain.Enums;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(OperationId = "ListApplications")]
    public async Task<IActionResult> GetAll(
        [FromQuery] ApplicationStatus? status,
        [FromQuery] string sort = "dateApplied",
        [FromQuery] string order = "desc",
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetApplicationsQuery(status, sort, order), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(OperationId = "GetApplication")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetApplicationByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [SwaggerOperation(OperationId = "CreateApplication")]
    public async Task<IActionResult> Create(
        [FromBody] CreateApplicationCommand command, CancellationToken ct = default)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(OperationId = "UpdateApplication")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateApplicationRequest request, CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateApplicationCommand(id, request.CompanyName, request.Role,
                request.DateApplied, request.Status, request.ContactName,
                request.ContactEmail, request.ContactPhone, request.JobPosting), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [SwaggerOperation(OperationId = "PatchApplicationStatus")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateApplicationStatusRequest request, CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateApplicationStatusCommand(id, request.Status), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(OperationId = "DeleteApplication")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await mediator.Send(new DeleteApplicationCommand(id), ct);
        return NoContent();
    }
}

public record UpdateApplicationRequest(
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting);

public record UpdateApplicationStatusRequest(ApplicationStatus Status);
```

- [ ] **Step 4: Create ApplicationNotesController**

Create `backend/Yaam.API/Controllers/ApplicationNotesController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Yaam.Application.Applications.Commands.Notes;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/applications/{applicationId:guid}/notes")]
public class ApplicationNotesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(OperationId = "AddApplicationNote")]
    public async Task<IActionResult> Add(
        Guid applicationId, [FromBody] NoteBodyRequest request, CancellationToken ct = default)
    {
        var result = await mediator.Send(new AddApplicationNoteCommand(applicationId, request.Body), ct);
        return result is null ? NotFound() : Created(string.Empty, result);
    }

    [HttpPut("{noteId:guid}")]
    [SwaggerOperation(OperationId = "UpdateApplicationNote")]
    public async Task<IActionResult> Update(
        Guid applicationId, Guid noteId, [FromBody] NoteBodyRequest request, CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new UpdateApplicationNoteCommand(applicationId, noteId, request.Body), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{noteId:guid}")]
    [SwaggerOperation(OperationId = "DeleteApplicationNote")]
    public async Task<IActionResult> Delete(
        Guid applicationId, Guid noteId, CancellationToken ct = default)
    {
        await mediator.Send(new DeleteApplicationNoteCommand(applicationId, noteId), ct);
        return NoContent();
    }
}

public record NoteBodyRequest(string Body);
```

- [ ] **Step 5: Verify full build**

```bash
cd /path/to/yaam/backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Smoke-test the API**

```bash
dotnet run --project Yaam.API
```

Open `https://localhost:5001/swagger` — expect Swagger UI showing all 9 endpoints under `Applications` and `ApplicationNotes`.

- [ ] **Step 7: Commit**

```bash
git add backend/Yaam.API/
git commit -m "feat: add ApplicationsController and ApplicationNotesController"
```

---

## Task 8: Integration tests

**Files:**
- Modify: `backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj`
- Create: `backend/Yaam.Tests.Integration/ApiFactory.cs`
- Create: `backend/Yaam.Tests.Integration/Applications/ApplicationsEndpointsTests.cs`

**Interfaces:**
- Consumes: `Program` (partial class), `AppDbContext`, all endpoints from Task 7
- Produces: verified end-to-end coverage of all 9 endpoints

- [ ] **Step 1: Add required packages to integration test project**

```bash
cd /path/to/yaam/backend
dotnet add Yaam.Tests.Integration/Yaam.Tests.Integration.csproj \
  package Microsoft.AspNetCore.Mvc.Testing
dotnet add Yaam.Tests.Integration/Yaam.Tests.Integration.csproj \
  package FluentAssertions --version 6.*
```

- [ ] **Step 2: Create ApiFactory**

Create `backend/Yaam.Tests.Integration/ApiFactory.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yaam.Infrastructure.Persistence;

namespace Yaam.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? "Host=localhost;Port=5433;Database=yaam_test;Username=yaam;Password=yaam";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}
```

- [ ] **Step 3: Create endpoint tests**

Create `backend/Yaam.Tests.Integration/Applications/ApplicationsEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Enums;

namespace Yaam.Tests.Integration.Applications;

public class ApplicationsEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_Applications_ReturnsEmptyList_WhenNoApplicationsExist()
    {
        var response = await _client.GetAsync("/api/applications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ApplicationSummaryDto>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Applications_Returns201_WithValidDraftApplication()
    {
        var payload = new
        {
            companyName = "Acme Corp",
            role = "Software Engineer",
            status = "Draft"
        };

        var response = await _client.PostAsJsonAsync("/api/applications", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApplicationDto>();
        body!.CompanyName.Should().Be("Acme Corp");
        body.Status.Should().Be(ApplicationStatus.Draft);
        body.DateApplied.Should().BeNull();
    }

    [Fact]
    public async Task POST_Applications_Returns400_WhenDateAppliedMissingForNonDraft()
    {
        var payload = new
        {
            companyName = "Acme Corp",
            role = "Engineer",
            status = "Applied"
        };

        var response = await _client.PostAsJsonAsync("/api/applications", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_ApplicationById_Returns404_ForUnknownId()
    {
        var response = await _client.GetAsync($"/api/applications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Application_UpdatesAndReturns200()
    {
        var created = await CreateApplicationAsync("Update Test Co", "Dev");

        var update = new
        {
            companyName = "Updated Co",
            role = "Dev",
            status = "Applied",
            dateApplied = "2026-07-10"
        };

        var response = await _client.PutAsJsonAsync($"/api/applications/{created.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationDto>();
        body!.CompanyName.Should().Be("Updated Co");
    }

    [Fact]
    public async Task PATCH_ApplicationStatus_UpdatesStatusImmediately()
    {
        var created = await CreateApplicationAsync("Status Test Co", "QA");

        var patch = new { status = "Applied" };
        var response = await _client.PatchAsJsonAsync($"/api/applications/{created.Id}/status", patch);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationDto>();
        body!.Status.Should().Be(ApplicationStatus.Applied);
    }

    [Fact]
    public async Task DELETE_Application_Returns204()
    {
        var created = await CreateApplicationAsync("Delete Test Co", "PM");

        var response = await _client.DeleteAsync($"/api/applications/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task POST_Note_Returns201_WithValidBody()
    {
        var app = await CreateApplicationAsync("Note Test Co", "Dev");

        var payload = new { body = "Had a great call with the recruiter." };
        var response = await _client.PostAsJsonAsync(
            $"/api/applications/{app.Id}/notes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var note = await response.Content.ReadFromJsonAsync<ApplicationNoteDto>();
        note!.Body.Should().Be("Had a great call with the recruiter.");
    }

    [Fact]
    public async Task PUT_Note_UpdatesBody()
    {
        var app = await CreateApplicationAsync("Note Update Co", "Dev");
        var note = await CreateNoteAsync(app.Id, "Original note.");

        var update = new { body = "Updated note." };
        var response = await _client.PutAsJsonAsync(
            $"/api/applications/{app.Id}/notes/{note.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationNoteDto>();
        body!.Body.Should().Be("Updated note.");
    }

    [Fact]
    public async Task DELETE_Note_Returns204()
    {
        var app = await CreateApplicationAsync("Note Delete Co", "Dev");
        var note = await CreateNoteAsync(app.Id, "To be deleted.");

        var response = await _client.DeleteAsync(
            $"/api/applications/{app.Id}/notes/{note.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<ApplicationDto> CreateApplicationAsync(string company, string role)
    {
        var payload = new { companyName = company, role, status = "Draft" };
        var response = await _client.PostAsJsonAsync("/api/applications", payload);
        return (await response.Content.ReadFromJsonAsync<ApplicationDto>())!;
    }

    private async Task<ApplicationNoteDto> CreateNoteAsync(Guid applicationId, string body)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/applications/{applicationId}/notes", new { body });
        return (await response.Content.ReadFromJsonAsync<ApplicationNoteDto>())!;
    }
}
```

- [ ] **Step 4: Run integration tests**

Ensure PostgreSQL is running (`docker compose up -d postgres`), then:

```bash
cd /path/to/yaam/backend
dotnet test Yaam.Tests.Integration
```

Expected: PASS — all 10 tests passing.

- [ ] **Step 5: Run full test suite**

```bash
dotnet test
```

Expected: all unit + integration tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/Yaam.Tests.Integration/
git commit -m "test: add integration tests for all application and note endpoints"
```

---

## Task 9: openapi-generator-cli and Tailwind/daisyUI setup

**Files:**
- Create: `frontend/openapitools.json`
- Create: `frontend/tailwind.config.js`
- Create: `frontend/postcss.config.js`
- Modify: `frontend/package.json`
- Modify: `frontend/src/styles.scss`
- Modify: `.gitignore`

**Interfaces:**
- Produces: `frontend/src/app/generated/api/` (generated, not committed), Tailwind + daisyUI available in all components

- [ ] **Step 1: Add generated directory to .gitignore**

Add to `.gitignore`:

```gitignore
# Generated API client
frontend/src/app/generated/
```

Also add an ESLint ignore file for the generated code. Create `frontend/src/app/generated/.eslintignore` is not needed — instead add to `frontend/.eslintignore` if it exists, or note that ESLint should be configured to ignore generated files. Add to the root `eslint.config.js` or add a note in the plan. For now, add to `.gitignore` only and move on.

- [ ] **Step 2: Install openapi-generator-cli**

```bash
cd /path/to/yaam/frontend
npm install --save-dev @openapitools/openapi-generator-cli
```

- [ ] **Step 3: Create openapitools.json**

Create `frontend/openapitools.json`:

```json
{
  "generator-cli": {
    "version": "7.13.0",
    "generators": {
      "yaam-api": {
        "generatorName": "typescript-angular",
        "output": "src/app/generated/api",
        "inputSpec": "http://localhost:5001/swagger/v1/swagger.json",
        "additionalProperties": {
          "ngVersion": "22",
          "providedIn": "root",
          "withInterfaces": true,
          "enumPropertyNaming": "original"
        }
      }
    }
  }
}
```

- [ ] **Step 4: Add generate:api script to package.json**

In `frontend/package.json`, add to `scripts`:

```json
"generate:api": "openapi-generator-cli generate"
```

- [ ] **Step 5: Generate the API client**

Ensure the backend API is running (`dotnet run --project backend/Yaam.API`), then:

```bash
cd /path/to/yaam/frontend
npm run generate:api
```

Expected: files created in `frontend/src/app/generated/api/`. Verify you see `api/applications.service.ts` and `model/applicationDto.ts` among the generated files.

- [ ] **Step 6: Install Tailwind CSS and daisyUI**

```bash
cd /path/to/yaam/frontend
npm install --save-dev tailwindcss@3 postcss autoprefixer daisyui@3
npx tailwindcss init -p
```

This creates `tailwind.config.js` and `postcss.config.js`.

- [ ] **Step 7: Configure tailwind.config.js**

Replace the generated `frontend/tailwind.config.js`:

```js
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: { extend: {} },
  plugins: [require('daisyui')],
  daisyui: {
    themes: ['light', 'dark'],
  },
};
```

- [ ] **Step 8: Update styles.scss**

Replace `frontend/src/styles.scss`:

```scss
@tailwind base;
@tailwind components;
@tailwind utilities;
```

- [ ] **Step 9: Verify Angular build still passes**

```bash
cd /path/to/yaam/frontend
npx ng build --configuration production
```

Expected: build succeeds with no errors.

- [ ] **Step 10: Commit**

```bash
cd /path/to/yaam
git add frontend/ .gitignore
git commit -m "feat: add openapi-generator-cli, Tailwind CSS v3, and daisyUI v3"
```

---

## Task 10: Angular models and ProblemDetails interceptor

**Files:**
- Create: `frontend/src/app/features/applications/models/application.model.ts`
- Create: `frontend/src/app/features/applications/models/application-note.model.ts`
- Create: `frontend/src/app/core/interceptors/problem-details.interceptor.ts`
- Modify: `frontend/src/app/app.config.ts`

**Interfaces:**
- Produces: `Application`, `ApplicationNote`, `ApplicationStatus` re-exported from generated client; `problemDetailsInterceptor` registered globally

- [ ] **Step 1: Create application model (re-exports generated types)**

Create `frontend/src/app/features/applications/models/application.model.ts`:

```typescript
export {
  ApplicationDto as Application,
  ApplicationSummaryDto as ApplicationSummary,
  ApplicationStatus,
} from '../../../generated/api';

export const STATUS_LABELS: Record<string, string> = {
  Draft: 'Draft',
  Applied: 'Applied',
  InterviewScheduled: 'Interview Scheduled',
  Interviewed: 'Interviewed',
  OfferReceived: 'Offer Received',
  Accepted: 'Accepted',
  Rejected: 'Rejected',
  Withdrawn: 'Withdrawn',
};

export const ALL_STATUSES = Object.keys(STATUS_LABELS);
```

- [ ] **Step 2: Create application note model**

Create `frontend/src/app/features/applications/models/application-note.model.ts`:

```typescript
export { ApplicationNoteDto as ApplicationNote } from '../../../generated/api';
```

- [ ] **Step 3: Create ProblemDetails interceptor**

Create `frontend/src/app/core/interceptors/problem-details.interceptor.ts`:

```typescript
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface ProblemDetails {
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}

export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const problem: ProblemDetails = error.error ?? {
        title: 'An unexpected error occurred.',
        status: error.status,
      };
      return throwError(() => problem);
    })
  );
```

- [ ] **Step 4: Register interceptor in app.config.ts**

Replace `frontend/src/app/app.config.ts`:

```typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { problemDetailsInterceptor } from './core/interceptors/problem-details.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([problemDetailsInterceptor])),
  ],
};
```

- [ ] **Step 5: Verify build**

```bash
cd /path/to/yaam/frontend
npx ng build
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/
git commit -m "feat: add application models, ProblemDetails interceptor, and HttpClient config"
```

---

## Task 11: Application list page

**Files:**
- Create: `frontend/src/app/features/applications/pages/application-list/application-list.component.ts`
- Create: `frontend/src/app/features/applications/pages/application-list/application-list.component.html`
- Modify: `frontend/src/app/features/applications/applications.routes.ts`

**Interfaces:**
- Consumes: `ApplicationsService` (generated), `ApplicationSummary`, `STATUS_LABELS`, `ALL_STATUSES`
- Produces: `/applications` route showing filterable, sortable list

- [ ] **Step 1: Create ApplicationListComponent**

Create `frontend/src/app/features/applications/pages/application-list/application-list.component.ts`:

```typescript
import { Component, inject, signal, resource } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApplicationsService } from '../../../../generated/api';
import {
  ApplicationSummary,
  ALL_STATUSES,
  STATUS_LABELS,
} from '../../models/application.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-application-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './application-list.component.html',
})
export class ApplicationListComponent {
  private readonly api = inject(ApplicationsService);
  protected readonly router = inject(Router);

  protected readonly STATUS_LABELS = STATUS_LABELS;
  protected readonly ALL_STATUSES = ALL_STATUSES;

  protected readonly filterStatus = signal('');
  protected readonly sort = signal('dateApplied');
  protected readonly order = signal<'asc' | 'desc'>('desc');

  protected readonly applications = resource<ApplicationSummary[], unknown>({
    request: () => ({
      status: this.filterStatus(),
      sort: this.sort(),
      order: this.order(),
    }),
    loader: ({ request }) =>
      firstValueFrom(
        this.api.listApplications(
          request.status || undefined,
          request.sort,
          request.order
        )
      ),
  });

  protected setFilter(status: string): void {
    this.filterStatus.set(status);
  }

  protected toggleOrder(): void {
    this.order.update(o => (o === 'desc' ? 'asc' : 'desc'));
  }
}
```

- [ ] **Step 2: Create ApplicationListComponent template**

Create `frontend/src/app/features/applications/pages/application-list/application-list.component.html`:

```html
<div class="container mx-auto p-6">
  <div class="flex items-center justify-between mb-6">
    <h1 class="text-2xl font-bold">Applications</h1>
    <a routerLink="/applications/new" class="btn btn-primary btn-sm">Add application</a>
  </div>

  <div class="flex gap-2 mb-4 flex-wrap">
    <button class="btn btn-sm" [class.btn-active]="filterStatus() === ''" (click)="setFilter('')">All</button>
    @for (s of ALL_STATUSES; track s) {
      <button
        class="btn btn-sm"
        [class.btn-active]="filterStatus() === s"
        (click)="setFilter(s)">
        {{ STATUS_LABELS[s] }}
      </button>
    }
  </div>

  @if (applications.isLoading()) {
    <div class="flex justify-center py-12">
      <span class="loading loading-spinner loading-lg"></span>
    </div>
  } @else if (applications.status() === 'error') {
    <div class="alert alert-error">Failed to load applications.</div>
  } @else if (!applications.value()?.length) {
    <div class="flex flex-col items-center py-16 text-base-content/50">
      <p class="text-lg">No applications yet.</p>
      <a routerLink="/applications/new" class="btn btn-primary mt-4">Add your first application</a>
    </div>
  } @else {
    <div class="card bg-base-100 shadow">
      <div class="overflow-x-auto">
        <table class="table table-zebra">
          <thead>
            <tr>
              <th>Company</th>
              <th>Role</th>
              <th>
                <button class="flex items-center gap-1" (click)="toggleOrder()">
                  Date applied
                  <span>{{ order() === 'desc' ? '↓' : '↑' }}</span>
                </button>
              </th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            @for (app of applications.value(); track app.id) {
              <tr class="cursor-pointer hover" [routerLink]="['/applications', app.id]">
                <td class="font-medium">{{ app.companyName }}</td>
                <td>{{ app.role }}</td>
                <td>{{ app.dateApplied ?? '—' }}</td>
                <td>
                  <span class="badge badge-sm" [class]="statusBadgeClass(app.status)">
                    {{ STATUS_LABELS[app.status] ?? app.status }}
                  </span>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  }
</div>
```

Add `statusBadgeClass` to the component class:

```typescript
protected statusBadgeClass(status: string): string {
  const map: Record<string, string> = {
    Draft: 'badge-ghost',
    Applied: 'badge-info',
    InterviewScheduled: 'badge-primary',
    Interviewed: 'badge-primary',
    OfferReceived: 'badge-success',
    Accepted: 'badge-success',
    Rejected: 'badge-error',
    Withdrawn: 'badge-warning',
  };
  return map[status] ?? 'badge-ghost';
}
```

- [ ] **Step 3: Update applications.routes.ts**

Replace `frontend/src/app/features/applications/applications.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { ApplicationListComponent } from './pages/application-list/application-list.component';

export const APPLICATIONS_ROUTES: Routes = [
  { path: '', component: ApplicationListComponent },
];
```

- [ ] **Step 4: Verify build**

```bash
cd /path/to/yaam/frontend
npx ng build
```

Expected: build succeeds with no type errors.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/
git commit -m "feat: add application list page with status filter and sort"
```

---

## Task 12: Application detail page and form component

**Files:**
- Create: `frontend/src/app/features/applications/components/application-form/application-form.component.ts`
- Create: `frontend/src/app/features/applications/components/application-form/application-form.component.html`
- Create: `frontend/src/app/features/applications/pages/application-detail/application-detail.component.ts`
- Create: `frontend/src/app/features/applications/pages/application-detail/application-detail.component.html`
- Modify: `frontend/src/app/features/applications/applications.routes.ts`

**Interfaces:**
- Consumes: `ApplicationsService` (generated), `Application`, `STATUS_LABELS`, `ALL_STATUSES`
- Produces: `/applications/new` (create) and `/applications/:id` (view/edit/status-patch) routes

- [ ] **Step 1: Create ApplicationFormComponent**

Create `frontend/src/app/features/applications/components/application-form/application-form.component.ts`:

```typescript
import { Component, input, output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Application, ALL_STATUSES, STATUS_LABELS } from '../../models/application.model';

@Component({
  selector: 'app-application-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './application-form.component.html',
})
export class ApplicationFormComponent implements OnInit {
  readonly existing = input<Application | null>(null);
  readonly saved = output<FormGroup>();
  readonly cancelled = output<void>();

  protected readonly ALL_STATUSES = ALL_STATUSES;
  protected readonly STATUS_LABELS = STATUS_LABELS;

  protected form!: FormGroup;

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    const a = this.existing();
    this.form = this.fb.group({
      companyName: [a?.companyName ?? '', Validators.required],
      role: [a?.role ?? '', Validators.required],
      dateApplied: [a?.dateApplied ?? null],
      status: [a?.status ?? 'Draft', Validators.required],
      contactName: [a?.contactName ?? ''],
      contactEmail: [a?.contactEmail ?? ''],
      contactPhone: [a?.contactPhone ?? ''],
      jobPosting: [a?.jobPosting ?? ''],
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saved.emit(this.form);
  }

  protected cancel(): void {
    this.cancelled.emit();
  }

  protected fieldError(field: string): string | null {
    const control = this.form.get(field);
    if (!control?.touched || !control.errors) return null;
    if (control.errors['required']) return `${field} is required.`;
    return null;
  }
}
```

- [ ] **Step 2: Create ApplicationFormComponent template**

Create `frontend/src/app/features/applications/components/application-form/application-form.component.html`:

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">
  <div class="form-control">
    <label class="label"><span class="label-text">Company name *</span></label>
    <input formControlName="companyName" type="text" class="input input-bordered" />
    @if (fieldError('companyName')) {
      <label class="label"><span class="label-text-alt text-error">{{ fieldError('companyName') }}</span></label>
    }
  </div>

  <div class="form-control">
    <label class="label"><span class="label-text">Role *</span></label>
    <input formControlName="role" type="text" class="input input-bordered" />
    @if (fieldError('role')) {
      <label class="label"><span class="label-text-alt text-error">{{ fieldError('role') }}</span></label>
    }
  </div>

  <div class="form-control">
    <label class="label"><span class="label-text">Status *</span></label>
    <select formControlName="status" class="select select-bordered">
      @for (s of ALL_STATUSES; track s) {
        <option [value]="s">{{ STATUS_LABELS[s] }}</option>
      }
    </select>
  </div>

  <div class="form-control">
    <label class="label"><span class="label-text">Date applied</span></label>
    <input formControlName="dateApplied" type="date" class="input input-bordered" />
  </div>

  <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
    <div class="form-control">
      <label class="label"><span class="label-text">Contact name</span></label>
      <input formControlName="contactName" type="text" class="input input-bordered" />
    </div>
    <div class="form-control">
      <label class="label"><span class="label-text">Contact email</span></label>
      <input formControlName="contactEmail" type="email" class="input input-bordered" />
    </div>
    <div class="form-control">
      <label class="label"><span class="label-text">Contact phone</span></label>
      <input formControlName="contactPhone" type="text" class="input input-bordered" />
    </div>
  </div>

  <div class="form-control">
    <label class="label"><span class="label-text">Job posting</span></label>
    <textarea formControlName="jobPosting" rows="6" class="textarea textarea-bordered"></textarea>
  </div>

  <div class="flex gap-2 justify-end">
    <button type="button" class="btn btn-ghost" (click)="cancel()">Cancel</button>
    <button type="submit" class="btn btn-primary">Save</button>
  </div>
</form>
```

- [ ] **Step 3: Create ApplicationDetailComponent**

Create `frontend/src/app/features/applications/pages/application-detail/application-detail.component.ts`:

```typescript
import { Component, computed, inject, resource, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { firstValueFrom } from 'rxjs';
import { ApplicationsService } from '../../../../generated/api';
import { Application, STATUS_LABELS, ALL_STATUSES } from '../../models/application.model';
import { ApplicationFormComponent } from '../../components/application-form/application-form.component';
import { ApplicationNotesComponent } from '../../components/application-notes/application-notes.component';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-application-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ApplicationFormComponent, ApplicationNotesComponent],
  templateUrl: './application-detail.component.html',
})
export class ApplicationDetailComponent {
  private readonly api = inject(ApplicationsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly STATUS_LABELS = STATUS_LABELS;
  protected readonly ALL_STATUSES = ALL_STATUSES;

  protected readonly editMode = signal(false);
  protected readonly statusSaving = signal(false);
  protected readonly deleteModalOpen = signal(false);
  protected readonly serverErrors = signal<Record<string, string[]>>({});

  protected readonly applicationId = toSignal(
    this.route.paramMap.pipe(map(p => p.get('id') ?? undefined))
  );
  protected readonly isNew = computed(() => !this.applicationId());

  protected readonly applicationResource = resource<Application, string | undefined>({
    request: () => this.applicationId(),
    loader: ({ request }) =>
      request
        ? firstValueFrom(this.api.getApplication(request))
        : Promise.resolve(undefined),
  });

  protected async onSave(form: FormGroup): Promise<void> {
    const value = form.value;
    try {
      if (this.isNew()) {
        const created = await firstValueFrom(this.api.createApplication(value));
        await this.router.navigate(['/applications', created.id]);
      } else {
        await firstValueFrom(
          this.api.updateApplication(this.applicationId()!, value)
        );
        this.applicationResource.reload();
        this.editMode.set(false);
      }
    } catch (err: any) {
      this.serverErrors.set(err?.errors ?? {});
    }
  }

  protected onCancelEdit(): void {
    if (this.isNew()) {
      this.router.navigate(['/applications']);
    } else {
      this.editMode.set(false);
    }
  }

  protected async onStatusChange(event: Event): Promise<void> {
    const status = (event.target as HTMLSelectElement).value;
    this.statusSaving.set(true);
    try {
      await firstValueFrom(
        this.api.patchApplicationStatus(this.applicationId()!, { status } as any)
      );
      this.applicationResource.reload();
    } finally {
      this.statusSaving.set(false);
    }
  }

  protected async onDelete(): Promise<void> {
    await firstValueFrom(this.api.deleteApplication(this.applicationId()!));
    await this.router.navigate(['/applications']);
  }
}
```

- [ ] **Step 4: Create ApplicationDetailComponent template**

Create `frontend/src/app/features/applications/pages/application-detail/application-detail.component.html`:

```html
<div class="container mx-auto p-6 max-w-3xl">
  <div class="mb-4">
    <a routerLink="/applications" class="btn btn-ghost btn-sm">← Back</a>
  </div>

  @if (applicationResource.isLoading()) {
    <div class="flex justify-center py-12">
      <span class="loading loading-spinner loading-lg"></span>
    </div>
  } @else if (applicationResource.status() === 'error') {
    <div class="alert alert-error">Failed to load application.</div>
  } @else if (isNew() || editMode()) {
    <div class="card bg-base-100 shadow p-6">
      <h1 class="text-xl font-bold mb-6">{{ isNew() ? 'New application' : 'Edit application' }}</h1>
      <app-application-form
        [existing]="applicationResource.value() ?? null"
        (saved)="onSave($event)"
        (cancelled)="onCancelEdit()" />
    </div>
  } @else {
    @let app = applicationResource.value()!;
    <div class="card bg-base-100 shadow p-6 space-y-6">
      <div class="flex items-start justify-between">
        <div>
          <h1 class="text-2xl font-bold">{{ app.companyName }}</h1>
          <p class="text-base-content/70">{{ app.role }}</p>
        </div>
        <div class="flex gap-2">
          <button class="btn btn-sm btn-outline" (click)="editMode.set(true)">Edit</button>
          <button class="btn btn-sm btn-error btn-outline" (click)="deleteModalOpen.set(true)">Delete</button>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <label class="label-text font-medium">Status</label>
        <select
          class="select select-bordered select-sm"
          [value]="app.status"
          [disabled]="statusSaving()"
          (change)="onStatusChange($event)">
          @for (s of ALL_STATUSES; track s) {
            <option [value]="s">{{ STATUS_LABELS[s] }}</option>
          }
        </select>
        @if (statusSaving()) {
          <span class="loading loading-spinner loading-xs"></span>
        }
      </div>

      <div class="grid grid-cols-2 gap-4 text-sm">
        <div><span class="font-medium">Date applied:</span> {{ app.dateApplied ?? '—' }}</div>
        @if (app.contactName) {
          <div><span class="font-medium">Contact:</span> {{ app.contactName }}</div>
        }
        @if (app.contactEmail) {
          <div><span class="font-medium">Email:</span> {{ app.contactEmail }}</div>
        }
        @if (app.contactPhone) {
          <div><span class="font-medium">Phone:</span> {{ app.contactPhone }}</div>
        }
      </div>

      @if (app.jobPosting) {
        <div>
          <p class="font-medium mb-2">Job posting</p>
          <pre class="bg-base-200 rounded p-4 text-sm whitespace-pre-wrap">{{ app.jobPosting }}</pre>
        </div>
      }

      <app-application-notes [applicationId]="app.id" [initialNotes]="app.notes" />
    </div>

    @if (deleteModalOpen()) {
      <div class="modal modal-open">
        <div class="modal-box">
          <h3 class="font-bold text-lg">Delete application?</h3>
          <p class="py-4">This will permanently delete the application and all its notes.</p>
          <div class="modal-action">
            <button class="btn btn-ghost" (click)="deleteModalOpen.set(false)">Cancel</button>
            <button class="btn btn-error" (click)="onDelete()">Delete</button>
          </div>
        </div>
      </div>
    }
  }
</div>
```

- [ ] **Step 5: Update routes**

Replace `frontend/src/app/features/applications/applications.routes.ts`:

```typescript
import { Routes } from '@angular/router';
import { ApplicationListComponent } from './pages/application-list/application-list.component';
import { ApplicationDetailComponent } from './pages/application-detail/application-detail.component';

export const APPLICATIONS_ROUTES: Routes = [
  { path: '', component: ApplicationListComponent },
  { path: 'new', component: ApplicationDetailComponent },
  { path: ':id', component: ApplicationDetailComponent },
];
```

- [ ] **Step 6: Verify build**

```bash
cd /path/to/yaam/frontend
npx ng build
```

Expected: build succeeds.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/
git commit -m "feat: add application detail page with create, view, edit, and status patch"
```

---

## Task 13: Application notes component

**Files:**
- Create: `frontend/src/app/features/applications/components/application-notes/application-notes.component.ts`
- Create: `frontend/src/app/features/applications/components/application-notes/application-notes.component.html`

**Interfaces:**
- Consumes: `ApplicationsService` (generated), `ApplicationNote` model, `applicationId` and `initialNotes` inputs from `ApplicationDetailComponent`
- Produces: notes section with add, inline edit, delete per note

- [ ] **Step 1: Create ApplicationNotesComponent**

Create `frontend/src/app/features/applications/components/application-notes/application-notes.component.ts`:

```typescript
import { Component, input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApplicationsService } from '../../../../generated/api';
import { ApplicationNote } from '../../models/application-note.model';

@Component({
  selector: 'app-application-notes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './application-notes.component.html',
})
export class ApplicationNotesComponent implements OnInit {
  readonly applicationId = input.required<string>();
  readonly initialNotes = input<ApplicationNote[]>([]);

  private readonly api = inject(ApplicationsService);

  protected notes = signal<ApplicationNote[]>([]);
  protected newNoteBody = signal('');
  protected editingNoteId = signal<string | null>(null);
  protected editingBody = signal('');
  protected deleteConfirmId = signal<string | null>(null);

  ngOnInit(): void {
    this.notes.set([...this.initialNotes()]);
  }

  protected async addNote(): Promise<void> {
    const body = this.newNoteBody().trim();
    if (!body) return;
    const note = await firstValueFrom(
      this.api.addApplicationNote(this.applicationId(), { body })
    );
    this.notes.update(n => [note, ...n]);
    this.newNoteBody.set('');
  }

  protected startEdit(note: ApplicationNote): void {
    this.editingNoteId.set(note.id);
    this.editingBody.set(note.body);
  }

  protected cancelEdit(): void {
    this.editingNoteId.set(null);
    this.editingBody.set('');
  }

  protected async saveEdit(noteId: string): Promise<void> {
    const body = this.editingBody().trim();
    if (!body) return;
    const updated = await firstValueFrom(
      this.api.updateApplicationNote(this.applicationId(), noteId, { body })
    );
    this.notes.update(notes =>
      notes.map(n => (n.id === noteId ? updated : n))
    );
    this.editingNoteId.set(null);
  }

  protected async deleteNote(noteId: string): Promise<void> {
    await firstValueFrom(
      this.api.deleteApplicationNote(this.applicationId(), noteId)
    );
    this.notes.update(notes => notes.filter(n => n.id !== noteId));
    this.deleteConfirmId.set(null);
  }

  protected isEdited(note: ApplicationNote): boolean {
    return note.updatedAt !== note.createdAt;
  }
}
```

- [ ] **Step 2: Create ApplicationNotesComponent template**

Create `frontend/src/app/features/applications/components/application-notes/application-notes.component.html`:

```html
<div>
  <h2 class="text-lg font-semibold mb-4">Notes</h2>

  <div class="form-control mb-4">
    <textarea
      class="textarea textarea-bordered"
      rows="3"
      placeholder="Add a note…"
      [value]="newNoteBody()"
      (input)="newNoteBody.set($any($event.target).value)">
    </textarea>
    <div class="mt-2 flex justify-end">
      <button
        class="btn btn-sm btn-primary"
        [disabled]="!newNoteBody().trim()"
        (click)="addNote()">
        Add note
      </button>
    </div>
  </div>

  @if (!notes().length) {
    <p class="text-base-content/50 text-sm text-center py-6">No notes yet.</p>
  }

  <div class="space-y-3">
    @for (note of notes(); track note.id) {
      <div class="card bg-base-200 p-4">
        @if (editingNoteId() === note.id) {
          <textarea
            class="textarea textarea-bordered w-full mb-2"
            rows="3"
            [value]="editingBody()"
            (input)="editingBody.set($any($event.target).value)">
          </textarea>
          <div class="flex gap-2 justify-end">
            <button class="btn btn-ghost btn-xs" (click)="cancelEdit()">Cancel</button>
            <button class="btn btn-primary btn-xs" (click)="saveEdit(note.id)">Save</button>
          </div>
        } @else {
          <p class="text-sm whitespace-pre-wrap mb-2">{{ note.body }}</p>
          <div class="flex items-center justify-between text-xs text-base-content/50">
            <span>
              {{ note.createdAt | date:'medium' }}
              @if (isEdited(note)) {
                <span class="ml-1">(edited {{ note.updatedAt | date:'medium' }})</span>
              }
            </span>
            <div class="flex gap-2">
              <button class="btn btn-ghost btn-xs" (click)="startEdit(note)">Edit</button>
              @if (deleteConfirmId() === note.id) {
                <button class="btn btn-error btn-xs" (click)="deleteNote(note.id)">Confirm delete</button>
                <button class="btn btn-ghost btn-xs" (click)="deleteConfirmId.set(null)">Cancel</button>
              } @else {
                <button class="btn btn-ghost btn-xs text-error" (click)="deleteConfirmId.set(note.id)">Delete</button>
              }
            </div>
          </div>
        }
      </div>
    }
  </div>
</div>
```

- [ ] **Step 3: Verify full frontend build**

```bash
cd /path/to/yaam/frontend
npx ng build --configuration production
```

Expected: build succeeds, no type errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/
git commit -m "feat: add application notes component with add, edit, and delete"
```

---

## Self-Review

**Spec coverage:**
- Story 1 (create): `POST /api/applications` + `ApplicationDetailComponent` new mode ✅
- Story 2 (list + filter): `GET /api/applications?status=&sort=&order=` + `ApplicationListComponent` ✅
- Story 3 (status patch): `PATCH /api/applications/{id}/status` + status select in detail ✅
- Story 4 (edit mode toggle, delete): `PUT /api/applications/{id}` + `DELETE` + edit/cancel/save in detail ✅
- Story 6 (notes CRUD): three note endpoints + `ApplicationNotesComponent` ✅
- Story 5 (CV link): deferred ✅
- Draft status + conditional DateApplied: validator + model ✅
- IDE run configs: Task 1 ✅
- Rider `.run/Yaam API.run.xml`, WebStorm `.run/Frontend Dev Server.run.xml` ✅

**Placeholder scan:** None found. All steps contain complete code or commands.

**Type consistency:**
- `IApplicationRepository` methods match `ApplicationRepository` implementation ✅
- `GetApplicationsQuery(Status, Sort, Order)` matches handler constructor and test ✅
- `CreateApplicationCommand` 8-param record matches validator and handler ✅
- `ApplicationNoteDto(Id, Body, CreatedAt, UpdatedAt)` used consistently ✅
- `ApplicationNotesComponent` inputs `applicationId` and `initialNotes` match usage in `ApplicationDetailComponent` ✅
- Generated service method names (`listApplications`, `getApplication`, `createApplication`, `updateApplication`, `patchApplicationStatus`, `deleteApplication`, `addApplicationNote`, `updateApplicationNote`, `deleteApplicationNote`) match `SwaggerOperation` OperationId values ✅
