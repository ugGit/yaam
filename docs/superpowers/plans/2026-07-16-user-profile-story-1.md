# User Profile — Story 1: View Profile

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the backend foundation (domain model, GET endpoint) and a read-only profile page that displays all sections with empty-state prompts.

**Architecture:** Singleton `Profile` aggregate with scalar fields (stored as columns), `Skills` as a JSON-serialized text column, and six child-table collections (WorkExperience, Education, Language, Certification, ProfileLink, CustomField). No UserId — one profile per database until auth is added. GET /api/profile calls `GetOrCreateAsync`, which creates an empty profile on first access. Frontend uses `resource()` to load the profile and passes data down to section components via `input()`.

**Tech Stack:** .NET 10, EF Core 10 (Npgsql), MediatR, Angular 22, Angular Signals, daisyUI

## Global Constraints

- Handler parameters: `command` for `IRequestHandler<TCommand>`, `query` for `IRequestHandler<TQuery>` — never `request`
- `CancellationToken` always named `cancellationToken`, never `= default`
- `mediator.Send` formatting: command/query on its own line, `cancellationToken` on third line
- No type aliases — resolve ambiguity by restructuring
- All profile endpoints under `/api/profile`
- Profile is singleton (no `UserId`) until auth is built
- Angular standalone components, signals-first
- daisyUI is the mandatory UI component library — invoke the `daisyui` skill before writing any component HTML
- `frontend/src/app/generated/` is auto-generated — never edit manually; run `npm run generate:api` after backend endpoint changes
- Integration tests use real DB (no mocks), `IClassFixture<ApiFactory>` pattern

---

## File Structure

**Backend — create:**
```
Yaam.Domain/
  Entities/
    Profile.cs
    WorkExperience.cs
    Education.cs
    Language.cs
    Certification.cs
    ProfileLink.cs
    CustomField.cs
  Enums/
    LanguageProficiency.cs
  Repositories/
    IProfileRepository.cs

Yaam.Infrastructure/
  Persistence/
    AppDbContext.cs                        (modify)
    Configurations/
      ProfileConfiguration.cs
      WorkExperienceConfiguration.cs
      EducationConfiguration.cs
      LanguageConfiguration.cs
      CertificationConfiguration.cs
      ProfileLinkConfiguration.cs
      CustomFieldConfiguration.cs
  Repositories/
    ProfileRepository.cs
  DependencyInjection.cs                  (modify)

Yaam.UseCases/
  Profile/
    Queries/
      GetProfileQuery.cs
    Dtos/
      ProfileDto.cs
      WorkExperienceDto.cs
      EducationDto.cs
      LanguageDto.cs
      CertificationDto.cs
      ProfileLinkDto.cs
      CustomFieldDto.cs
    ProfileMapper.cs

Yaam.API/
  Controllers/
    ProfileController.cs

Yaam.Tests.Integration/
  Profile/
    ProfileEndpointsTests.cs
```

**Frontend — create/modify:**
```
frontend/src/app/
  features/profile/
    profile.routes.ts                          (modify)
    models/
      profile.model.ts                         (new)
    pages/
      profile-page/
        profile-page.component.ts              (new)
        profile-page.component.html            (new)
    components/
      profile-info-section/
        profile-info-section.component.ts      (new)
        profile-info-section.component.html    (new)
      profile-skills-section/
        profile-skills-section.component.ts    (new)
        profile-skills-section.component.html  (new)
      work-experience-section/
        work-experience-section.component.ts   (new)
        work-experience-section.component.html (new)
      education-section/
        education-section.component.ts         (new)
        education-section.component.html       (new)
      language-section/
        language-section.component.ts          (new)
        language-section.component.html        (new)
      certification-section/
        certification-section.component.ts     (new)
        certification-section.component.html   (new)
      profile-link-section/
        profile-link-section.component.ts      (new)
        profile-link-section.component.html    (new)
      custom-fields-section/
        custom-fields-section.component.ts     (new)
        custom-fields-section.component.html   (new)
  generated/
    api.ts                                     (modify — add ProfileService)
```

---

### Task 1: Domain entities, EF Core configuration, migration, repository

**Files:**
- Create: `backend/Yaam.Domain/Entities/Profile.cs`
- Create: `backend/Yaam.Domain/Entities/WorkExperience.cs`
- Create: `backend/Yaam.Domain/Entities/Education.cs`
- Create: `backend/Yaam.Domain/Entities/Language.cs`
- Create: `backend/Yaam.Domain/Entities/Certification.cs`
- Create: `backend/Yaam.Domain/Entities/ProfileLink.cs`
- Create: `backend/Yaam.Domain/Entities/CustomField.cs`
- Create: `backend/Yaam.Domain/Enums/LanguageProficiency.cs`
- Create: `backend/Yaam.Domain/Repositories/IProfileRepository.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/ProfileConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/WorkExperienceConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/EducationConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/LanguageConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/CertificationConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/ProfileLinkConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Persistence/Configurations/CustomFieldConfiguration.cs`
- Create: `backend/Yaam.Infrastructure/Repositories/ProfileRepository.cs`
- Modify: `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `backend/Yaam.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Produces: `IProfileRepository` — `GetAsync`, `GetOrCreateAsync`, `UpdateAsync`
- Produces: `Profile` entity with all scalar/collection fields
- Produces: All five child entities registered in EF Core with cascade delete

- [ ] **Step 1: Create CustomField entity**

`backend/Yaam.Domain/Entities/CustomField.cs`
```csharp
namespace Yaam.Domain.Entities;

public class CustomField : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Label { get; set; }
    public required string Value { get; set; }
}
```

- [ ] **Step 2: Create LanguageProficiency enum**

`backend/Yaam.Domain/Enums/LanguageProficiency.cs`
```csharp
namespace Yaam.Domain.Enums;

public enum LanguageProficiency
{
    Basic,
    BusinessProficiency,
    Fluent,
    Native
}
```

- [ ] **Step 3: Create Profile entity**

`backend/Yaam.Domain/Entities/Profile.cs`
```csharp
namespace Yaam.Domain.Entities;

public class Profile : Entity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public List<string> Skills { get; set; } = [];
    public ICollection<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();
    public ICollection<Education> Educations { get; set; } = new List<Education>();
    public ICollection<Language> Languages { get; set; } = new List<Language>();
    public ICollection<Certification> Certifications { get; set; } = new List<Certification>();
    public ICollection<ProfileLink> Links { get; set; } = new List<ProfileLink>();
    public ICollection<CustomField> CustomFields { get; set; } = new List<CustomField>();
}
```

- [ ] **Step 4: Create child entities**

`backend/Yaam.Domain/Entities/WorkExperience.cs`
```csharp
namespace Yaam.Domain.Entities;

public class WorkExperience : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Company { get; set; }
    public required string Title { get; set; }
    public required DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
}
```

`backend/Yaam.Domain/Entities/Education.cs`
```csharp
namespace Yaam.Domain.Entities;

public class Education : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Institution { get; set; }
    public string? Degree { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
```

`backend/Yaam.Domain/Entities/Language.cs`
```csharp
using Yaam.Domain.Enums;

namespace Yaam.Domain.Entities;

public class Language : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Name { get; set; }
    public required LanguageProficiency Proficiency { get; set; }
}
```

`backend/Yaam.Domain/Entities/Certification.cs`
```csharp
namespace Yaam.Domain.Entities;

public class Certification : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Name { get; set; }
    public string? Issuer { get; set; }
    public required DateOnly Date { get; set; }
}
```

`backend/Yaam.Domain/Entities/ProfileLink.cs`
```csharp
namespace Yaam.Domain.Entities;

public class ProfileLink : Entity
{
    public required Guid ProfileId { get; set; }
    public required string Label { get; set; }
    public required string Url { get; set; }
}
```

- [ ] **Step 5: Create IProfileRepository**

`backend/Yaam.Domain/Repositories/IProfileRepository.cs`
```csharp
using Yaam.Domain.Entities;

namespace Yaam.Domain.Repositories;

public interface IProfileRepository
{
    Task<Profile?> GetAsync(CancellationToken cancellationToken);
    Task<Profile> GetOrCreateAsync(CancellationToken cancellationToken);
    Task UpdateAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 6: Create EF Core configurations**

`backend/Yaam.Infrastructure/Persistence/Configurations/ProfileConfiguration.cs`
```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FirstName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100);
        builder.Property(p => p.Email).HasMaxLength(200);
        builder.Property(p => p.Phone).HasMaxLength(30);
        builder.Property(p => p.Location).HasMaxLength(200);
        builder.Property(p => p.Summary).HasMaxLength(2000);

        var skillsProp = builder.Property(p => p.Skills)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new())
            .HasColumnType("text");
        skillsProp.Metadata.SetValueComparer(new ValueComparer<List<string>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()));

        builder.HasMany(p => p.WorkExperiences)
            .WithOne()
            .HasForeignKey(w => w.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Educations)
            .WithOne()
            .HasForeignKey(e => e.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Languages)
            .WithOne()
            .HasForeignKey(l => l.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Certifications)
            .WithOne()
            .HasForeignKey(c => c.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Links)
            .WithOne()
            .HasForeignKey(l => l.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.CustomFields)
            .WithOne()
            .HasForeignKey(cf => cf.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

`backend/Yaam.Infrastructure/Persistence/Configurations/WorkExperienceConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class WorkExperienceConfiguration : IEntityTypeConfiguration<WorkExperience>
{
    public void Configure(EntityTypeBuilder<WorkExperience> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Company).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Title).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Description).HasMaxLength(2000);
    }
}
```

`backend/Yaam.Infrastructure/Persistence/Configurations/EducationConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class EducationConfiguration : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Institution).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Degree).HasMaxLength(200);
        builder.Property(e => e.FieldOfStudy).HasMaxLength(200);
    }
}
```

`backend/Yaam.Infrastructure/Persistence/Configurations/LanguageConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Proficiency).IsRequired().HasConversion<string>();
    }
}
```

`backend/Yaam.Infrastructure/Persistence/Configurations/CertificationConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class CertificationConfiguration : IEntityTypeConfiguration<Certification>
{
    public void Configure(EntityTypeBuilder<Certification> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Issuer).HasMaxLength(200);
    }
}
```

`backend/Yaam.Infrastructure/Persistence/Configurations/ProfileLinkConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class ProfileLinkConfiguration : IEntityTypeConfiguration<ProfileLink>
{
    public void Configure(EntityTypeBuilder<ProfileLink> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Label).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Url).IsRequired().HasMaxLength(500);
    }
}
```

`backend/Yaam.Infrastructure/Persistence/Configurations/CustomFieldConfiguration.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Yaam.Domain.Entities;

namespace Yaam.Infrastructure.Persistence.Configurations;

public class CustomFieldConfiguration : IEntityTypeConfiguration<CustomField>
{
    public void Configure(EntityTypeBuilder<CustomField> builder)
    {
        builder.HasKey(cf => cf.Id);
        builder.Property(cf => cf.Label).IsRequired().HasMaxLength(250);
        builder.Property(cf => cf.Value).IsRequired().HasMaxLength(1000);
    }
}
```

- [ ] **Step 7: Register DbSets in AppDbContext**

In `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs`, add these DbSet properties after the existing ones:

```csharp
public DbSet<Profile> Profiles => Set<Profile>();
public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
public DbSet<Education> Educations => Set<Education>();
public DbSet<Language> Languages => Set<Language>();
public DbSet<Certification> Certifications => Set<Certification>();
public DbSet<ProfileLink> ProfileLinks => Set<ProfileLink>();
public DbSet<CustomField> CustomFields => Set<CustomField>();
```

- [ ] **Step 8: Create ProfileRepository**

`backend/Yaam.Infrastructure/Repositories/ProfileRepository.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.Infrastructure.Persistence;

namespace Yaam.Infrastructure.Repositories;

public class ProfileRepository(AppDbContext context) : IProfileRepository
{
    public async Task<Profile?> GetAsync(CancellationToken cancellationToken)
    {
        return await context.Profiles
            .Include(p => p.WorkExperiences)
            .Include(p => p.Educations)
            .Include(p => p.Languages)
            .Include(p => p.Certifications)
            .Include(p => p.Links)
            .Include(p => p.CustomFields)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Profile> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var profile = await GetAsync(cancellationToken);
        if (profile is not null) return profile;

        profile = new Profile();
        context.Profiles.Add(profile);
        await context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public async Task UpdateAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 9: Register IProfileRepository in DI**

In `backend/Yaam.Infrastructure/DependencyInjection.cs`, add after the existing repository registration:

```csharp
services.AddScoped<IProfileRepository, ProfileRepository>();
```

- [ ] **Step 10: Generate and apply migration**

Run from `backend/`:
```bash
dotnet ef migrations add AddProfile --project Yaam.Infrastructure --startup-project Yaam.API
dotnet ef database update --project Yaam.Infrastructure --startup-project Yaam.API
```

Expected: migration file created in `Yaam.Infrastructure/Migrations/`, DB tables created: `Profiles`, `WorkExperiences`, `Educations`, `Languages`, `Certifications`, `ProfileLinks`.

- [ ] **Step 11: Verify the project builds**

```bash
dotnet build backend/Yaam.sln
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 12: Commit**

```bash
git add backend/Yaam.Domain backend/Yaam.Infrastructure
git commit -m "feat: add Profile domain model, EF Core config, migration, repository"
```

---

### Task 2: Get Profile query, DTOs, mapper, controller, integration tests

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Queries/GetProfileQuery.cs`
- Create: `backend/Yaam.UseCases/Profile/Dtos/ProfileDto.cs`
- Create: `backend/Yaam.UseCases/Profile/Dtos/WorkExperienceDto.cs`
- Create: `backend/Yaam.UseCases/Profile/Dtos/EducationDto.cs`
- Create: `backend/Yaam.UseCases/Profile/Dtos/LanguageDto.cs`
- Create: `backend/Yaam.UseCases/Profile/Dtos/CertificationDto.cs`
- Create: `backend/Yaam.UseCases/Profile/Dtos/ProfileLinkDto.cs`
- Create: `backend/Yaam.UseCases/Profile/Dtos/CustomFieldDto.cs`
- Create: `backend/Yaam.UseCases/Profile/ProfileMapper.cs`
- Create: `backend/Yaam.API/Controllers/ProfileController.cs`
- Create: `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`

**Interfaces:**
- Consumes: `IProfileRepository.GetOrCreateAsync` (from Task 1)
- Produces: `GET /api/profile` → 200 `ProfileDto`

- [ ] **Step 1: Write the failing integration test**

`backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`
```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.Tests.Integration.Profile;

public class ProfileEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GET_Profile_ReturnsEmptyProfile_WhenNoneExists()
    {
        var response = await _client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile.Should().NotBeNull();
        profile!.FirstName.Should().BeNull();
        profile.Skills.Should().BeEmpty();
        profile.WorkExperiences.Should().BeEmpty();
        profile.CustomFields.Should().BeEmpty();
    }

    [Fact]
    public async Task GET_Profile_ReturnsSameId_OnSubsequentCalls()
    {
        var r1 = await _client.GetAsync("/api/profile");
        var p1 = await r1.Content.ReadFromJsonAsync<ProfileDto>();

        var r2 = await _client.GetAsync("/api/profile");
        var p2 = await r2.Content.ReadFromJsonAsync<ProfileDto>();

        p2!.Id.Should().Be(p1!.Id);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "FullyQualifiedName~Profile"
```

Expected: FAIL — `ProfileDto` type not found / endpoint returns 404.

- [ ] **Step 3: Create DTOs (one file each)**

`backend/Yaam.UseCases/Profile/Dtos/ProfileDto.cs`
```csharp
namespace Yaam.UseCases.Profile.Dtos;

public record ProfileDto(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Location,
    string? Summary,
    List<WorkExperienceDto> WorkExperiences,
    List<EducationDto> Educations,
    List<string> Skills,
    List<LanguageDto> Languages,
    List<CertificationDto> Certifications,
    List<ProfileLinkDto> Links,
    List<CustomFieldDto> CustomFields);
```

`backend/Yaam.UseCases/Profile/Dtos/WorkExperienceDto.cs`
```csharp
namespace Yaam.UseCases.Profile.Dtos;

public record WorkExperienceDto(
    Guid Id,
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description);
```

`backend/Yaam.UseCases/Profile/Dtos/EducationDto.cs`
```csharp
namespace Yaam.UseCases.Profile.Dtos;

public record EducationDto(
    Guid Id,
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate);
```

`backend/Yaam.UseCases/Profile/Dtos/LanguageDto.cs`
```csharp
namespace Yaam.UseCases.Profile.Dtos;

public record LanguageDto(Guid Id, string Name, string Proficiency);
```

`backend/Yaam.UseCases/Profile/Dtos/CertificationDto.cs`
```csharp
namespace Yaam.UseCases.Profile.Dtos;

public record CertificationDto(Guid Id, string Name, string? Issuer, DateOnly Date);
```

`backend/Yaam.UseCases/Profile/Dtos/ProfileLinkDto.cs`
```csharp
namespace Yaam.UseCases.Profile.Dtos;

public record ProfileLinkDto(Guid Id, string Label, string Url);
```

`backend/Yaam.UseCases/Profile/Dtos/CustomFieldDto.cs`
```csharp
namespace Yaam.UseCases.Profile.Dtos;

public record CustomFieldDto(Guid Id, string Label, string Value);
```

- [ ] **Step 4: Create ProfileMapper**

`backend/Yaam.UseCases/Profile/ProfileMapper.cs`
```csharp
using Yaam.Domain.Entities;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile;

public static class ProfileMapper
{
    public static ProfileDto ToDto(Yaam.Domain.Entities.Profile profile) => new(
        profile.Id,
        profile.FirstName,
        profile.LastName,
        profile.Email,
        profile.Phone,
        profile.Location,
        profile.Summary,
        profile.WorkExperiences.Select(ToDto).ToList(),
        profile.Educations.Select(ToDto).ToList(),
        profile.Skills,
        profile.Languages.Select(ToDto).ToList(),
        profile.Certifications.Select(ToDto).ToList(),
        profile.Links.Select(ToDto).ToList(),
        profile.CustomFields.Select(ToDto).ToList());

    public static WorkExperienceDto ToDto(WorkExperience w) =>
        new(w.Id, w.Company, w.Title, w.StartDate, w.EndDate, w.Description);

    public static EducationDto ToDto(Education e) =>
        new(e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate);

    public static LanguageDto ToDto(Language l) =>
        new(l.Id, l.Name, l.Proficiency.ToString());

    public static CertificationDto ToDto(Certification c) =>
        new(c.Id, c.Name, c.Issuer, c.Date);

    public static ProfileLinkDto ToDto(ProfileLink l) =>
        new(l.Id, l.Label, l.Url);

    public static CustomFieldDto ToDto(CustomField cf) =>
        new(cf.Id, cf.Label, cf.Value);
}
```

- [ ] **Step 5: Create GetProfileQuery with handler**

`backend/Yaam.UseCases/Profile/Queries/GetProfileQuery.cs`
```csharp
using MediatR;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Queries;

public record GetProfileQuery : IRequest<ProfileDto>;

public class GetProfileQueryHandler(IProfileRepository repository)
    : IRequestHandler<GetProfileQuery, ProfileDto>
{
    public async Task<ProfileDto> Handle(GetProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        return ProfileMapper.ToDto(profile);
    }
}
```

- [ ] **Step 6: Create ProfileController**

`backend/Yaam.API/Controllers/ProfileController.cs`
```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.UseCases.Profile.Queries;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("GetProfile")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetProfileQuery(),
            cancellationToken);
        return Ok(result);
    }
}
```

- [ ] **Step 7: Run tests — expect pass**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "FullyQualifiedName~Profile"
```

Expected: PASS — both tests green.

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.UseCases/Profile backend/Yaam.API/Controllers/ProfileController.cs backend/Yaam.Tests.Integration/Profile
git commit -m "feat: add GET /api/profile endpoint with GetOrCreate singleton"
```

---

### Task 3: Frontend models, ProfileService, routes

**Files:**
- Create: `frontend/src/app/features/profile/models/profile.model.ts`
- Modify: `frontend/src/app/features/profile/profile.routes.ts`
- `frontend/src/app/generated/` — regenerated via `npm run generate:api`, never edited manually

**Interfaces:**
- Produces: `Profile` and all child types as TypeScript interfaces
- Produces: `ProfileService.getProfile()` from the generated client

- [ ] **Step 1: Create profile models**

`frontend/src/app/features/profile/models/profile.model.ts`
```typescript
export interface Profile {
  id: string;
  firstName: string | null;
  lastName: string | null;
  email: string | null;
  phone: string | null;
  location: string | null;
  summary: string | null;
  skills: string[];
  workExperiences: WorkExperience[];
  educations: Education[];
  languages: Language[];
  certifications: Certification[];
  links: ProfileLink[];
  customFields: CustomField[];
}

export interface WorkExperience {
  id: string;
  company: string;
  title: string;
  startDate: string;
  endDate: string | null;
  description: string | null;
}

export interface Education {
  id: string;
  institution: string;
  degree: string | null;
  fieldOfStudy: string | null;
  startDate: string | null;
  endDate: string | null;
}

export type LanguageProficiency = 'Basic' | 'BusinessProficiency' | 'Fluent' | 'Native';

export const LANGUAGE_PROFICIENCY_LABELS: Record<LanguageProficiency, string> = {
  Basic: 'Basic',
  BusinessProficiency: 'Business Proficiency',
  Fluent: 'Fluent',
  Native: 'Native',
};

export const ALL_LANGUAGE_PROFICIENCIES: LanguageProficiency[] =
  ['Basic', 'BusinessProficiency', 'Fluent', 'Native'];

export interface Language {
  id: string;
  name: string;
  proficiency: LanguageProficiency;
}

export interface Certification {
  id: string;
  name: string;
  issuer: string | null;
  date: string;
}

export interface ProfileLink {
  id: string;
  label: string;
  url: string;
}

export interface CustomField {
  id: string;
  label: string;
  value: string;
}
```

- [ ] **Step 2: Regenerate the API client**

With the backend running (Task 2 must be complete and the backend started), run from `frontend/`:

```bash
npm run generate:api
```

This calls `http://localhost:5001/openapi/v1.json` and writes the generated Angular services and models to `src/app/generated/api/`. The generated output will include a `ProfileService` with a `getProfile()` method that returns `Observable<ProfileDto>` and all profile model types.

**Never edit files under `src/app/generated/` manually.** Re-run this command whenever backend endpoints are added or changed.

After generation, update any component imports that previously pointed to `'../../../generated/api'` to use the path the generator created (typically `'../../../generated/api/api'` or the specific service file).

- [ ] **Step 3: Update profile.routes.ts**

`frontend/src/app/features/profile/profile.routes.ts`
```typescript
import { Routes } from '@angular/router';
import { ProfilePageComponent } from './pages/profile-page/profile-page.component';

export const PROFILE_ROUTES: Routes = [
  { path: '', component: ProfilePageComponent },
];
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/profile
git commit -m "feat: add Profile TypeScript models and profile routes"
```

---

### Task 4: Profile page and read-only section components

**Files:**
- Create: `profile-page.component.ts` + `.html`
- Create: `profile-info-section.component.ts` + `.html`
- Create: `profile-skills-section.component.ts` + `.html`
- Create: `work-experience-section.component.ts` + `.html`
- Create: `education-section.component.ts` + `.html`
- Create: `language-section.component.ts` + `.html`
- Create: `certification-section.component.ts` + `.html`
- Create: `profile-link-section.component.ts` + `.html`
- Create: `custom-fields-section.component.ts` + `.html`

**Interfaces:**
- Consumes: `ProfileService.getProfile()` (Task 3)
- Produces: `/profile` route renders all sections; empty-state shown when no data

- [ ] **Step 1: Create ProfilePageComponent**

`frontend/src/app/features/profile/pages/profile-page/profile-page.component.ts`
```typescript
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { resource } from '@angular/core';
import { ProfileService } from '../../../../generated/api/api'; // path emitted by openapi-generator; adjust if different
import { Profile } from '../../models/profile.model';
import { ProfileInfoSectionComponent } from '../../components/profile-info-section/profile-info-section.component';
import { ProfileSkillsSectionComponent } from '../../components/profile-skills-section/profile-skills-section.component';
import { WorkExperienceSectionComponent } from '../../components/work-experience-section/work-experience-section.component';
import { EducationSectionComponent } from '../../components/education-section/education-section.component';
import { LanguageSectionComponent } from '../../components/language-section/language-section.component';
import { CertificationSectionComponent } from '../../components/certification-section/certification-section.component';
import { ProfileLinkSectionComponent } from '../../components/profile-link-section/profile-link-section.component';
import { CustomFieldsSectionComponent } from '../../components/custom-fields-section/custom-fields-section.component';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [
    CommonModule,
    ProfileInfoSectionComponent,
    ProfileSkillsSectionComponent,
    WorkExperienceSectionComponent,
    EducationSectionComponent,
    LanguageSectionComponent,
    CertificationSectionComponent,
    ProfileLinkSectionComponent,
    CustomFieldsSectionComponent,
  ],
  templateUrl: './profile-page.component.html',
})
export class ProfilePageComponent {
  private readonly profileService = inject(ProfileService);

  protected readonly profileResource = resource<Profile, void>({
    params: () => undefined,
    loader: () => firstValueFrom(this.profileService.getProfile()),
  });
}
```

`frontend/src/app/features/profile/pages/profile-page/profile-page.component.html`
```html
<div class="container mx-auto max-w-4xl px-4 py-8">
  <h1 class="text-2xl font-bold mb-6">My Profile</h1>

  @if (profileResource.isLoading()) {
    <div class="flex justify-center py-16">
      <span class="loading loading-spinner loading-lg"></span>
    </div>
  } @else if (profileResource.error()) {
    <div class="alert alert-error mb-4">Failed to load profile. Please try again.</div>
  } @else {
    @let profile = profileResource.value()!;
    <div class="flex flex-col gap-6">
      <app-profile-info-section [profile]="profile" />
      <app-work-experience-section [items]="profile.workExperiences" />
      <app-education-section [items]="profile.educations" />
      <app-profile-skills-section [skills]="profile.skills" />
      <app-language-section [items]="profile.languages" />
      <app-certification-section [items]="profile.certifications" />
      <app-profile-link-section [items]="profile.links" />
      <app-custom-fields-section [fields]="profile.customFields" />
    </div>
  }
</div>
```

- [ ] **Step 2: Create ProfileInfoSectionComponent**

`frontend/src/app/features/profile/components/profile-info-section/profile-info-section.component.ts`
```typescript
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Profile } from '../../models/profile.model';

@Component({
  selector: 'app-profile-info-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-info-section.component.html',
})
export class ProfileInfoSectionComponent {
  readonly profile = input.required<Profile>();
}
```

`frontend/src/app/features/profile/components/profile-info-section/profile-info-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <h2 class="card-title text-lg">Contact Info</h2>
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-2">
      <div>
        <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">First name</p>
        @if (profile().firstName) {
          <p>{{ profile().firstName }}</p>
        } @else {
          <p class="text-base-content/40 italic">Not set</p>
        }
      </div>
      <div>
        <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">Last name</p>
        @if (profile().lastName) {
          <p>{{ profile().lastName }}</p>
        } @else {
          <p class="text-base-content/40 italic">Not set</p>
        }
      </div>
      <div>
        <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">Email</p>
        @if (profile().email) {
          <p>{{ profile().email }}</p>
        } @else {
          <p class="text-base-content/40 italic">Not set</p>
        }
      </div>
      <div>
        <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">Phone</p>
        @if (profile().phone) {
          <p>{{ profile().phone }}</p>
        } @else {
          <p class="text-base-content/40 italic">Not set</p>
        }
      </div>
      <div class="sm:col-span-2">
        <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">Location</p>
        @if (profile().location) {
          <p>{{ profile().location }}</p>
        } @else {
          <p class="text-base-content/40 italic">Not set</p>
        }
      </div>
    </div>

    <div class="divider my-2"></div>
    <h2 class="card-title text-lg">Summary</h2>
    @if (profile().summary) {
      <p class="whitespace-pre-wrap">{{ profile().summary }}</p>
    } @else {
      <p class="text-base-content/40 italic">No summary yet. Add a brief professional summary.</p>
    }
  </div>
</div>
```

- [ ] **Step 3: Create ProfileSkillsSectionComponent**

`frontend/src/app/features/profile/components/profile-skills-section/profile-skills-section.component.ts`
```typescript
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-profile-skills-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-skills-section.component.html',
})
export class ProfileSkillsSectionComponent {
  readonly skills = input.required<string[]>();
}
```

`frontend/src/app/features/profile/components/profile-skills-section/profile-skills-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <h2 class="card-title text-lg">Skills</h2>
    @if (skills().length > 0) {
      <div class="flex flex-wrap gap-2 mt-2">
        @for (skill of skills(); track skill) {
          <span class="badge badge-outline">{{ skill }}</span>
        }
      </div>
    } @else {
      <p class="text-base-content/40 italic">No skills added yet.</p>
    }
  </div>
</div>
```

- [ ] **Step 4: Create WorkExperienceSectionComponent**

`frontend/src/app/features/profile/components/work-experience-section/work-experience-section.component.ts`
```typescript
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkExperience } from '../../models/profile.model';

@Component({
  selector: 'app-work-experience-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './work-experience-section.component.html',
})
export class WorkExperienceSectionComponent {
  readonly items = input.required<WorkExperience[]>();
}
```

`frontend/src/app/features/profile/components/work-experience-section/work-experience-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <h2 class="card-title text-lg">Work Experience</h2>
    @if (items().length > 0) {
      <ul class="flex flex-col gap-4 mt-2">
        @for (item of items(); track item.id) {
          <li class="border-l-2 border-base-300 pl-4">
            <p class="font-semibold">{{ item.title }} — {{ item.company }}</p>
            <p class="text-sm text-base-content/60">
              {{ item.startDate }} – {{ item.endDate ?? 'Present' }}
            </p>
            @if (item.description) {
              <p class="text-sm mt-1 whitespace-pre-wrap">{{ item.description }}</p>
            }
          </li>
        }
      </ul>
    } @else {
      <p class="text-base-content/40 italic">No work experience added yet.</p>
    }
  </div>
</div>
```

- [ ] **Step 5: Create EducationSectionComponent**

`frontend/src/app/features/profile/components/education-section/education-section.component.ts`
```typescript
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Education } from '../../models/profile.model';

@Component({
  selector: 'app-education-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './education-section.component.html',
})
export class EducationSectionComponent {
  readonly items = input.required<Education[]>();
}
```

`frontend/src/app/features/profile/components/education-section/education-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <h2 class="card-title text-lg">Education</h2>
    @if (items().length > 0) {
      <ul class="flex flex-col gap-4 mt-2">
        @for (item of items(); track item.id) {
          <li class="border-l-2 border-base-300 pl-4">
            <p class="font-semibold">{{ item.institution }}</p>
            @if (item.degree || item.fieldOfStudy) {
              <p class="text-sm">{{ [item.degree, item.fieldOfStudy].filter(Boolean).join(', ') }}</p>
            }
            @if (item.startDate || item.endDate) {
              <p class="text-sm text-base-content/60">
                {{ item.startDate ?? '?' }} – {{ item.endDate ?? 'Present' }}
              </p>
            }
          </li>
        }
      </ul>
    } @else {
      <p class="text-base-content/40 italic">No education added yet.</p>
    }
  </div>
</div>
```

- [ ] **Step 6: Create LanguageSectionComponent**

`frontend/src/app/features/profile/components/language-section/language-section.component.ts`
```typescript
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Language, LANGUAGE_PROFICIENCY_LABELS } from '../../models/profile.model';

@Component({
  selector: 'app-language-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './language-section.component.html',
})
export class LanguageSectionComponent {
  readonly items = input.required<Language[]>();
  protected readonly proficiencyLabels = LANGUAGE_PROFICIENCY_LABELS;
}
```

`frontend/src/app/features/profile/components/language-section/language-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <h2 class="card-title text-lg">Languages</h2>
    @if (items().length > 0) {
      <ul class="flex flex-col gap-2 mt-2">
        @for (item of items(); track item.id) {
          <li class="flex justify-between">
            <span>{{ item.name }}</span>
            <span class="badge badge-ghost">{{ proficiencyLabels[item.proficiency] }}</span>
          </li>
        }
      </ul>
    } @else {
      <p class="text-base-content/40 italic">No languages added yet.</p>
    }
  </div>
</div>
```

- [ ] **Step 7: Create CertificationSectionComponent**

`frontend/src/app/features/profile/components/certification-section/certification-section.component.ts`
```typescript
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Certification } from '../../models/profile.model';

@Component({
  selector: 'app-certification-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './certification-section.component.html',
})
export class CertificationSectionComponent {
  readonly items = input.required<Certification[]>();
}
```

`frontend/src/app/features/profile/components/certification-section/certification-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <h2 class="card-title text-lg">Certifications</h2>
    @if (items().length > 0) {
      <ul class="flex flex-col gap-2 mt-2">
        @for (item of items(); track item.id) {
          <li>
            <p class="font-medium">{{ item.name }}</p>
            @if (item.issuer) {
              <p class="text-sm text-base-content/60">{{ item.issuer }} · {{ item.date }}</p>
            } @else {
              <p class="text-sm text-base-content/60">{{ item.date }}</p>
            }
          </li>
        }
      </ul>
    } @else {
      <p class="text-base-content/40 italic">No certifications added yet.</p>
    }
  </div>
</div>
```

- [ ] **Step 8: Create ProfileLinkSectionComponent**

`frontend/src/app/features/profile/components/profile-link-section/profile-link-section.component.ts`
```typescript
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProfileLink } from '../../models/profile.model';

@Component({
  selector: 'app-profile-link-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-link-section.component.html',
})
export class ProfileLinkSectionComponent {
  readonly items = input.required<ProfileLink[]>();
}
```

`frontend/src/app/features/profile/components/profile-link-section/profile-link-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <h2 class="card-title text-lg">Links</h2>
    @if (items().length > 0) {
      <ul class="flex flex-col gap-2 mt-2">
        @for (item of items(); track item.id) {
          <li>
            <a [href]="item.url" target="_blank" rel="noopener noreferrer"
               class="link link-primary">{{ item.label }}</a>
          </li>
        }
      </ul>
    } @else {
      <p class="text-base-content/40 italic">No links added yet.</p>
    }
  </div>
</div>
```

- [ ] **Step 9: Create CustomFieldsSectionComponent**

`frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.ts`
```typescript
import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CustomField } from '../../models/profile.model';

@Component({
  selector: 'app-custom-fields-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-fields-section.component.html',
})
export class CustomFieldsSectionComponent {
  readonly fields = input.required<CustomField[]>();
}
```

`frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <h2 class="card-title text-lg">Additional Info</h2>
    @if (fields().length > 0) {
      <dl class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-2">
        @for (field of fields(); track field.id) {
          <div>
            <dt class="text-xs text-base-content/50 uppercase tracking-wide">{{ field.label }}</dt>
            <dd>{{ field.value }}</dd>
          </div>
        }
      </dl>
    } @else {
      <p class="text-base-content/40 italic">No additional info added yet.</p>
    }
  </div>
</div>
```

- [ ] **Step 10: Build and verify**

```bash
cd frontend && npm run build
```

Expected: Build succeeded, no TypeScript errors. Navigate to `http://localhost:4200/profile` and confirm:
- Loading spinner appears briefly
- All sections render (all empty state initially)
- No console errors

- [ ] **Step 11: Commit**

```bash
git add frontend/src/app/features/profile
git commit -m "feat: profile page with read-only section display and empty states"
```
