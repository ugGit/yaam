# DTO / ViewModel Layer Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a clean two-layer output type model: `*Dto` types stay in `Yaam.UseCases` (unchanged), new `*ViewModel` types live in `Yaam.API`, and API-layer mappers convert between them before controllers return a response.

**Architecture:** MediatR handlers continue to return `*Dto` records. Each controller calls an API-layer mapper to convert the DTO to a `*ViewModel` before calling `Ok()` / `Created()`. The frontend regenerates from the updated OpenAPI spec and sees `*ViewModel` type names. The ViewModel structure mirrors the DTO structure today but is free to diverge in the future.

**Tech Stack:** .NET 9 / C# (backend), Angular + TypeScript (frontend), MediatR, OpenAPI generator (`npm run generate:api` from `frontend/`).

## Global Constraints

- `Yaam.UseCases` layer: **zero changes** — all `*Dto` files, commands, queries, and the use-case mappers remain exactly as they are.
- Never edit files under `frontend/src/app/generated/` by hand — regenerate only.
- New ViewModel files go in `backend/Yaam.API/` under feature sub-folders.
- Mappers in `Yaam.API` are `internal static` classes, co-located with their ViewModels.

---

## New File Structure in Yaam.API

```
backend/Yaam.API/
  Applications/
    ApplicationViewModel.cs          ← ApplicationViewModel, ApplicationSummaryViewModel, ApplicationNoteViewModel
    ApplicationViewModelMapper.cs    ← maps ApplicationDto / ApplicationSummaryDto / ApplicationNoteDto → ViewModels
  Profile/
    ProfileViewModel.cs              ← ProfileViewModel, WorkExperienceViewModel, EducationViewModel,
                                       LanguageViewModel, CertificationViewModel, ProfileLinkViewModel, CustomFieldViewModel
    ProfileViewModelMapper.cs        ← maps ProfileDto and sub-DTOs → ViewModels
```

Controllers import from these namespaces and call the mapper after every `mediator.Send(...)`.

---

## Files Created

- `backend/Yaam.API/Applications/ApplicationViewModel.cs`
- `backend/Yaam.API/Applications/ApplicationViewModelMapper.cs`
- `backend/Yaam.API/Profile/ProfileViewModel.cs`
- `backend/Yaam.API/Profile/ProfileViewModelMapper.cs`

## Files Modified

- `backend/Yaam.API/Controllers/ApplicationsController.cs` — map DTOs to ViewModels
- `backend/Yaam.API/Controllers/ApplicationNotesController.cs` — map DTOs to ViewModels
- `backend/Yaam.API/Controllers/ProfileController.cs` — map DTOs to ViewModels
- `frontend/src/app/generated/**` — regenerated (never edit by hand)
- `frontend/src/app/features/applications/models/application.model.ts`
- `frontend/src/app/features/applications/models/application-note.model.ts`
- `frontend/src/app/features/profile/components/**/*.ts` (15 component files)
- `frontend/src/app/features/profile/pages/profile-page/profile-page.component.ts`
- `CLAUDE.md`

## Files Unchanged

Everything in `Yaam.UseCases` — all `*Dto` records, mappers, commands, queries, validators — stays exactly as written today.

---

## Task 1: Create the feature branch

- [ ] **Step 1: Create and switch to branch**

```bash
git checkout -b refactor/dto-to-viewmodel
```

Expected: `Switched to a new branch 'refactor/dto-to-viewmodel'`

---

## Task 2: Create Application ViewModels and API-layer mapper

**Files:**
- Create: `backend/Yaam.API/Applications/ApplicationViewModel.cs`
- Create: `backend/Yaam.API/Applications/ApplicationViewModelMapper.cs`

- [ ] **Step 1: Create the Applications sub-folder and ApplicationViewModel.cs**

```bash
mkdir -p backend/Yaam.API/Applications
```

File content for `backend/Yaam.API/Applications/ApplicationViewModel.cs`:

```csharp
using Yaam.Domain.Enums;

namespace Yaam.API.Applications;

public record ApplicationViewModel(
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
    List<ApplicationNoteViewModel> Notes);

public record ApplicationSummaryViewModel(
    Guid Id,
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status);

public record ApplicationNoteViewModel(
    Guid Id,
    string Body,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

- [ ] **Step 2: Create ApplicationViewModelMapper.cs**

File content for `backend/Yaam.API/Applications/ApplicationViewModelMapper.cs`:

```csharp
using Yaam.UseCases.Applications.Dtos;

namespace Yaam.API.Applications;

internal static class ApplicationViewModelMapper
{
    internal static ApplicationViewModel ToViewModel(ApplicationDto dto) => new(
        dto.Id,
        dto.CompanyName,
        dto.Role,
        dto.DateApplied,
        dto.Status,
        dto.ContactName,
        dto.ContactEmail,
        dto.ContactPhone,
        dto.JobPosting,
        dto.CreatedAt,
        dto.UpdatedAt,
        dto.Notes.Select(ToViewModel).ToList());

    internal static ApplicationSummaryViewModel ToViewModel(ApplicationSummaryDto dto) =>
        new(dto.Id, dto.CompanyName, dto.Role, dto.DateApplied, dto.Status);

    internal static ApplicationNoteViewModel ToViewModel(ApplicationNoteDto dto) =>
        new(dto.Id, dto.Body, dto.CreatedAt, dto.UpdatedAt);
}
```

- [ ] **Step 3: Verify the two new files compile**

```bash
dotnet build backend/Yaam.API/Yaam.API.csproj
```

Expected: Build succeeded, 0 errors.

---

## Task 3: Update ApplicationsController and ApplicationNotesController to return ViewModels

**Files:**
- Modify: `backend/Yaam.API/Controllers/ApplicationsController.cs`
- Modify: `backend/Yaam.API/Controllers/ApplicationNotesController.cs`

- [ ] **Step 1: Update ApplicationsController.cs**

The controller gets a DTO from MediatR and immediately maps it to a ViewModel before returning. Replace the entire file:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.API.Applications;
using Yaam.Domain.Common;
using Yaam.Domain.Enums;
using Yaam.UseCases.Applications.Commands;
using Yaam.UseCases.Applications.Queries;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("ListApplications")]
    public async Task<ActionResult<List<ApplicationSummaryViewModel>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] ApplicationStatus? status,
        [FromQuery] string sort = ApplicationSortFields.DateApplied,
        [FromQuery] string order = "desc")
    {
        var sortField = sort switch
        {
            ApplicationSortFields.CompanyName => ApplicationSortField.CompanyName,
            _ => ApplicationSortField.DateApplied,
        };
        var result = await mediator.Send(new GetApplicationsQuery(status, sortField, order), cancellationToken);
        return Ok(result.Select(ApplicationViewModelMapper.ToViewModel).ToList());
    }

    [HttpGet("{id:guid}")]
    [EndpointName("GetApplication")]
    public async Task<ActionResult<ApplicationViewModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetApplicationByIdQuery(id),
            cancellationToken);
        return Ok(ApplicationViewModelMapper.ToViewModel(result));
    }

    [HttpPost]
    [EndpointName("CreateApplication")]
    public async Task<ActionResult<ApplicationViewModel>> Create(
        [FromBody] CreateApplicationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateApplicationCommand(
                input.CompanyName,
                input.Role,
                input.DateApplied,
                input.Status,
                input.ContactName,
                input.ContactEmail,
                input.ContactPhone,
                input.JobPosting),
            cancellationToken);
        var viewModel = ApplicationViewModelMapper.ToViewModel(result);
        return CreatedAtAction(nameof(GetById), new { id = viewModel.Id }, viewModel);
    }

    [HttpPut("{id:guid}")]
    [EndpointName("UpdateApplication")]
    public async Task<ActionResult<ApplicationViewModel>> Update(
        Guid id, [FromBody] UpdateApplicationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateApplicationCommand(
                id,
                input.CompanyName,
                input.Role,
                input.DateApplied,
                input.Status,
                input.ContactName,
                input.ContactEmail,
                input.ContactPhone,
                input.JobPosting),
            cancellationToken);
        return Ok(ApplicationViewModelMapper.ToViewModel(result));
    }

    [HttpPatch("{id:guid}/status")]
    [EndpointName("PatchApplicationStatus")]
    public async Task<ActionResult<ApplicationViewModel>> UpdateStatus(
        Guid id, [FromBody] UpdateApplicationStatusInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateApplicationStatusCommand(id, input.Status),
            cancellationToken);
        return Ok(ApplicationViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("{id:guid}")]
    [EndpointName("DeleteApplication")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteApplicationCommand(id),
            cancellationToken);
        return NoContent();
    }
}

public record CreateApplicationInputModel(
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting);

public record UpdateApplicationInputModel(
    string CompanyName,
    string Role,
    DateOnly? DateApplied,
    ApplicationStatus Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? JobPosting);

public record UpdateApplicationStatusInputModel(ApplicationStatus Status);
```

- [ ] **Step 2: Update ApplicationNotesController.cs**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.API.Applications;
using Yaam.UseCases.Applications.Commands.Notes;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/applications/{applicationId:guid}/notes")]
public class ApplicationNotesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [EndpointName("AddApplicationNote")]
    public async Task<ActionResult<ApplicationNoteViewModel>> Add(
        Guid applicationId, [FromBody] CreateApplicationNoteInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddApplicationNoteCommand(applicationId, input.Body),
            cancellationToken);
        var viewModel = ApplicationViewModelMapper.ToViewModel(result);
        return Created($"api/applications/{applicationId}/notes/{viewModel.Id}", viewModel);
    }

    [HttpPut("{noteId:guid}")]
    [EndpointName("UpdateApplicationNote")]
    public async Task<ActionResult<ApplicationNoteViewModel>> Update(
        Guid applicationId, Guid noteId,
        [FromBody] UpdateApplicationNoteInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateApplicationNoteCommand(applicationId, noteId, input.Body),
            cancellationToken);
        return Ok(ApplicationViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("{noteId:guid}")]
    [EndpointName("DeleteApplicationNote")]
    public async Task<IActionResult> Delete(
        Guid applicationId, Guid noteId, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteApplicationNoteCommand(applicationId, noteId),
            cancellationToken);
        return NoContent();
    }
}

public record CreateApplicationNoteInputModel(string Body);
public record UpdateApplicationNoteInputModel(string Body);
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build backend/Yaam.API/Yaam.API.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add backend/Yaam.API/Applications/ backend/Yaam.API/Controllers/ApplicationsController.cs backend/Yaam.API/Controllers/ApplicationNotesController.cs
git commit -m "refactor: add Application ViewModels and API-layer mapper, update controllers"
```

---

## Task 4: Create Profile ViewModels and API-layer mapper

**Files:**
- Create: `backend/Yaam.API/Profile/ProfileViewModel.cs`
- Create: `backend/Yaam.API/Profile/ProfileViewModelMapper.cs`

- [ ] **Step 1: Create the Profile sub-folder and ProfileViewModel.cs**

```bash
mkdir -p backend/Yaam.API/Profile
```

File content for `backend/Yaam.API/Profile/ProfileViewModel.cs`:

```csharp
using Yaam.Domain.Enums;

namespace Yaam.API.Profile;

public record ProfileViewModel(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Location,
    string? Summary,
    List<WorkExperienceViewModel> WorkExperiences,
    List<EducationViewModel> Educations,
    List<string> Skills,
    List<LanguageViewModel> Languages,
    List<CertificationViewModel> Certifications,
    List<ProfileLinkViewModel> Links,
    List<CustomFieldViewModel> CustomFields);

public record WorkExperienceViewModel(
    Guid Id,
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description);

public record EducationViewModel(
    Guid Id,
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate);

public record LanguageViewModel(Guid Id, string Name, LanguageProficiency Proficiency);

public record CertificationViewModel(Guid Id, string Name, string? Issuer, DateOnly Date);

public record ProfileLinkViewModel(Guid Id, string Label, string Url);

public record CustomFieldViewModel(Guid Id, string Label, string Value);
```

- [ ] **Step 2: Create ProfileViewModelMapper.cs**

File content for `backend/Yaam.API/Profile/ProfileViewModelMapper.cs`:

```csharp
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.API.Profile;

internal static class ProfileViewModelMapper
{
    internal static ProfileViewModel ToViewModel(ProfileDto dto) => new(
        dto.Id,
        dto.FirstName,
        dto.LastName,
        dto.Email,
        dto.Phone,
        dto.Location,
        dto.Summary,
        dto.WorkExperiences.Select(ToViewModel).ToList(),
        dto.Educations.Select(ToViewModel).ToList(),
        dto.Skills,
        dto.Languages.Select(ToViewModel).ToList(),
        dto.Certifications.Select(ToViewModel).ToList(),
        dto.Links.Select(ToViewModel).ToList(),
        dto.CustomFields.Select(ToViewModel).ToList());

    internal static WorkExperienceViewModel ToViewModel(WorkExperienceDto dto) =>
        new(dto.Id, dto.Company, dto.Title, dto.StartDate, dto.EndDate, dto.Description);

    internal static EducationViewModel ToViewModel(EducationDto dto) =>
        new(dto.Id, dto.Institution, dto.Degree, dto.FieldOfStudy, dto.StartDate, dto.EndDate);

    internal static LanguageViewModel ToViewModel(LanguageDto dto) =>
        new(dto.Id, dto.Name, dto.Proficiency);

    internal static CertificationViewModel ToViewModel(CertificationDto dto) =>
        new(dto.Id, dto.Name, dto.Issuer, dto.Date);

    internal static ProfileLinkViewModel ToViewModel(ProfileLinkDto dto) =>
        new(dto.Id, dto.Label, dto.Url);

    internal static CustomFieldViewModel ToViewModel(CustomFieldDto dto) =>
        new(dto.Id, dto.Label, dto.Value);
}
```

- [ ] **Step 3: Verify the two new files compile**

```bash
dotnet build backend/Yaam.API/Yaam.API.csproj
```

Expected: Build succeeded, 0 errors.

---

## Task 5: Update ProfileController to return ViewModels

**File:** `backend/Yaam.API/Controllers/ProfileController.cs`

The controller currently returns `IActionResult` for most endpoints and `ActionResult<ProfileDto>` for Get. Each must now return the appropriate ViewModel type. Replace the entire file:

- [ ] **Step 1: Replace ProfileController.cs**

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Yaam.API.Profile;
using Yaam.Domain.Enums;
using Yaam.UseCases.Profile.Commands;
using Yaam.UseCases.Profile.Commands.Certifications;
using Yaam.UseCases.Profile.Commands.Education;
using Yaam.UseCases.Profile.Commands.Languages;
using Yaam.UseCases.Profile.Commands.Links;
using Yaam.UseCases.Profile.Commands.WorkExperiences;
using Yaam.UseCases.Profile.Queries;

namespace Yaam.API.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("GetProfile")]
    public async Task<ActionResult<ProfileViewModel>> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetProfileQuery(),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpPut("info")]
    [EndpointName("UpdateProfileInfo")]
    public async Task<ActionResult<ProfileViewModel>> UpdateInfo(
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
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpPut("skills")]
    [EndpointName("UpdateProfileSkills")]
    public async Task<ActionResult<ProfileViewModel>> UpdateSkills(
        [FromBody] UpdateProfileSkillsInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateProfileSkillsCommand(input.Skills),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpPost("work-experiences")]
    [EndpointName("AddWorkExperience")]
    public async Task<ActionResult<WorkExperienceViewModel>> AddWorkExperience(
        [FromBody] WorkExperienceInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddWorkExperienceCommand(
                input.Company, input.Title, input.StartDate, input.EndDate, input.Description),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpPut("work-experiences/{id:guid}")]
    [EndpointName("UpdateWorkExperience")]
    public async Task<ActionResult<WorkExperienceViewModel>> UpdateWorkExperience(
        Guid id, [FromBody] WorkExperienceInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateWorkExperienceCommand(
                id, input.Company, input.Title, input.StartDate, input.EndDate, input.Description),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
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

    [HttpPost("education")]
    [EndpointName("AddEducation")]
    public async Task<ActionResult<EducationViewModel>> AddEducation(
        [FromBody] EducationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddEducationCommand(
                input.Institution, input.Degree, input.FieldOfStudy, input.StartDate, input.EndDate),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpPut("education/{id:guid}")]
    [EndpointName("UpdateEducation")]
    public async Task<ActionResult<EducationViewModel>> UpdateEducation(
        Guid id, [FromBody] EducationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateEducationCommand(
                id, input.Institution, input.Degree, input.FieldOfStudy, input.StartDate, input.EndDate),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("education/{id:guid}")]
    [EndpointName("DeleteEducation")]
    public async Task<IActionResult> DeleteEducation(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteEducationCommand(id),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("languages")]
    [EndpointName("AddLanguage")]
    public async Task<ActionResult<LanguageViewModel>> AddLanguage(
        [FromBody] LanguageInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddLanguageCommand(input.Name, input.Proficiency),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpPut("languages/{id:guid}")]
    [EndpointName("UpdateLanguage")]
    public async Task<ActionResult<LanguageViewModel>> UpdateLanguage(
        Guid id, [FromBody] LanguageInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateLanguageCommand(id, input.Name, input.Proficiency),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("languages/{id:guid}")]
    [EndpointName("DeleteLanguage")]
    public async Task<IActionResult> DeleteLanguage(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteLanguageCommand(id),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("certifications")]
    [EndpointName("AddCertification")]
    public async Task<ActionResult<CertificationViewModel>> AddCertification(
        [FromBody] CertificationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddCertificationCommand(input.Name, input.Issuer, input.Date),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpPut("certifications/{id:guid}")]
    [EndpointName("UpdateCertification")]
    public async Task<ActionResult<CertificationViewModel>> UpdateCertification(
        Guid id, [FromBody] CertificationInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateCertificationCommand(id, input.Name, input.Issuer, input.Date),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("certifications/{id:guid}")]
    [EndpointName("DeleteCertification")]
    public async Task<IActionResult> DeleteCertification(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteCertificationCommand(id),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("links")]
    [EndpointName("AddProfileLink")]
    public async Task<ActionResult<ProfileLinkViewModel>> AddLink(
        [FromBody] ProfileLinkInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddProfileLinkCommand(input.Label, input.Url),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpPut("links/{id:guid}")]
    [EndpointName("UpdateProfileLink")]
    public async Task<ActionResult<ProfileLinkViewModel>> UpdateLink(
        Guid id, [FromBody] ProfileLinkInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateProfileLinkCommand(id, input.Label, input.Url),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpDelete("links/{id:guid}")]
    [EndpointName("DeleteProfileLink")]
    public async Task<IActionResult> DeleteLink(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new DeleteProfileLinkCommand(id),
            cancellationToken);
        return NoContent();
    }
}

public record UpdateProfileInfoInputModel(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Location,
    string? Summary);

public record UpdateProfileSkillsInputModel(List<string> Skills);

public record WorkExperienceInputModel(
    string Company,
    string Title,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Description);

public record EducationInputModel(
    string Institution,
    string? Degree,
    string? FieldOfStudy,
    DateOnly? StartDate,
    DateOnly? EndDate);

public record LanguageInputModel(string Name, LanguageProficiency Proficiency);

public record CertificationInputModel(string Name, string? Issuer, DateOnly Date);

public record ProfileLinkInputModel(string Label, string Url);
```

Note: `IActionResult` endpoints that previously returned Profile/sub-entity DTOs now have proper typed return types (`ActionResult<ProfileViewModel>`, `ActionResult<WorkExperienceViewModel>`, etc.), which improves OpenAPI schema generation.

- [ ] **Step 2: Build the full solution**

```bash
dotnet build backend/Yaam.sln
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Run integration tests**

```bash
dotnet test backend/Yaam.sln
```

Expected: All tests pass (the wire format is unchanged, only the C# type names differ).

- [ ] **Step 4: Commit**

```bash
git add backend/Yaam.API/Profile/ backend/Yaam.API/Controllers/ProfileController.cs
git commit -m "refactor: add Profile ViewModels and API-layer mapper, update controller"
```

---

## Task 6: Regenerate frontend API types

**Files:** All of `frontend/src/app/generated/` (auto-regenerated — do not edit by hand)

- [ ] **Step 1: Start the backend**

```bash
dotnet run --project backend/Yaam.API &
```

Wait ~5 seconds, then verify:

```bash
curl -s http://localhost:5059/openapi/v1.json | python3 -c "import sys,json; d=json.load(sys.stdin); print(list(d['components']['schemas'].keys())[:10])"
```

Expected: Schema names list includes `ApplicationViewModel`, `ProfileViewModel`, etc. (not `ApplicationDto`).

- [ ] **Step 2: Regenerate from frontend directory**

```bash
cd frontend && npm run generate:api
```

Expected: No errors; generated files updated.

- [ ] **Step 3: Stop the backend**

```bash
kill %1
```

- [ ] **Step 4: Verify new ViewModel type names in generated output**

```bash
grep -r "ApplicationViewModel\|ProfileViewModel" frontend/src/app/generated/ --include="*.ts" | head -5
```

Expected: Multiple matches.

---

## Task 7: Update frontend feature imports

The generated layer now exports `*ViewModel` names. Update every hand-written feature file that imported the old `*Dto` names.

- [ ] **Step 1: Update `applications/models/application.model.ts`**

```typescript
export type {
  ApplicationViewModel as Application,
  ApplicationSummaryViewModel as ApplicationSummary,
} from '../../../generated/api/index';
```

- [ ] **Step 2: Update `applications/models/application-note.model.ts`**

```typescript
export type { ApplicationNoteViewModel as ApplicationNote } from '../../../generated/api/index';
```

- [ ] **Step 3: Update `profile-info-modal.component.ts`**

Change import line and all type usages from `ProfileDto` → `ProfileViewModel`:
```typescript
import { ProfileViewModel, ProfileService } from '../../../../generated/api';
```
Update `readonly profile = input.required<ProfileViewModel>();` and `readonly saved = output<ProfileViewModel>();`.

- [ ] **Step 4: Update `profile-link-section.component.ts`**

```typescript
import { ProfileViewModel, ProfileService, ProfileLinkViewModel } from '../../../../generated/api';
```
Replace all `ProfileDto` → `ProfileViewModel` and `ProfileLinkDto` → `ProfileLinkViewModel` in the component.

- [ ] **Step 5: Update `custom-fields-section.component.ts`**

```typescript
import { CustomFieldViewModel } from '../../../../generated/api';
```
Update `readonly fields = input.required<CustomFieldViewModel[]>();`.

- [ ] **Step 6: Update `profile-link-modal.component.ts`**

```typescript
import { ProfileLinkViewModel } from '../../../../generated/api';
```
Update `readonly item = input<ProfileLinkViewModel | null>(null);`.

- [ ] **Step 7: Update `profile-info-section.component.ts`**

```typescript
import { ProfileViewModel } from '../../../../generated/api';
```
Replace all `ProfileDto` → `ProfileViewModel` in the component.

- [ ] **Step 8: Update `profile-skills-section.component.ts`**

```typescript
import { ProfileViewModel, ProfileService } from '../../../../generated/api';
```
Update `readonly changed = output<ProfileViewModel>();`.

- [ ] **Step 9: Update `certification-section.component.ts`**

```typescript
import { ProfileViewModel, ProfileService, CertificationViewModel } from '../../../../generated/api';
```
Replace all `ProfileDto` → `ProfileViewModel` and `CertificationDto` → `CertificationViewModel`.

- [ ] **Step 10: Update `education-modal.component.ts`**

```typescript
import { EducationViewModel } from '../../../../generated/api';
```
Update `readonly item = input<EducationViewModel | null>(null);`.

- [ ] **Step 11: Update `education-section.component.ts`**

```typescript
import { ProfileViewModel, ProfileService, EducationViewModel } from '../../../../generated/api';
```
Replace all `ProfileDto` → `ProfileViewModel` and `EducationDto` → `EducationViewModel` (including `degreeAndField(item: EducationViewModel)`).

- [ ] **Step 12: Update `language-modal.component.ts`**

```typescript
import { LanguageViewModel, LanguageProficiency } from '../../../../generated/api';
```
Update `readonly item = input<LanguageViewModel | null>(null);` and the proficiency cast: `proficiency: data.proficiency as LanguageViewModel['proficiency']`.

- [ ] **Step 13: Update `language-section.component.ts`**

```typescript
import { ProfileViewModel, ProfileService, LanguageViewModel } from '../../../../generated/api';
```
Replace all `ProfileDto` → `ProfileViewModel` and `LanguageDto` → `LanguageViewModel`.

- [ ] **Step 14: Update `certification-modal.component.ts`**

```typescript
import { CertificationViewModel } from '../../../../generated/api';
```
Update `readonly item = input<CertificationViewModel | null>(null);`.

- [ ] **Step 15: Update `work-experience-section.component.ts`**

```typescript
import { ProfileViewModel, ProfileService, WorkExperienceViewModel } from '../../../../generated/api';
```
Replace all `ProfileDto` → `ProfileViewModel` and `WorkExperienceDto` → `WorkExperienceViewModel`.

- [ ] **Step 16: Update `work-experience-modal.component.ts`**

```typescript
import { WorkExperienceViewModel } from '../../../../generated/api';
```
Update `readonly item = input<WorkExperienceViewModel | null>(null);`.

- [ ] **Step 17: Update `profile-page.component.ts`**

```typescript
import { ProfileViewModel, ProfileService } from '../../../../generated/api';
```
Update `protected readonly profileResource = resource<ProfileViewModel, void>({...`.

- [ ] **Step 18: Build the frontend**

```bash
cd frontend && npm run build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 19: Commit**

```bash
git add frontend/src/app/generated/ frontend/src/app/features/
git commit -m "refactor: regenerate API types and update frontend imports for ViewModel suffix"
```

---

## Task 8: Update CLAUDE.md and create PR

- [ ] **Step 1: Add the naming guideline to CLAUDE.md**

Append after the existing `## Backend controllers — input models` section:

```markdown
## Backend controllers — response naming (ViewModel suffix)

Controller responses use a two-layer type model:

- **Use-case layer** (`Yaam.UseCases/<Feature>/Dtos/`): `*Dto` records are the output of MediatR handlers. They are internal to the use-case layer and must not be returned directly from a controller.
- **API layer** (`Yaam.API/<Feature>/`): `*ViewModel` records are what controllers return. An API-layer mapper (e.g. `ApplicationViewModelMapper`, `ProfileViewModelMapper`) converts `*Dto` → `*ViewModel` inside the controller method.

This separation means the API shape can evolve independently of the use-case internals.
```

- [ ] **Step 2: Build the full solution one final time**

```bash
dotnet build backend/Yaam.sln && cd frontend && npm run build
```

Expected: Both build with 0 errors.

- [ ] **Step 3: Run all backend tests**

```bash
dotnet test backend/Yaam.sln
```

Expected: All tests pass.

- [ ] **Step 4: Stage and commit CLAUDE.md**

```bash
git add CLAUDE.md
git commit -m "docs: add ViewModel/Dto layer separation guideline to CLAUDE.md"
```

- [ ] **Step 5: Push and open PR**

```bash
git push -u origin refactor/dto-to-viewmodel
gh pr create \
  --title "refactor: introduce API-layer ViewModels separate from use-case DTOs" \
  --body "$(cat <<'EOF'
## Summary

- Added \`*ViewModel\` records in \`Yaam.API/Applications/\` and \`Yaam.API/Profile/\` — the types controllers return over HTTP
- Added \`ApplicationViewModelMapper\` and \`ProfileViewModelMapper\` in the API layer to convert use-case \`*Dto\` results into ViewModels
- Updated all three controllers to call the mapper before returning
- \`Yaam.UseCases\` layer is unchanged — all \`*Dto\` records, handlers, and use-case mappers are untouched
- Regenerated frontend OpenAPI types; updated all feature component imports to \`*ViewModel\`
- Added the DTO/ViewModel layer contract to CLAUDE.md

## Test plan

- [ ] \`dotnet build backend/Yaam.sln\` — zero errors
- [ ] \`dotnet test backend/Yaam.sln\` — all integration tests pass
- [ ] \`npm run build\` (from \`frontend/\`) — zero errors
EOF
)"
```
