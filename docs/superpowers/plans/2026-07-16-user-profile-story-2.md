# User Profile — Story 2: Edit Predefined Fields

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add mutation endpoints for all predefined profile sections (info, skills, and five list types) and wire them into the frontend with section-level edit modes and modal-based CRUD for list items.

**Architecture:** Backend gets one command per mutation (Add/Update/Delete per list type, plus UpdateInfo and UpdateSkills). All commands go through the existing MediatR pipeline. Frontend section components become semi-smart: they inject `ProfileService`, call mutations directly, and emit a `changed` output so the page can reload. List item modals are presentational — they emit form data upward; the section component owns the API call. Tag input for skills is built inline (no extra library).

**Prerequisites:** Story 1 plan fully implemented — domain model, migration, GET /api/profile, and all read-only section components exist.

**Tech Stack:** .NET 10, EF Core 10, MediatR, FluentValidation, Angular 22, Angular Signals Forms API (`@angular/forms/signals`), daisyUI

## Global Constraints

- Handler parameters: `command` for `IRequestHandler<TCommand>` — never `request`
- `CancellationToken` always named `cancellationToken`, never `= default`
- `mediator.Send` formatting: command on its own line, `cancellationToken` on third line
- No type aliases — resolve ambiguity by restructuring
- All profile endpoints under `/api/profile`
- `frontend/src/app/generated/` — never edit manually; regenerate with `npm run generate:api`
- Invoke the `daisyui` skill before writing any component HTML
- Integration tests use real DB, `IClassFixture<ApiFactory>` pattern
- Angular standalone components, signals-first; `firstValueFrom()` to bridge Observables

---

## File Structure

**Backend — create:**
```
Yaam.UseCases/Profile/
  Commands/
    UpdateProfileInfoCommand.cs
    UpdateProfileSkillsCommand.cs
    WorkExperiences/
      AddWorkExperienceCommand.cs
      UpdateWorkExperienceCommand.cs
      DeleteWorkExperienceCommand.cs
    Education/
      AddEducationCommand.cs
      UpdateEducationCommand.cs
      DeleteEducationCommand.cs
    Languages/
      AddLanguageCommand.cs
      UpdateLanguageCommand.cs
      DeleteLanguageCommand.cs
    Certifications/
      AddCertificationCommand.cs
      UpdateCertificationCommand.cs
      DeleteCertificationCommand.cs
    Links/
      AddProfileLinkCommand.cs
      UpdateProfileLinkCommand.cs
      DeleteProfileLinkCommand.cs

Yaam.API/Controllers/
  ProfileController.cs              (modify — add all mutation actions)

Yaam.Tests.Integration/Profile/
  ProfileEndpointsTests.cs          (modify — add mutation tests)
```

**Frontend — modify:**
```
features/profile/
  pages/profile-page/
    profile-page.component.ts       (modify — add changed handler)
    profile-page.component.html     (modify — wire changed outputs)
  components/
    profile-info-section/           (modify — add edit mode)
    profile-skills-section/         (modify — add tag input edit)
    work-experience-section/        (modify — add modal CRUD)
    work-experience-modal/          (new)
    education-section/              (modify — add modal CRUD)
    education-modal/                (new)
    language-section/               (modify — add modal CRUD)
    language-modal/                 (new)
    certification-section/          (modify — add modal CRUD)
    certification-modal/            (new)
    profile-link-section/           (modify — add modal CRUD)
    profile-link-modal/             (new)
```

---

### Task 1: Update profile info + skills — commands, controller, tests

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/UpdateProfileInfoCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/UpdateProfileSkillsCommand.cs`
- Modify: `backend/Yaam.API/Controllers/ProfileController.cs`
- Modify: `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`

**Interfaces:**
- Consumes: `IProfileRepository.GetOrCreateAsync`, `IProfileRepository.UpdateAsync`
- Produces: `PUT /api/profile/info` → 200 `ProfileDto`; `PUT /api/profile/skills` → 200 `ProfileDto`

- [ ] **Step 1: Write failing tests**

Add to `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`:

```csharp
[Fact]
public async Task PUT_ProfileInfo_UpdatesScalarFields()
{
    var payload = new
    {
        firstName = "Ada",
        lastName = "Lovelace",
        email = "ada@example.com",
        phone = "+41 79 000 00 00",
        location = "Zurich, Switzerland",
        summary = "Pioneer of computing."
    };

    var response = await _client.PutAsJsonAsync("/api/profile/info", payload);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
    profile!.FirstName.Should().Be("Ada");
    profile.LastName.Should().Be("Lovelace");
    profile.Summary.Should().Be("Pioneer of computing.");
}

[Fact]
public async Task PUT_ProfileInfo_Returns400_WhenRequiredFieldsMissing()
{
    var payload = new
    {
        firstName = "",
        lastName = "Lovelace",
        email = "ada@example.com",
        phone = "+41 79 000 00 00",
        location = (string?)null,
        summary = (string?)null
    };

    var response = await _client.PutAsJsonAsync("/api/profile/info", payload);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}

[Fact]
public async Task PUT_ProfileSkills_ReplacesSkillsList()
{
    var payload = new { skills = new[] { "C#", "Angular", "PostgreSQL" } };

    var response = await _client.PutAsJsonAsync("/api/profile/skills", payload);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
    profile!.Skills.Should().BeEquivalentTo(new[] { "C#", "Angular", "PostgreSQL" });
}
```

- [ ] **Step 2: Run tests — expect fail**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "PUT_Profile"
```

Expected: FAIL — 404 (endpoints do not exist yet).

- [ ] **Step 3: Create UpdateProfileInfoCommand**

`backend/Yaam.UseCases/Profile/Commands/UpdateProfileInfoCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands;

public record UpdateProfileInfoCommand(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Location,
    string? Summary) : IRequest<ProfileDto>;

public class UpdateProfileInfoCommandValidator : AbstractValidator<UpdateProfileInfoCommand>
{
    public UpdateProfileInfoCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Location).MaximumLength(200).When(x => x.Location is not null);
        RuleFor(x => x.Summary).MaximumLength(2000).When(x => x.Summary is not null);
    }
}

public class UpdateProfileInfoCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateProfileInfoCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpdateProfileInfoCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        profile.FirstName = command.FirstName;
        profile.LastName = command.LastName;
        profile.Email = command.Email;
        profile.Phone = command.Phone;
        profile.Location = command.Location;
        profile.Summary = command.Summary;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(profile);
    }
}
```

- [ ] **Step 4: Create UpdateProfileSkillsCommand**

`backend/Yaam.UseCases/Profile/Commands/UpdateProfileSkillsCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands;

public record UpdateProfileSkillsCommand(List<string> Skills) : IRequest<ProfileDto>;

public class UpdateProfileSkillsCommandValidator : AbstractValidator<UpdateProfileSkillsCommand>
{
    public UpdateProfileSkillsCommandValidator()
    {
        RuleForEach(x => x.Skills).NotEmpty().MaximumLength(100);
    }
}

public class UpdateProfileSkillsCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateProfileSkillsCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpdateProfileSkillsCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        profile.Skills = command.Skills;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(profile);
    }
}
```

- [ ] **Step 5: Add mutation actions to ProfileController**

Add these actions and input models to `backend/Yaam.API/Controllers/ProfileController.cs`:

```csharp
// add to existing using block:
using Yaam.UseCases.Profile.Commands;

// --- actions ---

[HttpPut("info")]
[EndpointName("UpdateProfileInfo")]
public async Task<IActionResult> UpdateInfo(
    [FromBody] UpdateProfileInfoInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateProfileInfoCommand(
            input.FirstName,
            input.LastName,
            input.Email,
            input.Phone,
            input.Location,
            input.Summary),
        cancellationToken);
    return Ok(result);
}

[HttpPut("skills")]
[EndpointName("UpdateProfileSkills")]
public async Task<IActionResult> UpdateSkills(
    [FromBody] UpdateProfileSkillsInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateProfileSkillsCommand(input.Skills),
        cancellationToken);
    return Ok(result);
}

// --- input models (add at bottom of file) ---

public record UpdateProfileInfoInputModel(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Location,
    string? Summary);

public record UpdateProfileSkillsInputModel(List<string> Skills);
```

- [ ] **Step 6: Run tests — expect pass**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "PUT_Profile"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/Yaam.UseCases/Profile/Commands/UpdateProfileInfoCommand.cs \
        backend/Yaam.UseCases/Profile/Commands/UpdateProfileSkillsCommand.cs \
        backend/Yaam.API/Controllers/ProfileController.cs \
        backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
git commit -m "feat: add PUT /api/profile/info and PUT /api/profile/skills"
```

---

### Task 2: Work Experience CRUD — commands, controller, tests

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/WorkExperiences/AddWorkExperienceCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/WorkExperiences/UpdateWorkExperienceCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/WorkExperiences/DeleteWorkExperienceCommand.cs`
- Modify: `backend/Yaam.API/Controllers/ProfileController.cs`
- Modify: `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`

**Interfaces:**
- Produces: `POST /api/profile/work-experiences` → 200 `WorkExperienceDto`
- Produces: `PUT /api/profile/work-experiences/{id}` → 200 `WorkExperienceDto`
- Produces: `DELETE /api/profile/work-experiences/{id}` → 204

- [ ] **Step 1: Write failing tests**

Add to `ProfileEndpointsTests.cs`:

```csharp
[Fact]
public async Task WorkExperienceCrudFlow()
{
    // Add
    var addPayload = new
    {
        company = "ACME Corp",
        title = "Software Engineer",
        startDate = "2022-01-01",
        endDate = (string?)null,
        description = "Built things."
    };
    var addResponse = await _client.PostAsJsonAsync("/api/profile/work-experiences", addPayload);
    addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var added = await addResponse.Content.ReadFromJsonAsync<WorkExperienceDto>();
    added!.Company.Should().Be("ACME Corp");

    // Update
    var updatePayload = new
    {
        company = "ACME Corp",
        title = "Senior Software Engineer",
        startDate = "2022-01-01",
        endDate = "2024-12-31",
        description = "Built more things."
    };
    var updateResponse = await _client.PutAsJsonAsync(
        $"/api/profile/work-experiences/{added.Id}", updatePayload);
    updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var updated = await updateResponse.Content.ReadFromJsonAsync<WorkExperienceDto>();
    updated!.Title.Should().Be("Senior Software Engineer");

    // Delete
    var deleteResponse = await _client.DeleteAsync($"/api/profile/work-experiences/{added.Id}");
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

    // Verify gone
    var profile = await (await _client.GetAsync("/api/profile"))
        .Content.ReadFromJsonAsync<ProfileDto>();
    profile!.WorkExperiences.Should().BeEmpty();
}

[Fact]
public async Task POST_WorkExperience_Returns400_WhenRequiredFieldsMissing()
{
    var payload = new { description = "No company or title" };
    var response = await _client.PostAsJsonAsync("/api/profile/work-experiences", payload);
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

- [ ] **Step 2: Run tests — expect fail**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "WorkExperience"
```

Expected: FAIL — 404.

- [ ] **Step 3: Create AddWorkExperienceCommand**

`backend/Yaam.UseCases/Profile/Commands/WorkExperiences/AddWorkExperienceCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.WorkExperiences;

public record AddWorkExperienceCommand(
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description) : IRequest<WorkExperienceDto>;

public class AddWorkExperienceCommandValidator : AbstractValidator<AddWorkExperienceCommand>
{
    public AddWorkExperienceCommandValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}

public class AddWorkExperienceCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddWorkExperienceCommand, WorkExperienceDto>
{
    public async Task<WorkExperienceDto> Handle(AddWorkExperienceCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new WorkExperience
        {
            ProfileId = profile.Id,
            Company = command.Company,
            Title = command.Title,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Description = command.Description,
        };
        profile.WorkExperiences.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 4: Create UpdateWorkExperienceCommand**

`backend/Yaam.UseCases/Profile/Commands/WorkExperiences/UpdateWorkExperienceCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.WorkExperiences;

public record UpdateWorkExperienceCommand(
    Guid Id,
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description) : IRequest<WorkExperienceDto>;

public class UpdateWorkExperienceCommandValidator : AbstractValidator<UpdateWorkExperienceCommand>
{
    public UpdateWorkExperienceCommandValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}

public class UpdateWorkExperienceCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateWorkExperienceCommand, WorkExperienceDto>
{
    public async Task<WorkExperienceDto> Handle(UpdateWorkExperienceCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.WorkExperiences.FirstOrDefault(w => w.Id == command.Id)
            ?? throw new NotFoundException(nameof(WorkExperience), command.Id);
        entry.Company = command.Company;
        entry.Title = command.Title;
        entry.StartDate = command.StartDate;
        entry.EndDate = command.EndDate;
        entry.Description = command.Description;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 5: Create DeleteWorkExperienceCommand**

`backend/Yaam.UseCases/Profile/Commands/WorkExperiences/DeleteWorkExperienceCommand.cs`
```csharp
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.WorkExperiences;

public record DeleteWorkExperienceCommand(Guid Id) : IRequest;

public class DeleteWorkExperienceCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteWorkExperienceCommand>
{
    public async Task Handle(DeleteWorkExperienceCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.WorkExperiences.FirstOrDefault(w => w.Id == command.Id)
            ?? throw new NotFoundException(nameof(WorkExperience), command.Id);
        profile.WorkExperiences.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Add Work Experience actions to ProfileController**

Add to `ProfileController.cs`:

```csharp
// add to usings:
using Yaam.UseCases.Profile.Commands.WorkExperiences;

// --- actions ---

[HttpPost("work-experiences")]
[EndpointName("AddWorkExperience")]
public async Task<IActionResult> AddWorkExperience(
    [FromBody] WorkExperienceInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new AddWorkExperienceCommand(
            input.Company, input.Title, input.StartDate, input.EndDate, input.Description),
        cancellationToken);
    return Ok(result);
}

[HttpPut("work-experiences/{id:guid}")]
[EndpointName("UpdateWorkExperience")]
public async Task<IActionResult> UpdateWorkExperience(
    Guid id, [FromBody] WorkExperienceInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateWorkExperienceCommand(
            id, input.Company, input.Title, input.StartDate, input.EndDate, input.Description),
        cancellationToken);
    return Ok(result);
}

[HttpDelete("work-experiences/{id:guid}")]
[EndpointName("DeleteWorkExperience")]
public async Task<IActionResult> DeleteWorkExperience(Guid id, CancellationToken cancellationToken)
{
    await mediator.Send(
        new DeleteWorkExperienceCommand(id),
        cancellationToken);
    return NoContent();
}

// --- input model (add at bottom of file) ---

public record WorkExperienceInputModel(
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description);
```

- [ ] **Step 7: Run tests — expect pass**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "WorkExperience"
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.UseCases/Profile/Commands/WorkExperiences \
        backend/Yaam.API/Controllers/ProfileController.cs \
        backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
git commit -m "feat: add Work Experience CRUD endpoints"
```

---

### Task 3: Education CRUD — commands, controller, tests

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/Education/AddEducationCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/Education/UpdateEducationCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/Education/DeleteEducationCommand.cs`
- Modify: `backend/Yaam.API/Controllers/ProfileController.cs`
- Modify: `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`

**Interfaces:**
- Produces: `POST /api/profile/education` → 200 `EducationDto`
- Produces: `PUT /api/profile/education/{id}` → 200 `EducationDto`
- Produces: `DELETE /api/profile/education/{id}` → 204

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task EducationCrudFlow()
{
    var addPayload = new
    {
        institution = "ETH Zurich",
        degree = "MSc",
        fieldOfStudy = "Computer Science",
        startDate = "2018-09-01",
        endDate = "2020-06-30"
    };
    var addResponse = await _client.PostAsJsonAsync("/api/profile/education", addPayload);
    addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var added = await addResponse.Content.ReadFromJsonAsync<EducationDto>();
    added!.Institution.Should().Be("ETH Zurich");

    var updatePayload = new { institution = "ETH Zurich", degree = "PhD", fieldOfStudy = "Computer Science", startDate = "2020-09-01", endDate = (string?)null };
    var updateResponse = await _client.PutAsJsonAsync($"/api/profile/education/{added.Id}", updatePayload);
    updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var updated = await updateResponse.Content.ReadFromJsonAsync<EducationDto>();
    updated!.Degree.Should().Be("PhD");

    var deleteResponse = await _client.DeleteAsync($"/api/profile/education/{added.Id}");
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
}
```

- [ ] **Step 2: Run — expect fail**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "EducationCrudFlow"
```

- [ ] **Step 3: Create AddEducationCommand**

`backend/Yaam.UseCases/Profile/Commands/Education/AddEducationCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Education;

public record AddEducationCommand(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate) : IRequest<EducationDto>;

public class AddEducationCommandValidator : AbstractValidator<AddEducationCommand>
{
    public AddEducationCommandValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).MaximumLength(200).When(x => x.Degree is not null);
        RuleFor(x => x.FieldOfStudy).MaximumLength(200).When(x => x.FieldOfStudy is not null);
    }
}

public class AddEducationCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddEducationCommand, EducationDto>
{
    public async Task<EducationDto> Handle(AddEducationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new Yaam.Domain.Entities.Education
        {
            ProfileId = profile.Id,
            Institution = command.Institution,
            Degree = command.Degree,
            FieldOfStudy = command.FieldOfStudy,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
        };
        profile.Educations.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 4: Create UpdateEducationCommand**

`backend/Yaam.UseCases/Profile/Commands/Education/UpdateEducationCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Education;

public record UpdateEducationCommand(
    Guid Id,
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate) : IRequest<EducationDto>;

public class UpdateEducationCommandValidator : AbstractValidator<UpdateEducationCommand>
{
    public UpdateEducationCommandValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).MaximumLength(200).When(x => x.Degree is not null);
        RuleFor(x => x.FieldOfStudy).MaximumLength(200).When(x => x.FieldOfStudy is not null);
    }
}

public class UpdateEducationCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateEducationCommand, EducationDto>
{
    public async Task<EducationDto> Handle(UpdateEducationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Educations.FirstOrDefault(e => e.Id == command.Id)
            ?? throw new NotFoundException(nameof(Yaam.Domain.Entities.Education), command.Id);
        entry.Institution = command.Institution;
        entry.Degree = command.Degree;
        entry.FieldOfStudy = command.FieldOfStudy;
        entry.StartDate = command.StartDate;
        entry.EndDate = command.EndDate;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 5: Create DeleteEducationCommand**

`backend/Yaam.UseCases/Profile/Commands/Education/DeleteEducationCommand.cs`
```csharp
using MediatR;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.Education;

public record DeleteEducationCommand(Guid Id) : IRequest;

public class DeleteEducationCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteEducationCommand>
{
    public async Task Handle(DeleteEducationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Educations.FirstOrDefault(e => e.Id == command.Id)
            ?? throw new NotFoundException(nameof(Yaam.Domain.Entities.Education), command.Id);
        profile.Educations.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Add Education actions to ProfileController**

```csharp
// add to usings:
using Yaam.UseCases.Profile.Commands.Education;

[HttpPost("education")]
[EndpointName("AddEducation")]
public async Task<IActionResult> AddEducation(
    [FromBody] EducationInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new AddEducationCommand(
            input.Institution, input.Degree, input.FieldOfStudy, input.StartDate, input.EndDate),
        cancellationToken);
    return Ok(result);
}

[HttpPut("education/{id:guid}")]
[EndpointName("UpdateEducation")]
public async Task<IActionResult> UpdateEducation(
    Guid id, [FromBody] EducationInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateEducationCommand(
            id, input.Institution, input.Degree, input.FieldOfStudy, input.StartDate, input.EndDate),
        cancellationToken);
    return Ok(result);
}

[HttpDelete("education/{id:guid}")]
[EndpointName("DeleteEducation")]
public async Task<IActionResult> DeleteEducation(Guid id, CancellationToken cancellationToken)
{
    await mediator.Send(new DeleteEducationCommand(id), cancellationToken);
    return NoContent();
}

public record EducationInputModel(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate);
```

- [ ] **Step 7: Run tests — expect pass**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "EducationCrudFlow"
```

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.UseCases/Profile/Commands/Education \
        backend/Yaam.API/Controllers/ProfileController.cs \
        backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
git commit -m "feat: add Education CRUD endpoints"
```

---

### Task 4: Language CRUD — commands, controller, tests

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/Languages/AddLanguageCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/Languages/UpdateLanguageCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/Languages/DeleteLanguageCommand.cs`
- Modify: `backend/Yaam.API/Controllers/ProfileController.cs`
- Modify: `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`

**Interfaces:**
- Produces: `POST /api/profile/languages` → 200 `LanguageDto`
- Produces: `PUT /api/profile/languages/{id}` → 200 `LanguageDto`
- Produces: `DELETE /api/profile/languages/{id}` → 204

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task LanguageCrudFlow()
{
    var addPayload = new { name = "German", proficiency = "Fluent" };
    var addResponse = await _client.PostAsJsonAsync("/api/profile/languages", addPayload);
    addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var added = await addResponse.Content.ReadFromJsonAsync<LanguageDto>();
    added!.Name.Should().Be("German");
    added.Proficiency.Should().Be("Fluent");

    var updatePayload = new { name = "German", proficiency = "Native" };
    var updateResponse = await _client.PutAsJsonAsync($"/api/profile/languages/{added.Id}", updatePayload);
    updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var updated = await updateResponse.Content.ReadFromJsonAsync<LanguageDto>();
    updated!.Proficiency.Should().Be("Native");

    var deleteResponse = await _client.DeleteAsync($"/api/profile/languages/{added.Id}");
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
}
```

- [ ] **Step 2: Run — expect fail**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "LanguageCrudFlow"
```

- [ ] **Step 3: Create AddLanguageCommand**

`backend/Yaam.UseCases/Profile/Commands/Languages/AddLanguageCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Languages;

public record AddLanguageCommand(string Name, LanguageProficiency Proficiency) : IRequest<LanguageDto>;

public class AddLanguageCommandValidator : AbstractValidator<AddLanguageCommand>
{
    public AddLanguageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Proficiency).IsInEnum();
    }
}

public class AddLanguageCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddLanguageCommand, LanguageDto>
{
    public async Task<LanguageDto> Handle(AddLanguageCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new Language
        {
            ProfileId = profile.Id,
            Name = command.Name,
            Proficiency = command.Proficiency,
        };
        profile.Languages.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 4: Create UpdateLanguageCommand**

`backend/Yaam.UseCases/Profile/Commands/Languages/UpdateLanguageCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Enums;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Languages;

public record UpdateLanguageCommand(Guid Id, string Name, LanguageProficiency Proficiency) : IRequest<LanguageDto>;

public class UpdateLanguageCommandValidator : AbstractValidator<UpdateLanguageCommand>
{
    public UpdateLanguageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Proficiency).IsInEnum();
    }
}

public class UpdateLanguageCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateLanguageCommand, LanguageDto>
{
    public async Task<LanguageDto> Handle(UpdateLanguageCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Languages.FirstOrDefault(l => l.Id == command.Id)
            ?? throw new NotFoundException(nameof(Language), command.Id);
        entry.Name = command.Name;
        entry.Proficiency = command.Proficiency;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 5: Create DeleteLanguageCommand**

`backend/Yaam.UseCases/Profile/Commands/Languages/DeleteLanguageCommand.cs`
```csharp
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.Languages;

public record DeleteLanguageCommand(Guid Id) : IRequest;

public class DeleteLanguageCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteLanguageCommand>
{
    public async Task Handle(DeleteLanguageCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Languages.FirstOrDefault(l => l.Id == command.Id)
            ?? throw new NotFoundException(nameof(Language), command.Id);
        profile.Languages.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Add Language actions to ProfileController**

```csharp
// add to usings:
using Yaam.Domain.Enums;
using Yaam.UseCases.Profile.Commands.Languages;

[HttpPost("languages")]
[EndpointName("AddLanguage")]
public async Task<IActionResult> AddLanguage(
    [FromBody] LanguageInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new AddLanguageCommand(input.Name, input.Proficiency),
        cancellationToken);
    return Ok(result);
}

[HttpPut("languages/{id:guid}")]
[EndpointName("UpdateLanguage")]
public async Task<IActionResult> UpdateLanguage(
    Guid id, [FromBody] LanguageInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateLanguageCommand(id, input.Name, input.Proficiency),
        cancellationToken);
    return Ok(result);
}

[HttpDelete("languages/{id:guid}")]
[EndpointName("DeleteLanguage")]
public async Task<IActionResult> DeleteLanguage(Guid id, CancellationToken cancellationToken)
{
    await mediator.Send(new DeleteLanguageCommand(id), cancellationToken);
    return NoContent();
}

public record LanguageInputModel(string Name, LanguageProficiency Proficiency);
```

- [ ] **Step 7: Run tests — expect pass**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "LanguageCrudFlow"
```

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.UseCases/Profile/Commands/Languages \
        backend/Yaam.API/Controllers/ProfileController.cs \
        backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
git commit -m "feat: add Language CRUD endpoints"
```

---

### Task 5: Certification CRUD — commands, controller, tests

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/Certifications/AddCertificationCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/Certifications/UpdateCertificationCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/Certifications/DeleteCertificationCommand.cs`
- Modify: `backend/Yaam.API/Controllers/ProfileController.cs`
- Modify: `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`

**Interfaces:**
- Produces: `POST /api/profile/certifications` → 200 `CertificationDto`
- Produces: `PUT /api/profile/certifications/{id}` → 200 `CertificationDto`
- Produces: `DELETE /api/profile/certifications/{id}` → 204

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task CertificationCrudFlow()
{
    var addPayload = new { name = "AWS Solutions Architect", issuer = "Amazon", date = "2023-06-15" };
    var addResponse = await _client.PostAsJsonAsync("/api/profile/certifications", addPayload);
    addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var added = await addResponse.Content.ReadFromJsonAsync<CertificationDto>();
    added!.Name.Should().Be("AWS Solutions Architect");

    var updatePayload = new { name = "AWS Solutions Architect Professional", issuer = "Amazon", date = "2024-01-01" };
    var updateResponse = await _client.PutAsJsonAsync($"/api/profile/certifications/{added.Id}", updatePayload);
    updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var updated = await updateResponse.Content.ReadFromJsonAsync<CertificationDto>();
    updated!.Name.Should().Be("AWS Solutions Architect Professional");

    var deleteResponse = await _client.DeleteAsync($"/api/profile/certifications/{added.Id}");
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
}
```

- [ ] **Step 2: Run — expect fail**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "CertificationCrudFlow"
```

- [ ] **Step 3: Create AddCertificationCommand**

`backend/Yaam.UseCases/Profile/Commands/Certifications/AddCertificationCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Certifications;

public record AddCertificationCommand(string Name, string? Issuer, DateOnly Date) : IRequest<CertificationDto>;

public class AddCertificationCommandValidator : AbstractValidator<AddCertificationCommand>
{
    public AddCertificationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Issuer).MaximumLength(200).When(x => x.Issuer is not null);
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class AddCertificationCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddCertificationCommand, CertificationDto>
{
    public async Task<CertificationDto> Handle(AddCertificationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new Certification
        {
            ProfileId = profile.Id,
            Name = command.Name,
            Issuer = command.Issuer,
            Date = command.Date,
        };
        profile.Certifications.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 4: Create UpdateCertificationCommand**

`backend/Yaam.UseCases/Profile/Commands/Certifications/UpdateCertificationCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Certifications;

public record UpdateCertificationCommand(Guid Id, string Name, string? Issuer, DateOnly Date) : IRequest<CertificationDto>;

public class UpdateCertificationCommandValidator : AbstractValidator<UpdateCertificationCommand>
{
    public UpdateCertificationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Issuer).MaximumLength(200).When(x => x.Issuer is not null);
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class UpdateCertificationCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateCertificationCommand, CertificationDto>
{
    public async Task<CertificationDto> Handle(UpdateCertificationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Certifications.FirstOrDefault(c => c.Id == command.Id)
            ?? throw new NotFoundException(nameof(Certification), command.Id);
        entry.Name = command.Name;
        entry.Issuer = command.Issuer;
        entry.Date = command.Date;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 5: Create DeleteCertificationCommand**

`backend/Yaam.UseCases/Profile/Commands/Certifications/DeleteCertificationCommand.cs`
```csharp
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.Certifications;

public record DeleteCertificationCommand(Guid Id) : IRequest;

public class DeleteCertificationCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteCertificationCommand>
{
    public async Task Handle(DeleteCertificationCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Certifications.FirstOrDefault(c => c.Id == command.Id)
            ?? throw new NotFoundException(nameof(Certification), command.Id);
        profile.Certifications.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Add Certification actions to ProfileController**

```csharp
using Yaam.UseCases.Profile.Commands.Certifications;

[HttpPost("certifications")]
[EndpointName("AddCertification")]
public async Task<IActionResult> AddCertification(
    [FromBody] CertificationInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new AddCertificationCommand(input.Name, input.Issuer, input.Date),
        cancellationToken);
    return Ok(result);
}

[HttpPut("certifications/{id:guid}")]
[EndpointName("UpdateCertification")]
public async Task<IActionResult> UpdateCertification(
    Guid id, [FromBody] CertificationInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateCertificationCommand(id, input.Name, input.Issuer, input.Date),
        cancellationToken);
    return Ok(result);
}

[HttpDelete("certifications/{id:guid}")]
[EndpointName("DeleteCertification")]
public async Task<IActionResult> DeleteCertification(Guid id, CancellationToken cancellationToken)
{
    await mediator.Send(new DeleteCertificationCommand(id), cancellationToken);
    return NoContent();
}

public record CertificationInputModel(string Name, string? Issuer, DateOnly Date);
```

- [ ] **Step 7: Run tests — expect pass**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "CertificationCrudFlow"
```

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.UseCases/Profile/Commands/Certifications \
        backend/Yaam.API/Controllers/ProfileController.cs \
        backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
git commit -m "feat: add Certification CRUD endpoints"
```

---

### Task 6: Profile Link CRUD — commands, controller, tests

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/Links/AddProfileLinkCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/Links/UpdateProfileLinkCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/Links/DeleteProfileLinkCommand.cs`
- Modify: `backend/Yaam.API/Controllers/ProfileController.cs`
- Modify: `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`

**Interfaces:**
- Produces: `POST /api/profile/links` → 200 `ProfileLinkDto`
- Produces: `PUT /api/profile/links/{id}` → 200 `ProfileLinkDto`
- Produces: `DELETE /api/profile/links/{id}` → 204

- [ ] **Step 1: Write failing test**

```csharp
[Fact]
public async Task ProfileLinkCrudFlow()
{
    var addPayload = new { label = "GitHub", url = "https://github.com/ada" };
    var addResponse = await _client.PostAsJsonAsync("/api/profile/links", addPayload);
    addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var added = await addResponse.Content.ReadFromJsonAsync<ProfileLinkDto>();
    added!.Label.Should().Be("GitHub");

    var updatePayload = new { label = "GitHub Profile", url = "https://github.com/ada" };
    var updateResponse = await _client.PutAsJsonAsync($"/api/profile/links/{added.Id}", updatePayload);
    updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var updated = await updateResponse.Content.ReadFromJsonAsync<ProfileLinkDto>();
    updated!.Label.Should().Be("GitHub Profile");

    var deleteResponse = await _client.DeleteAsync($"/api/profile/links/{added.Id}");
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
}
```

- [ ] **Step 2: Run — expect fail**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "ProfileLinkCrudFlow"
```

- [ ] **Step 3: Create AddProfileLinkCommand**

`backend/Yaam.UseCases/Profile/Commands/Links/AddProfileLinkCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Links;

public record AddProfileLinkCommand(string Label, string Url) : IRequest<ProfileLinkDto>;

public class AddProfileLinkCommandValidator : AbstractValidator<AddProfileLinkCommand>
{
    public AddProfileLinkCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500);
    }
}

public class AddProfileLinkCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddProfileLinkCommand, ProfileLinkDto>
{
    public async Task<ProfileLinkDto> Handle(AddProfileLinkCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new ProfileLink
        {
            ProfileId = profile.Id,
            Label = command.Label,
            Url = command.Url,
        };
        profile.Links.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 4: Create UpdateProfileLinkCommand**

`backend/Yaam.UseCases/Profile/Commands/Links/UpdateProfileLinkCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.Links;

public record UpdateProfileLinkCommand(Guid Id, string Label, string Url) : IRequest<ProfileLinkDto>;

public class UpdateProfileLinkCommandValidator : AbstractValidator<UpdateProfileLinkCommand>
{
    public UpdateProfileLinkCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(500);
    }
}

public class UpdateProfileLinkCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateProfileLinkCommand, ProfileLinkDto>
{
    public async Task<ProfileLinkDto> Handle(UpdateProfileLinkCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Links.FirstOrDefault(l => l.Id == command.Id)
            ?? throw new NotFoundException(nameof(ProfileLink), command.Id);
        entry.Label = command.Label;
        entry.Url = command.Url;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 5: Create DeleteProfileLinkCommand**

`backend/Yaam.UseCases/Profile/Commands/Links/DeleteProfileLinkCommand.cs`
```csharp
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.Links;

public record DeleteProfileLinkCommand(Guid Id) : IRequest;

public class DeleteProfileLinkCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteProfileLinkCommand>
{
    public async Task Handle(DeleteProfileLinkCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.Links.FirstOrDefault(l => l.Id == command.Id)
            ?? throw new NotFoundException(nameof(ProfileLink), command.Id);
        profile.Links.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Add Link actions to ProfileController**

```csharp
using Yaam.UseCases.Profile.Commands.Links;

[HttpPost("links")]
[EndpointName("AddProfileLink")]
public async Task<IActionResult> AddLink(
    [FromBody] ProfileLinkInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new AddProfileLinkCommand(input.Label, input.Url),
        cancellationToken);
    return Ok(result);
}

[HttpPut("links/{id:guid}")]
[EndpointName("UpdateProfileLink")]
public async Task<IActionResult> UpdateLink(
    Guid id, [FromBody] ProfileLinkInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateProfileLinkCommand(id, input.Label, input.Url),
        cancellationToken);
    return Ok(result);
}

[HttpDelete("links/{id:guid}")]
[EndpointName("DeleteProfileLink")]
public async Task<IActionResult> DeleteLink(Guid id, CancellationToken cancellationToken)
{
    await mediator.Send(new DeleteProfileLinkCommand(id), cancellationToken);
    return NoContent();
}

public record ProfileLinkInputModel(string Label, string Url);
```

- [ ] **Step 7: Run tests — expect pass**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "ProfileLinkCrudFlow"
```

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.UseCases/Profile/Commands/Links \
        backend/Yaam.API/Controllers/ProfileController.cs \
        backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
git commit -m "feat: add Profile Link CRUD endpoints"
```

---

### Task 7: Regenerate API client

**Files:**
- `frontend/src/app/generated/api/` — fully regenerated (never edit manually)

**Interfaces:**
- Produces: Generated `ProfileService` with all profile methods available for frontend tasks

- [ ] **Step 1: Start the backend**

```bash
dotnet run --project backend/Yaam.API
```

Confirm Scalar UI is accessible at `https://localhost:7131/scalar/v1` and all profile endpoints appear.

- [ ] **Step 2: Regenerate**

```bash
cd frontend && npm run generate:api
```

Expected: Files written to `src/app/generated/api/`. Verify that a `ProfileService` class exists in the output with methods for `getProfile`, `updateProfileInfo`, `updateProfileSkills`, `addWorkExperience`, `updateWorkExperience`, `deleteWorkExperience`, and equivalents for all other list types.

- [ ] **Step 3: Fix broken imports**

The old `src/app/generated/api.ts` is replaced by the new `src/app/generated/api/` directory. Update all existing component imports from:
```typescript
import { ApplicationsService } from '../../../../generated/api';
```
to the path the generator produced (typically `'../../../../generated/api/api'` or a named service file). Run:

```bash
cd frontend && npx tsc --noEmit
```

Fix any import errors until the build is clean.

- [ ] **Step 4: Build check**

```bash
cd frontend && npm run build
```

Expected: Build succeeded, no errors.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/generated frontend/src/app
git commit -m "chore: regenerate API client with profile endpoints"
```

---

### Task 8: ProfilePageComponent — wire changed outputs

**Files:**
- Modify: `frontend/src/app/features/profile/pages/profile-page/profile-page.component.ts`
- Modify: `frontend/src/app/features/profile/pages/profile-page/profile-page.component.html`

**Interfaces:**
- Produces: `onProfileChanged()` method that calls `profileResource.reload()`; all section components' `changed` output wired to it

- [ ] **Step 1: Update ProfilePageComponent**

`profile-page.component.ts` — add the reload handler:

```typescript
protected onProfileChanged(): void {
  this.profileResource.reload();
}
```

- [ ] **Step 2: Update template to wire changed outputs**

`profile-page.component.html` — add `(changed)="onProfileChanged()"` to every section:

```html
<app-profile-info-section [profile]="profile" (changed)="onProfileChanged()" />
<app-work-experience-section [items]="profile.workExperiences" (changed)="onProfileChanged()" />
<app-education-section [items]="profile.educations" (changed)="onProfileChanged()" />
<app-profile-skills-section [skills]="profile.skills" (changed)="onProfileChanged()" />
<app-language-section [items]="profile.languages" (changed)="onProfileChanged()" />
<app-certification-section [items]="profile.certifications" (changed)="onProfileChanged()" />
<app-profile-link-section [items]="profile.links" (changed)="onProfileChanged()" />
<app-custom-fields-section [fields]="profile.customFields" (changed)="onProfileChanged()" />
```

- [ ] **Step 3: Build check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: Errors for missing `changed` outputs on section components — these will be resolved in Tasks 9–15.

- [ ] **Step 4: Commit after all section components have `changed` output**

Defer this commit to after Task 15 when the build is clean.

---

### Task 9: Info section — edit mode

**Files:**
- Modify: `frontend/src/app/features/profile/components/profile-info-section/profile-info-section.component.ts`
- Modify: `frontend/src/app/features/profile/components/profile-info-section/profile-info-section.component.html`

**Interfaces:**
- Consumes: `ProfileService.updateProfileInfo(body)` from generated client
- Produces: `changed` output emitted after successful save; section toggles between read and edit mode

- [ ] **Step 1: Invoke daisyUI skill before writing HTML**

Run the `daisyui` skill to get current component guidance before implementing the template.

- [ ] **Step 2: Update ProfileInfoSectionComponent**

`profile-info-section.component.ts`
```typescript
import { Component, inject, input, linkedSignal, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
import { ProfileService } from '../../../../generated/api/api'; // adjust path to generated output
import { Profile } from '../../models/profile.model';

@Component({
  selector: 'app-profile-info-section',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './profile-info-section.component.html',
})
export class ProfileInfoSectionComponent {
  readonly profile = input.required<Profile>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  protected readonly editMode = signal(false);
  protected readonly saving = signal(false);
  protected readonly serverErrors = signal<Record<string, string[]>>({});

  protected readonly formModel = linkedSignal(() => ({
    firstName: this.profile().firstName ?? '',
    lastName: this.profile().lastName ?? '',
    email: this.profile().email ?? '',
    phone: this.profile().phone ?? '',
    location: this.profile().location ?? '',
    summary: this.profile().summary ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.firstName, { message: 'First name is required.' });
    required(f.lastName, { message: 'Last name is required.' });
    required(f.email, { message: 'Email is required.' });
    required(f.phone, { message: 'Phone is required.' });
  });

  protected onEdit(): void {
    this.editMode.set(true);
    this.serverErrors.set({});
  }

  protected onCancel(): void {
    this.editMode.set(false);
  }

  protected async onSave(): Promise<void> {
    await submit(this.fields, async () => {
      this.saving.set(true);
      try {
        const m = this.formModel();
        await firstValueFrom(
          this.profileService.updateProfileInfo({
            firstName: m.firstName,
            lastName: m.lastName,
            email: m.email,
            phone: m.phone,
            location: m.location || null,
            summary: m.summary || null,
          }),
        );
        this.editMode.set(false);
        this.changed.emit();
      } catch (err: unknown) {
        const apiErr = err as { errors?: Record<string, string[]> };
        this.serverErrors.set(apiErr?.errors ?? {});
      } finally {
        this.saving.set(false);
      }
    });
  }
}
```

- [ ] **Step 3: Update info section template**

`profile-info-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <div class="flex items-center justify-between">
      <h2 class="card-title text-lg">Contact Info</h2>
      @if (!editMode()) {
        <button class="btn btn-ghost btn-sm" (click)="onEdit()">Edit</button>
      }
    </div>

    @if (editMode()) {
      <form (ngSubmit)="onSave()" FormRoot [formModel]="fields">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-2">
          <div FormField="firstName">
            <label class="label"><span class="label-text">First name *</span></label>
            <input type="text" class="input input-bordered w-full" required
              [value]="formModel().firstName"
              (input)="formModel.update(m => ({ ...m, firstName: $any($event.target).value }))" />
          </div>
          <div FormField="lastName">
            <label class="label"><span class="label-text">Last name *</span></label>
            <input type="text" class="input input-bordered w-full" required
              [value]="formModel().lastName"
              (input)="formModel.update(m => ({ ...m, lastName: $any($event.target).value }))" />
          </div>
          <div FormField="email">
            <label class="label"><span class="label-text">Email *</span></label>
            <input type="email" class="input input-bordered w-full" required
              [value]="formModel().email"
              (input)="formModel.update(m => ({ ...m, email: $any($event.target).value }))" />
          </div>
          <div FormField="phone">
            <label class="label"><span class="label-text">Phone *</span></label>
            <input type="text" class="input input-bordered w-full" required
              [value]="formModel().phone"
              (input)="formModel.update(m => ({ ...m, phone: $any($event.target).value }))" />
          </div>
          <div class="sm:col-span-2" FormField="location">
            <label class="label"><span class="label-text">Location</span></label>
            <input type="text" class="input input-bordered w-full"
              [value]="formModel().location"
              (input)="formModel.update(m => ({ ...m, location: $any($event.target).value }))" />
          </div>
        </div>

        <div class="divider my-2"></div>
        <h2 class="font-semibold mb-1">Summary</h2>
        <div FormField="summary">
          <textarea class="textarea textarea-bordered w-full" rows="4"
            [value]="formModel().summary"
            (input)="formModel.update(m => ({ ...m, summary: $any($event.target).value }))"></textarea>
        </div>

        @if (serverErrors() | keyvalue; as errors) {
          @if (errors.length > 0) {
            <div class="alert alert-error mt-2">
              @for (e of errors; track e.key) {
                <p>{{ e.value.join(', ') }}</p>
              }
            </div>
          }
        }

        <div class="flex gap-2 justify-end mt-4">
          <button type="button" class="btn btn-ghost" (click)="onCancel()">Cancel</button>
          <button type="submit" class="btn btn-primary" [disabled]="saving()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            Save
          </button>
        </div>
      </form>
    } @else {
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-2">
        <div>
          <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">First name</p>
          @if (profile().firstName) { <p>{{ profile().firstName }}</p> }
          @else { <p class="text-base-content/40 italic">Not set</p> }
        </div>
        <div>
          <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">Last name</p>
          @if (profile().lastName) { <p>{{ profile().lastName }}</p> }
          @else { <p class="text-base-content/40 italic">Not set</p> }
        </div>
        <div>
          <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">Email</p>
          @if (profile().email) { <p>{{ profile().email }}</p> }
          @else { <p class="text-base-content/40 italic">Not set</p> }
        </div>
        <div>
          <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">Phone</p>
          @if (profile().phone) { <p>{{ profile().phone }}</p> }
          @else { <p class="text-base-content/40 italic">Not set</p> }
        </div>
        <div class="sm:col-span-2">
          <p class="text-xs text-base-content/50 uppercase tracking-wide mb-1">Location</p>
          @if (profile().location) { <p>{{ profile().location }}</p> }
          @else { <p class="text-base-content/40 italic">Not set</p> }
        </div>
      </div>
      <div class="divider my-2"></div>
      <h2 class="font-semibold mb-1">Summary</h2>
      @if (profile().summary) { <p class="whitespace-pre-wrap">{{ profile().summary }}</p> }
      @else { <p class="text-base-content/40 italic">No summary yet.</p> }
    }
  </div>
</div>
```

- [ ] **Step 4: Build check**

```bash
cd frontend && npx tsc --noEmit
```

- [ ] **Step 5: Manual verify**

Start the app (`npm start`), navigate to `/profile`, click "Edit" on the Contact Info section, fill in fields, save. Confirm values persist on reload.

---

### Task 10: Skills section — tag input edit

**Files:**
- Modify: `frontend/src/app/features/profile/components/profile-skills-section/profile-skills-section.component.ts`
- Modify: `frontend/src/app/features/profile/components/profile-skills-section/profile-skills-section.component.html`

**Interfaces:**
- Consumes: `ProfileService.updateProfileSkills(body)`
- Produces: `changed` output after save; tag input in edit mode

- [ ] **Step 1: Invoke daisyUI skill before writing HTML**

- [ ] **Step 2: Update ProfileSkillsSectionComponent**

`profile-skills-section.component.ts`
```typescript
import { Component, inject, input, linkedSignal, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileService } from '../../../../generated/api/api'; // adjust to generated path

@Component({
  selector: 'app-profile-skills-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-skills-section.component.html',
})
export class ProfileSkillsSectionComponent {
  readonly skills = input.required<string[]>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  protected readonly editMode = signal(false);
  protected readonly saving = signal(false);
  protected readonly skillInput = signal('');
  protected readonly editingSkills = linkedSignal(() => [...this.skills()]);

  protected onEdit(): void { this.editMode.set(true); }
  protected onCancel(): void { this.editMode.set(false); }

  protected addSkill(event: Event): void {
    event.preventDefault();
    const value = this.skillInput().trim().replace(/,$/, '');
    if (value && !this.editingSkills().includes(value)) {
      this.editingSkills.update(s => [...s, value]);
    }
    this.skillInput.set('');
  }

  protected removeSkill(skill: string): void {
    this.editingSkills.update(s => s.filter(x => x !== skill));
  }

  protected async onSave(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.profileService.updateProfileSkills({ skills: this.editingSkills() }),
      );
      this.editMode.set(false);
      this.changed.emit();
    } finally {
      this.saving.set(false);
    }
  }
}
```

- [ ] **Step 3: Update skills section template**

`profile-skills-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <div class="flex items-center justify-between">
      <h2 class="card-title text-lg">Skills</h2>
      @if (!editMode()) {
        <button class="btn btn-ghost btn-sm" (click)="onEdit()">Edit</button>
      }
    </div>

    @if (editMode()) {
      <div class="mt-2">
        <div class="flex flex-wrap gap-2 mb-3">
          @for (skill of editingSkills(); track skill) {
            <span class="badge badge-outline gap-1">
              {{ skill }}
              <button type="button" class="btn btn-ghost btn-xs p-0 min-h-0 h-auto"
                (click)="removeSkill(skill)">✕</button>
            </span>
          }
          @if (editingSkills().length === 0) {
            <p class="text-base-content/40 italic text-sm">No skills yet — type one below.</p>
          }
        </div>
        <input type="text" class="input input-bordered input-sm w-full"
          placeholder="Type a skill and press Enter"
          [value]="skillInput()"
          (input)="skillInput.set($any($event.target).value)"
          (keydown.enter)="addSkill($event)"
          (keydown.comma)="addSkill($event)" />
        <div class="flex gap-2 justify-end mt-3">
          <button type="button" class="btn btn-ghost btn-sm" (click)="onCancel()">Cancel</button>
          <button type="button" class="btn btn-primary btn-sm" [disabled]="saving()" (click)="onSave()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            Save
          </button>
        </div>
      </div>
    } @else {
      @if (skills().length > 0) {
        <div class="flex flex-wrap gap-2 mt-2">
          @for (skill of skills(); track skill) {
            <span class="badge badge-outline">{{ skill }}</span>
          }
        </div>
      } @else {
        <p class="text-base-content/40 italic">No skills added yet.</p>
      }
    }
  </div>
</div>
```

- [ ] **Step 4: Manual verify**

Edit skills section — add tags with Enter and comma, remove with ✕, save and confirm persistence.

---

### Task 11: Work Experience section + modal

**Files:**
- Create: `frontend/src/app/features/profile/components/work-experience-modal/work-experience-modal.component.ts`
- Create: `frontend/src/app/features/profile/components/work-experience-modal/work-experience-modal.component.html`
- Modify: `frontend/src/app/features/profile/components/work-experience-section/work-experience-section.component.ts`
- Modify: `frontend/src/app/features/profile/components/work-experience-section/work-experience-section.component.html`

**Interfaces:**
- Modal consumes: `open` input, `item` input (null = add, non-null = edit), emits `saved` with form data, emits `dismissed`
- Section consumes: `ProfileService` for CRUD; emits `changed`

- [ ] **Step 1: Invoke daisyUI skill before writing HTML**

- [ ] **Step 2: Create WorkExperienceModalComponent**

`work-experience-modal.component.ts`
```typescript
import { Component, input, linkedSignal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
import { WorkExperience } from '../../models/profile.model';

export interface WorkExperienceFormData {
  company: string;
  title: string;
  startDate: string;
  endDate: string;
  description: string;
}

@Component({
  selector: 'app-work-experience-modal',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './work-experience-modal.component.html',
})
export class WorkExperienceModalComponent {
  readonly open = input.required<boolean>();
  readonly item = input<WorkExperience | null>(null);
  readonly saved = output<WorkExperienceFormData>();
  readonly dismissed = output<void>();

  protected readonly formModel = linkedSignal<WorkExperienceFormData>(() => ({
    company: this.item()?.company ?? '',
    title: this.item()?.title ?? '',
    startDate: this.item()?.startDate ?? '',
    endDate: this.item()?.endDate ?? '',
    description: this.item()?.description ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.company, { message: 'Company is required.' });
    required(f.title, { message: 'Title is required.' });
    required(f.startDate, { message: 'Start date is required.' });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.saved.emit(this.formModel());
    });
  }

  protected onDismiss(): void { this.dismissed.emit(); }

  get isEditMode(): boolean { return this.item() !== null; }
}
```

`work-experience-modal.component.html`
```html
@if (open()) {
  <div class="modal modal-open">
    <div class="modal-box" FormRoot [formModel]="fields">
      <h3 class="font-bold text-lg mb-4">{{ isEditMode ? 'Edit' : 'Add' }} Work Experience</h3>

      <div class="flex flex-col gap-3">
        <div class="form-control" FormField="company">
          <label class="label"><span class="label-text">Company *</span></label>
          <input type="text" class="input input-bordered"
            [value]="formModel().company"
            (input)="formModel.update(m => ({ ...m, company: $any($event.target).value }))" />
        </div>
        <div class="form-control" FormField="title">
          <label class="label"><span class="label-text">Title *</span></label>
          <input type="text" class="input input-bordered"
            [value]="formModel().title"
            (input)="formModel.update(m => ({ ...m, title: $any($event.target).value }))" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="form-control" FormField="startDate">
            <label class="label"><span class="label-text">Start date *</span></label>
            <input type="date" class="input input-bordered"
              [value]="formModel().startDate"
              (input)="formModel.update(m => ({ ...m, startDate: $any($event.target).value }))" />
          </div>
          <div class="form-control" FormField="endDate">
            <label class="label"><span class="label-text">End date</span></label>
            <input type="date" class="input input-bordered"
              [value]="formModel().endDate"
              (input)="formModel.update(m => ({ ...m, endDate: $any($event.target).value }))" />
          </div>
        </div>
        <div class="form-control" FormField="description">
          <label class="label"><span class="label-text">Description</span></label>
          <textarea class="textarea textarea-bordered" rows="3"
            [value]="formModel().description"
            (input)="formModel.update(m => ({ ...m, description: $any($event.target).value }))"></textarea>
        </div>
      </div>

      <div class="modal-action">
        <button type="button" class="btn btn-ghost" (click)="onDismiss()">Cancel</button>
        <button type="button" class="btn btn-primary" (click)="onSubmit()">Save</button>
      </div>
    </div>
    <div class="modal-backdrop" (click)="onDismiss()"></div>
  </div>
}
```

- [ ] **Step 3: Update WorkExperienceSectionComponent**

`work-experience-section.component.ts`
```typescript
import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileService } from '../../../../generated/api/api'; // adjust to generated path
import { WorkExperience } from '../../models/profile.model';
import { WorkExperienceModalComponent, WorkExperienceFormData } from '../work-experience-modal/work-experience-modal.component';

@Component({
  selector: 'app-work-experience-section',
  standalone: true,
  imports: [CommonModule, WorkExperienceModalComponent],
  templateUrl: './work-experience-section.component.html',
})
export class WorkExperienceSectionComponent {
  readonly items = input.required<WorkExperience[]>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  protected readonly modalOpen = signal(false);
  protected readonly editingItem = signal<WorkExperience | null>(null);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modalOpen.set(true);
  }

  protected onEdit(item: WorkExperience): void {
    this.editingItem.set(item);
    this.modalOpen.set(true);
  }

  protected onModalDismissed(): void {
    this.modalOpen.set(false);
    this.editingItem.set(null);
  }

  protected async onSaved(data: WorkExperienceFormData): Promise<void> {
    const id = this.editingItem()?.id;
    this.saving.set(true);
    try {
      const payload = {
        company: data.company,
        title: data.title,
        startDate: data.startDate,
        endDate: data.endDate || null,
        description: data.description || null,
      };
      if (id) {
        await firstValueFrom(this.profileService.updateWorkExperience(id, payload));
      } else {
        await firstValueFrom(this.profileService.addWorkExperience(payload));
      }
      this.modalOpen.set(false);
      this.editingItem.set(null);
      this.changed.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected onDeleteStart(id: string): void { this.deletingId.set(id); }
  protected onDeleteCancel(): void { this.deletingId.set(null); }

  protected async onDeleteConfirm(id: string): Promise<void> {
    await firstValueFrom(this.profileService.deleteWorkExperience(id));
    this.deletingId.set(null);
    this.changed.emit();
  }
}
```

`work-experience-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <div class="flex items-center justify-between">
      <h2 class="card-title text-lg">Work Experience</h2>
      <button class="btn btn-ghost btn-sm" (click)="onAdd()">+ Add</button>
    </div>

    @if (items().length > 0) {
      <ul class="flex flex-col gap-4 mt-2">
        @for (item of items(); track item.id) {
          <li class="border-l-2 border-base-300 pl-4">
            <div class="flex items-start justify-between gap-2">
              <div>
                <p class="font-semibold">{{ item.title }} — {{ item.company }}</p>
                <p class="text-sm text-base-content/60">
                  {{ item.startDate }} – {{ item.endDate ?? 'Present' }}
                </p>
                @if (item.description) {
                  <p class="text-sm mt-1 whitespace-pre-wrap">{{ item.description }}</p>
                }
              </div>
              <div class="flex gap-1 shrink-0">
                @if (deletingId() === item.id) {
                  <button class="btn btn-error btn-xs" (click)="onDeleteConfirm(item.id)">Delete</button>
                  <button class="btn btn-ghost btn-xs" (click)="onDeleteCancel()">Cancel</button>
                } @else {
                  <button class="btn btn-ghost btn-xs" (click)="onEdit(item)">Edit</button>
                  <button class="btn btn-ghost btn-xs text-error" (click)="onDeleteStart(item.id)">Delete</button>
                }
              </div>
            </div>
          </li>
        }
      </ul>
    } @else {
      <p class="text-base-content/40 italic mt-2">No work experience added yet.</p>
    }
  </div>
</div>

<app-work-experience-modal
  [open]="modalOpen()"
  [item]="editingItem()"
  (saved)="onSaved($event)"
  (dismissed)="onModalDismissed()" />
```

- [ ] **Step 4: Manual verify**

Navigate to `/profile`. Add a work experience, edit it, delete it. Confirm the two-step delete (first click shows confirm/cancel, second click deletes).

---

### Task 12: Education section + modal

Follow the exact same pattern as Task 11. Field differences:

**Modal form fields:** Institution (required), Degree (optional text), Field of Study (optional text), Start Date (optional date), End Date (optional date).

**`EducationFormData`:**
```typescript
export interface EducationFormData {
  institution: string;
  degree: string;
  fieldOfStudy: string;
  startDate: string;
  endDate: string;
}
```

**Validator rules in modal:**
```typescript
required(f.institution, { message: 'Institution is required.' });
```

**Section API calls:**
```typescript
// add:
this.profileService.addEducation({ institution, degree: degree || null, fieldOfStudy: fieldOfStudy || null, startDate: startDate || null, endDate: endDate || null })
// update:
this.profileService.updateEducation(id, { ... })
// delete:
this.profileService.deleteEducation(id)
```

**Display (read-only):** institution name bold, degree + fieldOfStudy on second line, dates on third line — same pattern as Story 1 template.

- [ ] **Step 1: Invoke daisyUI skill before writing HTML**
- [ ] **Step 2: Create EducationModalComponent** (ts + html following Task 11 pattern with Education fields)
- [ ] **Step 3: Update EducationSectionComponent** (ts + html following Task 11 pattern)
- [ ] **Step 4: Manual verify** — add, edit, delete an education entry

---

### Task 13: Language section + modal

Follow the exact same pattern as Task 11. Field differences:

**Modal form fields:** Name (required text), Proficiency (required select from `ALL_LANGUAGE_PROFICIENCIES`).

**`LanguageFormData`:**
```typescript
export interface LanguageFormData {
  name: string;
  proficiency: string;
}
```

**Validator rules in modal:**
```typescript
required(f.name, { message: 'Language name is required.' });
required(f.proficiency, { message: 'Proficiency is required.' });
```

**Section API calls:**
```typescript
this.profileService.addLanguage({ name, proficiency })
this.profileService.updateLanguage(id, { name, proficiency })
this.profileService.deleteLanguage(id)
```

**Proficiency select in modal template:**
```html
<div class="form-control" FormField="proficiency">
  <label class="label"><span class="label-text">Proficiency *</span></label>
  <select class="select select-bordered"
    [value]="formModel().proficiency"
    (change)="formModel.update(m => ({ ...m, proficiency: $any($event.target).value }))">
    <option value="">Select proficiency</option>
    @for (p of allProficiencies; track p) {
      <option [value]="p">{{ proficiencyLabels[p] }}</option>
    }
  </select>
</div>
```

Import `ALL_LANGUAGE_PROFICIENCIES` and `LANGUAGE_PROFICIENCY_LABELS` from `profile.model.ts` into the modal component.

- [ ] **Step 1: Invoke daisyUI skill before writing HTML**
- [ ] **Step 2: Create LanguageModalComponent** (ts + html)
- [ ] **Step 3: Update LanguageSectionComponent** (ts + html)
- [ ] **Step 4: Manual verify** — add, edit, delete a language entry

---

### Task 14: Certification section + modal

Follow the exact same pattern as Task 11. Field differences:

**Modal form fields:** Name (required), Issuer (optional text), Date (required date).

**`CertificationFormData`:**
```typescript
export interface CertificationFormData {
  name: string;
  issuer: string;
  date: string;
}
```

**Validator rules:**
```typescript
required(f.name, { message: 'Certification name is required.' });
required(f.date, { message: 'Date is required.' });
```

**Section API calls:**
```typescript
this.profileService.addCertification({ name, issuer: issuer || null, date })
this.profileService.updateCertification(id, { name, issuer: issuer || null, date })
this.profileService.deleteCertification(id)
```

- [ ] **Step 1: Invoke daisyUI skill before writing HTML**
- [ ] **Step 2: Create CertificationModalComponent** (ts + html)
- [ ] **Step 3: Update CertificationSectionComponent** (ts + html)
- [ ] **Step 4: Manual verify**

---

### Task 15: Profile Link section + modal

Follow the exact same pattern as Task 11. Field differences:

**Modal form fields:** Label (required), URL (required).

**`ProfileLinkFormData`:**
```typescript
export interface ProfileLinkFormData {
  label: string;
  url: string;
}
```

**Validator rules:**
```typescript
required(f.label, { message: 'Label is required.' });
required(f.url, { message: 'URL is required.' });
```

**Section API calls:**
```typescript
this.profileService.addLink({ label, url })
this.profileService.updateLink(id, { label, url })
this.profileService.deleteLink(id)
```

- [ ] **Step 1: Invoke daisyUI skill before writing HTML**
- [ ] **Step 2: Create ProfileLinkModalComponent** (ts + html)
- [ ] **Step 3: Update ProfileLinkSectionComponent** (ts + html)
- [ ] **Step 4: Manual verify**

- [ ] **Step 5: Full build + final commit**

```bash
cd frontend && npm run build
```

Expected: Clean build, no errors.

```bash
git add frontend/src/app/features/profile
git commit -m "feat: profile edit — info section, skills tag input, list item modals for all 5 types"
```
