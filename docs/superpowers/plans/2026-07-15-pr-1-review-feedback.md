# PR #1 Review Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Address all reviewer feedback on PR #1 (application tracking), spanning backend naming, architecture, exception handling, validation, sorting, mapping, tests, and a full Angular signal forms migration.

**Architecture:** Backend changes flow from cosmetic (naming, aliases) to structural (input models, exception middleware, mapper), then constraints and tests. Frontend replaces `ReactiveFormsModule` + raw signal bindings with Angular 22 signal forms (`form()` / `schema()` / `[formField]`) throughout, per ADR 004 (already written).

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10 + PostgreSQL, MediatR, FluentValidation, Angular 22 signal forms (`@angular/forms`), daisyUI v3 + Tailwind CSS v3.

## Global Constraints

- No `DomainApp` / `DomainNote` or any abbreviated alias — use the full class name directly.
- Handler `Handle` methods: parameter must be named `command` for commands, `query` for queries — never `request`.
- Controller action `CancellationToken` parameters: always named `cancellationToken`, no `= default` default value.
- No `ReactiveFormsModule`, `FormBuilder`, `FormGroup`, or `FormControl` in frontend (ADR 004).
- No manual `[value]` / `(input)` signal bindings on form inputs — use `[formField]` (ADR 004).
- No `Co-Authored-By` trailers in git commits.
- Run `dotnet build` and `dotnet test` before each backend commit; run `npx ng build --configuration production` before each frontend commit.

---

## File Map

**Create:**
- `backend/Yaam.Domain/Errors/NotFoundException.cs`
- `backend/Yaam.Application/Applications/ApplicationMapper.cs`
- `backend/Yaam.Tests.Integration/appsettings.Test.json`

**Modify (Backend):**
- `backend/Yaam.API/Controllers/ApplicationsController.cs`
- `backend/Yaam.API/Controllers/ApplicationNotesController.cs`
- `backend/Yaam.API/GlobalExceptionHandler.cs`
- `backend/Yaam.Application/Applications/Commands/CreateApplicationCommand.cs`
- `backend/Yaam.Application/Applications/Commands/UpdateApplicationCommand.cs`
- `backend/Yaam.Application/Applications/Commands/UpdateApplicationStatusCommand.cs`
- `backend/Yaam.Application/Applications/Commands/DeleteApplicationCommand.cs`
- `backend/Yaam.Application/Applications/Commands/Notes/AddApplicationNoteCommand.cs`
- `backend/Yaam.Application/Applications/Commands/Notes/UpdateApplicationNoteCommand.cs`
- `backend/Yaam.Application/Applications/Commands/Notes/DeleteApplicationNoteCommand.cs`
- `backend/Yaam.Application/Applications/Queries/GetApplicationsQuery.cs`
- `backend/Yaam.Application/Applications/Queries/GetApplicationByIdQuery.cs`
- `backend/Yaam.Domain/Entities/Application.cs`
- `backend/Yaam.Domain/Entities/ApplicationNote.cs`
- `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs`
- `backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationConfiguration.cs`
- `backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs`
- `backend/Yaam.Tests.Integration/ApiFactory.cs`
- `backend/Yaam.Tests.Integration/Applications/ApplicationsEndpointsTests.cs`
- `backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj`
- `backend/Yaam.Tests.Unit/Applications/ApplicationNoteCommandValidatorTests.cs`
- `backend/Yaam.Tests.Unit/Applications/GetApplicationsQueryTests.cs`
- `docs/architecture/guidelines.md`
- `docs/architecture/decisions/005-input-models.md`

**Modify (Frontend):**
- `frontend/src/app/features/applications/components/application-form/application-form.component.ts`
- `frontend/src/app/features/applications/components/application-form/application-form.component.html`
- `frontend/src/app/features/applications/components/application-notes/application-notes.component.ts`
- `frontend/src/app/features/applications/components/application-notes/application-notes.component.html`
- `frontend/src/app/features/applications/pages/application-detail/application-detail.component.ts`

**Create (Docs):**
- `docs/specs/future-enhancements.md`

---

## Task 1: Backend naming & style

Fixes: aliases, `ct` rename, `= default`, multi-line command, handler parameter names.

**Files:**
- Modify: all handler and controller files listed above (no logic changes — naming only)
- Modify: `docs/architecture/guidelines.md`

**Interfaces:**
- Produces: clean, compilable codebase that later tasks build on

- [ ] **Step 1: Remove abbreviated aliases in Application layer**

In `backend/Yaam.Application/Applications/Commands/CreateApplicationCommand.cs`, replace:
```csharp
using Yaam.Domain.Repositories;
using DomainApp = Yaam.Domain.Entities.Application;
```
with:
```csharp
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
```
Then in the handler body replace `new DomainApp` with `new Application`.

In `backend/Yaam.Application/Applications/Commands/Notes/AddApplicationNoteCommand.cs`, replace:
```csharp
using Yaam.Domain.Repositories;
using DomainNote = Yaam.Domain.Entities.ApplicationNote;
```
with:
```csharp
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
```
Then replace `new DomainNote` with `new ApplicationNote`.

In `backend/Yaam.Application/Applications/Queries/GetApplicationsQuery.cs`, replace:
```csharp
using DomainApp = Yaam.Domain.Entities.Application;
```
with:
```csharp
using Yaam.Domain.Entities;
```
Then replace `DomainApp` with `Application` in the `ToDto` method.

In `backend/Yaam.Application/Applications/Queries/GetApplicationByIdQuery.cs`, remove the alias line:
```csharp
using DomainApp = Yaam.Domain.Entities.Application;
```
(`using Yaam.Domain.Entities;` is already there.) Replace `DomainApp` with `Application` in the `ToDto` method.

- [ ] **Step 2: Remove aliases in Infrastructure layer**

In `backend/Yaam.Infrastructure/Persistence/AppDbContext.cs`, replace:
```csharp
using Yaam.Domain.Entities;
using DomainApplication = Yaam.Domain.Entities.Application;
using DomainApplicationNote = Yaam.Domain.Entities.ApplicationNote;
```
with:
```csharp
using Yaam.Domain.Entities;
```
Then replace `DomainApplication` with `Application` and `DomainApplicationNote` with `ApplicationNote` throughout the file.

In `backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs`, replace:
```csharp
using DomainApplication = Yaam.Domain.Entities.Application;
using DomainApplicationNote = Yaam.Domain.Entities.ApplicationNote;
```
with:
```csharp
using Yaam.Domain.Entities;
```
Then replace all `DomainApplication` with `Application` and `DomainApplicationNote` with `ApplicationNote`.

In `backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationConfiguration.cs`, replace:
```csharp
using DomainApplication = Yaam.Domain.Entities.Application;
using DomainApplicationNote = Yaam.Domain.Entities.ApplicationNote;
```
with:
```csharp
using Yaam.Domain.Entities;
```
Replace `DomainApplication` with `Application` and `DomainApplicationNote` with `ApplicationNote`.

- [ ] **Step 3: Rename handler parameter from `request` to `command`/`query`**

In every command handler `Handle` method, rename the first parameter to `command`. In every query handler, rename it to `query`. Update all references to the parameter in the method body.

Files and changes:

`CreateApplicationCommand.cs` — `request` → `command`:
```csharp
public async Task<ApplicationDto> Handle(
    CreateApplicationCommand command, CancellationToken cancellationToken)
{
    var application = new Application
    {
        CompanyName = command.CompanyName,
        Role = command.Role,
        DateApplied = command.DateApplied,
        Status = command.Status,
        ContactName = command.ContactName,
        ContactEmail = command.ContactEmail,
        ContactPhone = command.ContactPhone,
        JobPosting = command.JobPosting,
    };
    await repository.AddAsync(application, cancellationToken);
    return new ApplicationDto(
        application.Id, application.CompanyName, application.Role,
        application.DateApplied, application.Status,
        application.ContactName, application.ContactEmail, application.ContactPhone,
        application.JobPosting, application.CreatedAt, application.UpdatedAt, []);
}
```

`UpdateApplicationCommand.cs` — `request` → `command` (also update all `request.X` references to `command.X`).

`UpdateApplicationStatusCommand.cs` — `request` → `command`.

`DeleteApplicationCommand.cs`:
```csharp
public async Task Handle(DeleteApplicationCommand command, CancellationToken cancellationToken)
    => await repository.DeleteAsync(command.Id, cancellationToken);
```

`AddApplicationNoteCommand.cs` — `request` → `command`:
```csharp
public async Task<ApplicationNoteDto?> Handle(
    AddApplicationNoteCommand command, CancellationToken cancellationToken)
{
    var application = await repository.GetByIdAsync(command.ApplicationId, cancellationToken);
    if (application is null) return null;

    var note = new ApplicationNote
    {
        ApplicationId = command.ApplicationId,
        Body = command.Body,
    };
    await repository.AddNoteAsync(note, cancellationToken);
    return new ApplicationNoteDto(note.Id, note.Body, note.CreatedAt, note.UpdatedAt);
}
```

`UpdateApplicationNoteCommand.cs` — `request` → `command`, update all references.

`DeleteApplicationNoteCommand.cs`:
```csharp
public async Task Handle(
    DeleteApplicationNoteCommand command, CancellationToken cancellationToken)
    => await repository.DeleteNoteAsync(command.ApplicationId, command.NoteId, cancellationToken);
```

`GetApplicationsQuery.cs`:
```csharp
public async Task<List<ApplicationSummaryDto>> Handle(
    GetApplicationsQuery query, CancellationToken cancellationToken)
{
    var applications = await repository.GetAllAsync(
        query.Status, query.Sort, query.Order, cancellationToken);
    return applications.Select(ToDto).ToList();
}
```

`GetApplicationByIdQuery.cs`:
```csharp
public async Task<ApplicationDto?> Handle(
    GetApplicationByIdQuery query, CancellationToken cancellationToken)
{
    var application = await repository.GetByIdAsync(query.Id, cancellationToken);
    return application is null ? null : ToDto(application);
}
```

- [ ] **Step 4: Fix CancellationToken naming in controllers**

In `ApplicationsController.cs`, rename every `ct` parameter to `cancellationToken` and remove all `= default` defaults. Also multi-line the `UpdateApplicationCommand` instantiation in `Update`:

```csharp
[HttpGet]
[EndpointName("ListApplications")]
public async Task<IActionResult> GetAll(
    [FromQuery] ApplicationStatus? status,
    [FromQuery] string sort = "dateApplied",
    [FromQuery] string order = "desc",
    CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetApplicationsQuery(status, sort, order), cancellationToken);
    return Ok(result);
}

[HttpGet("{id:guid}")]
[EndpointName("GetApplication")]
public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetApplicationByIdQuery(id), cancellationToken);
    return result is null ? NotFound() : Ok(result);
}

[HttpPost]
[EndpointName("CreateApplication")]
public async Task<IActionResult> Create(
    [FromBody] CreateApplicationCommand command, CancellationToken cancellationToken)
{
    var result = await mediator.Send(command, cancellationToken);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}

[HttpPut("{id:guid}")]
[EndpointName("UpdateApplication")]
public async Task<IActionResult> Update(
    Guid id, [FromBody] UpdateApplicationRequest request, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateApplicationCommand(
            id,
            request.CompanyName,
            request.Role,
            request.DateApplied,
            request.Status,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone,
            request.JobPosting),
        cancellationToken);
    return result is null ? NotFound() : Ok(result);
}

[HttpPatch("{id:guid}/status")]
[EndpointName("PatchApplicationStatus")]
public async Task<IActionResult> UpdateStatus(
    Guid id, [FromBody] UpdateApplicationStatusRequest request, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateApplicationStatusCommand(id, request.Status), cancellationToken);
    return result is null ? NotFound() : Ok(result);
}

[HttpDelete("{id:guid}")]
[EndpointName("DeleteApplication")]
public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
{
    await mediator.Send(new DeleteApplicationCommand(id), cancellationToken);
    return NoContent();
}
```

In `ApplicationNotesController.cs`, rename `ct` → `cancellationToken` and remove `= default` in all three actions.

- [ ] **Step 5: Append naming rules to architecture guidelines**

In `docs/architecture/guidelines.md`, append under the "Coding Style" section:

```markdown
### Backend Naming Conventions

- **Handler parameter:** `command` for `IRequestHandler<TCommand>`, `query` for `IRequestHandler<TQuery>`. Never `request`.
- **Controller CancellationToken:** Always named `cancellationToken`. Never `ct`. Never `= default` — ASP.NET Core always injects it.
- **Type aliases:** No abbreviated aliases (`DomainApp`, `DomainNote`). If a `using` alias is genuinely needed to resolve ambiguity, use the full class name as the alias (e.g., `using Application = Yaam.Domain.Entities.Application`). Prefer resolving the ambiguity by restructuring instead.
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build backend/Yaam.sln
```
Expected: no errors, no warnings about unused aliases.

- [ ] **Step 7: Commit**

```bash
git add backend/ docs/architecture/guidelines.md
git commit -m "refactor: rename ct, aliases, and handler parameter names throughout"
```

---

## Task 2: Input models & ADR

Adds `CreateApplicationInputModel`, renames the existing request records to `*InputModel`, and introduces ADR 005.

**Files:**
- Modify: `backend/Yaam.API/Controllers/ApplicationsController.cs`
- Modify: `backend/Yaam.API/Controllers/ApplicationNotesController.cs`
- Create: `docs/architecture/decisions/005-input-models.md`

**Interfaces:**
- Produces: controllers that never bind directly to Application/Infrastructure layer types

- [ ] **Step 1: Write ADR 005**

Create `docs/architecture/decisions/005-input-models.md`:

```markdown
# ADR 005: API layer input models

**Date:** 2026-07-15
**Status:** Accepted

## Context

The API layer should not bind HTTP request bodies directly to Application layer commands. Doing so couples HTTP serialization to the command's constructor — any change to the command (adding an internal field, changing a type) risks silently altering the public API contract.

## Decision

Every HTTP endpoint that accepts a request body uses a dedicated **input model** in the API layer. Input models:
- Are named `<Verb><Resource>InputModel` (e.g., `CreateApplicationInputModel`, `UpdateApplicationNoteInputModel`).
- Live in the same file as the controller that uses them (defined at the bottom of the file as `record` types).
- Are mapped to commands/queries explicitly inside the controller action.

The API layer is the only place where input models exist. They do not flow into Application or Domain layers.

## Consequences

- The HTTP surface is decoupled from command signatures.
- Renaming or restructuring a command does not require changing the API contract.
- Controllers grow slightly (mapping code), but the mapping is intentional and visible.
```

- [ ] **Step 2: Add `CreateApplicationInputModel` and rename existing records in `ApplicationsController.cs`**

Replace the bottom of `ApplicationsController.cs` (the record definitions) and the `Create` action with:

```csharp
// In the Create action — replace [FromBody] CreateApplicationCommand with [FromBody] CreateApplicationInputModel:
[HttpPost]
[EndpointName("CreateApplication")]
public async Task<IActionResult> Create(
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
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}

// Keep Update, UpdateStatus, and Delete actions unchanged for now.
```

Replace the record definitions at the bottom of the file:
```csharp
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

Update the `Update` and `UpdateStatus` actions to use the new names (`UpdateApplicationInputModel` and `UpdateApplicationStatusInputModel`). The mapping code in `Update` stays the same, just the type of `input` (was `request`) changes:

```csharp
[HttpPut("{id:guid}")]
[EndpointName("UpdateApplication")]
public async Task<IActionResult> Update(
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
    return result is null ? NotFound() : Ok(result);
}

[HttpPatch("{id:guid}/status")]
[EndpointName("PatchApplicationStatus")]
public async Task<IActionResult> UpdateStatus(
    Guid id, [FromBody] UpdateApplicationStatusInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateApplicationStatusCommand(id, input.Status), cancellationToken);
    return result is null ? NotFound() : Ok(result);
}
```

- [ ] **Step 3: Split `NoteBodyRequest` in `ApplicationNotesController.cs`**

Replace the single `NoteBodyRequest` record and update both actions:

```csharp
[HttpPost]
[EndpointName("AddApplicationNote")]
public async Task<IActionResult> Add(
    Guid applicationId, [FromBody] CreateApplicationNoteInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new AddApplicationNoteCommand(applicationId, input.Body), cancellationToken);
    return result is null ? NotFound() : Created(string.Empty, result);
}

[HttpPut("{noteId:guid}")]
[EndpointName("UpdateApplicationNote")]
public async Task<IActionResult> Update(
    Guid applicationId, Guid noteId,
    [FromBody] UpdateApplicationNoteInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateApplicationNoteCommand(applicationId, noteId, input.Body), cancellationToken);
    return result is null ? NotFound() : Ok(result);
}

// Delete action unchanged.
```

Remove `public record NoteBodyRequest(string Body);` and add:
```csharp
public record CreateApplicationNoteInputModel(string Body);
public record UpdateApplicationNoteInputModel(string Body);
```

- [ ] **Step 4: Build and verify**

```bash
dotnet build backend/Yaam.sln
```
Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add backend/Yaam.API/ docs/architecture/decisions/005-input-models.md
git commit -m "refactor: add API layer input models and split NoteBodyRequest"
```

---

## Task 3: NotFoundException + throw-on-not-found

Introduces `NotFoundException`, adds it to the global error handler, and converts all null-return patterns to throws.

**Files:**
- Create: `backend/Yaam.Domain/Errors/NotFoundException.cs`
- Modify: `backend/Yaam.API/GlobalExceptionHandler.cs`
- Modify: all handlers that currently return null (AddApplicationNote, UpdateApplicationNote, UpdateApplication, UpdateApplicationStatus, GetApplicationById)
- Modify: `ApplicationsController.cs` + `ApplicationNotesController.cs` (remove null checks)

**Interfaces:**
- Produces: non-nullable return types on all handlers; controllers call `Ok()` directly

- [ ] **Step 1: Create NotFoundException**

Create `backend/Yaam.Domain/Errors/NotFoundException.cs`:

```csharp
namespace Yaam.Domain.Errors;

public class NotFoundException(string entityName, Guid id)
    : Exception($"{entityName} with id '{id}' was not found.");
```

- [ ] **Step 2: Register NotFoundException in GlobalExceptionHandler**

In `backend/Yaam.API/GlobalExceptionHandler.cs`, add a `NotFoundException` arm to the switch:

```csharp
var (statusCode, title, errors) = exception switch
{
    ValidationException ve => (
        StatusCodes.Status400BadRequest,
        "Validation failed",
        ve.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray())),
    NotFoundException => (StatusCodes.Status404NotFound, "Resource not found.", (Dictionary<string, string[]>?)null),
    _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", (Dictionary<string, string[]>?)null)
};
```

Add `using Yaam.Domain.Errors;` at the top.

- [ ] **Step 3: Update handlers to throw instead of returning null**

**`GetApplicationByIdQuery.cs`** — change return type from `IRequest<ApplicationDto?>` to `IRequest<ApplicationDto>` and throw:

```csharp
public record GetApplicationByIdQuery(Guid Id) : IRequest<ApplicationDto>;

public class GetApplicationByIdQueryHandler(IApplicationRepository repository)
    : IRequestHandler<GetApplicationByIdQuery, ApplicationDto>
{
    public async Task<ApplicationDto> Handle(
        GetApplicationByIdQuery query, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(query.Id, cancellationToken);
        if (application is null) throw new NotFoundException(nameof(Application), query.Id);
        return ToDto(application);
    }
    // ToDto and ToNoteDto remain as-is
}
```

Add `using Yaam.Domain.Errors;` and `using Yaam.Domain.Entities;`.

**`UpdateApplicationCommand.cs`** — change `IRequest<ApplicationDto?>` to `IRequest<ApplicationDto>`:

```csharp
public record UpdateApplicationCommand(...) : IRequest<ApplicationDto>;

public class UpdateApplicationCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationCommand, ApplicationDto>
{
    public async Task<ApplicationDto> Handle(
        UpdateApplicationCommand command, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (application is null) throw new NotFoundException(nameof(Application), command.Id);

        application.CompanyName = command.CompanyName;
        application.Role = command.Role;
        application.DateApplied = command.DateApplied;
        application.Status = command.Status;
        application.ContactName = command.ContactName;
        application.ContactEmail = command.ContactEmail;
        application.ContactPhone = command.ContactPhone;
        application.JobPosting = command.JobPosting;

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

Add `using Yaam.Domain.Entities;` and `using Yaam.Domain.Errors;`.

**`UpdateApplicationStatusCommand.cs`** — same pattern, change to `IRequest<ApplicationDto>`, throw instead of null return. The body remains the same except the null check becomes a throw.

**`AddApplicationNoteCommand.cs`** — change to `IRequest<ApplicationNoteDto>`:

```csharp
public record AddApplicationNoteCommand(...) : IRequest<ApplicationNoteDto>;

public class AddApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<AddApplicationNoteCommand, ApplicationNoteDto>
{
    public async Task<ApplicationNoteDto> Handle(
        AddApplicationNoteCommand command, CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(command.ApplicationId, cancellationToken);
        if (application is null)
            throw new NotFoundException(nameof(Application), command.ApplicationId);

        var note = new ApplicationNote
        {
            ApplicationId = command.ApplicationId,
            Body = command.Body,
        };
        await repository.AddNoteAsync(note, cancellationToken);
        return new ApplicationNoteDto(note.Id, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
```

Add `using Yaam.Domain.Errors;`.

**`UpdateApplicationNoteCommand.cs`** — change to `IRequest<ApplicationNoteDto>`, throw on null:

```csharp
public record UpdateApplicationNoteCommand(...) : IRequest<ApplicationNoteDto>;

public class UpdateApplicationNoteCommandHandler(IApplicationRepository repository)
    : IRequestHandler<UpdateApplicationNoteCommand, ApplicationNoteDto>
{
    public async Task<ApplicationNoteDto> Handle(
        UpdateApplicationNoteCommand command, CancellationToken cancellationToken)
    {
        var note = await repository.GetNoteByIdAsync(
            command.ApplicationId, command.NoteId, cancellationToken);
        if (note is null)
            throw new NotFoundException(nameof(ApplicationNote), command.NoteId);

        note.Body = command.Body;
        await repository.UpdateNoteAsync(note, cancellationToken);
        return new ApplicationNoteDto(note.Id, note.Body, note.CreatedAt, note.UpdatedAt);
    }
}
```

Add `using Yaam.Domain.Entities;` and `using Yaam.Domain.Errors;`.

- [ ] **Step 4: Simplify controllers — remove null checks**

In `ApplicationsController.cs`, all actions that previously checked for null now just return `Ok()`:

```csharp
[HttpGet("{id:guid}")]
[EndpointName("GetApplication")]
public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
{
    var result = await mediator.Send(new GetApplicationByIdQuery(id), cancellationToken);
    return Ok(result);
}

[HttpPut("{id:guid}")]
[EndpointName("UpdateApplication")]
public async Task<IActionResult> Update(
    Guid id, [FromBody] UpdateApplicationInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateApplicationCommand(
            id, input.CompanyName, input.Role, input.DateApplied, input.Status,
            input.ContactName, input.ContactEmail, input.ContactPhone, input.JobPosting),
        cancellationToken);
    return Ok(result);
}

[HttpPatch("{id:guid}/status")]
[EndpointName("PatchApplicationStatus")]
public async Task<IActionResult> UpdateStatus(
    Guid id, [FromBody] UpdateApplicationStatusInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateApplicationStatusCommand(id, input.Status), cancellationToken);
    return Ok(result);
}
```

In `ApplicationNotesController.cs`, remove `result is null ? NotFound() :` from Add and Update:

```csharp
[HttpPost]
[EndpointName("AddApplicationNote")]
public async Task<IActionResult> Add(
    Guid applicationId, [FromBody] CreateApplicationNoteInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new AddApplicationNoteCommand(applicationId, input.Body), cancellationToken);
    return Created(string.Empty, result);
}

[HttpPut("{noteId:guid}")]
[EndpointName("UpdateApplicationNote")]
public async Task<IActionResult> Update(
    Guid applicationId, Guid noteId,
    [FromBody] UpdateApplicationNoteInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateApplicationNoteCommand(applicationId, noteId, input.Body), cancellationToken);
    return Ok(result);
}
```

- [ ] **Step 5: Run integration tests to verify 404 behavior is preserved**

```bash
dotnet test backend/Yaam.Tests.Integration --configuration Release
```
Expected: all 10 tests pass. (The existing `GET_ApplicationById_Returns404_ForUnknownId` test verifies the middleware works correctly.)

- [ ] **Step 6: Commit**

```bash
git add backend/
git commit -m "refactor: throw NotFoundException instead of returning null from handlers"
```

---

## Task 4: Mapper class

Extracts `Application → ApplicationDto` mapping (duplicated across 4 handlers) into a single static class.

**Files:**
- Create: `backend/Yaam.Application/Applications/ApplicationMapper.cs`
- Modify: `UpdateApplicationCommand.cs`, `UpdateApplicationStatusCommand.cs`, `GetApplicationByIdQuery.cs`

**Interfaces:**
- Consumes: `Application` and `ApplicationNote` entities; `ApplicationDto` and `ApplicationNoteDto` records
- Produces: `ApplicationMapper.ToDto(Application)` static method

- [ ] **Step 1: Create ApplicationMapper**

Create `backend/Yaam.Application/Applications/ApplicationMapper.cs`:

```csharp
using Yaam.Application.Applications.Dtos;
using Yaam.Domain.Entities;

namespace Yaam.Application.Applications;

internal static class ApplicationMapper
{
    internal static ApplicationDto ToDto(Application a) => new(
        a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status,
        a.ContactName, a.ContactEmail, a.ContactPhone, a.JobPosting,
        a.CreatedAt, a.UpdatedAt,
        a.Notes.OrderByDescending(n => n.CreatedAt).Select(NoteToDto).ToList());

    internal static ApplicationNoteDto NoteToDto(ApplicationNote n) =>
        new(n.Id, n.Body, n.CreatedAt, n.UpdatedAt);
}
```

- [ ] **Step 2: Replace inline mapping in UpdateApplicationCommand**

In `backend/Yaam.Application/Applications/Commands/UpdateApplicationCommand.cs`, remove the `return new ApplicationDto(...)` block and replace with:

```csharp
await repository.UpdateAsync(application, cancellationToken);
return ApplicationMapper.ToDto(application);
```

Remove the `ApplicationNoteDto` import if it's now unused (check other usages first).

- [ ] **Step 3: Replace inline mapping in UpdateApplicationStatusCommand**

Same pattern — replace the `return new ApplicationDto(...)` block with:

```csharp
await repository.UpdateAsync(application, cancellationToken);
return ApplicationMapper.ToDto(application);
```

- [ ] **Step 4: Replace ToDto in GetApplicationByIdQuery**

In `backend/Yaam.Application/Applications/Queries/GetApplicationByIdQuery.cs`, remove the private `ToDto` and `ToNoteDto` methods and replace the handler body with:

```csharp
public async Task<ApplicationDto> Handle(
    GetApplicationByIdQuery query, CancellationToken cancellationToken)
{
    var application = await repository.GetByIdAsync(query.Id, cancellationToken);
    if (application is null) throw new NotFoundException(nameof(Application), query.Id);
    return ApplicationMapper.ToDto(application);
}
```

Add `using Yaam.Application.Applications;` if needed (same assembly — no import needed since they're in the same project; just ensure the namespace is accessible).

Note: `CreateApplicationCommand` builds the `ApplicationDto` return from a freshly created entity that has empty notes — it should not use `ApplicationMapper.ToDto` since the `Notes` collection won't be loaded at that point. Leave `CreateApplicationCommand` unchanged.

- [ ] **Step 5: Build and run all tests**

```bash
dotnet build backend/Yaam.sln && dotnet test backend/Yaam.Tests.Integration --configuration Release && dotnet test backend/Yaam.Tests.Unit --configuration Release
```
Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/Yaam.Application/
git commit -m "refactor: extract ApplicationMapper to eliminate duplicate DTO mapping"
```

---

## Task 5: Domain constraints & validation lengths

Adds `required` keyword to entity string properties, raises Role max to 350, lowers ContactPhone max to 20, adds a DB migration.

**Files:**
- Modify: `backend/Yaam.Domain/Entities/Application.cs`
- Modify: `backend/Yaam.Domain/Entities/ApplicationNote.cs`
- Modify: `backend/Yaam.Application/Applications/Commands/CreateApplicationCommand.cs`
- Modify: `backend/Yaam.Application/Applications/Commands/UpdateApplicationCommand.cs`
- Modify: `backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationConfiguration.cs`

**Interfaces:**
- Produces: domain model that enforces non-empty strings at compile time; DB schema with updated column lengths

- [ ] **Step 1: Add `required` to entity string properties**

In `backend/Yaam.Domain/Entities/Application.cs`:
```csharp
public class Application : Entity
{
    public required string CompanyName { get; set; }
    public required string Role { get; set; }
    public DateOnly? DateApplied { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? JobPosting { get; set; }
    public ICollection<ApplicationNote> Notes { get; set; } = new List<ApplicationNote>();
}
```

In `backend/Yaam.Domain/Entities/ApplicationNote.cs`:
```csharp
public class ApplicationNote : Entity
{
    public Guid ApplicationId { get; set; }
    public required string Body { get; set; }
}
```

- [ ] **Step 2: Update FluentValidation max lengths**

In `CreateApplicationCommand.cs`, change Role validator and add ContactPhone:
```csharp
RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
RuleFor(x => x.Role).NotEmpty().MaximumLength(350);
RuleFor(x => x.ContactPhone).MaximumLength(20).When(x => x.ContactPhone is not null);
RuleFor(x => x.DateApplied)
    .NotNull()
    .WithMessage("Date applied is required unless status is Draft.")
    .When(x => x.Status != ApplicationStatus.Draft);
```

In `UpdateApplicationCommand.cs`, apply the same length changes:
```csharp
RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
RuleFor(x => x.Role).NotEmpty().MaximumLength(350);
RuleFor(x => x.ContactPhone).MaximumLength(20).When(x => x.ContactPhone is not null);
RuleFor(x => x.DateApplied)
    .NotNull()
    .WithMessage("Date applied is required unless status is Draft.")
    .When(x => x.Status != ApplicationStatus.Draft);
```

- [ ] **Step 3: Update EF Core configuration**

In `backend/Yaam.Infrastructure/Persistence/Configurations/ApplicationConfiguration.cs`:
```csharp
builder.Property(a => a.CompanyName).IsRequired().HasMaxLength(200);
builder.Property(a => a.Role).IsRequired().HasMaxLength(350);
builder.Property(a => a.Status).IsRequired().HasConversion<string>();
builder.Property(a => a.ContactName).HasMaxLength(200);
builder.Property(a => a.ContactEmail).HasMaxLength(200);
builder.Property(a => a.ContactPhone).HasMaxLength(20);
```

- [ ] **Step 4: Generate and apply DB migration**

```bash
dotnet ef migrations add UpdateColumnLengths --project backend/Yaam.Infrastructure --startup-project backend/Yaam.API
dotnet ef database update --project backend/Yaam.Infrastructure --startup-project backend/Yaam.API
```

Verify the generated migration alters `Role` to `varchar(350)` and `ContactPhone` to `varchar(20)`.

- [ ] **Step 5: Build and run all tests**

```bash
dotnet build backend/Yaam.sln && dotnet test backend/Yaam.Tests.Integration --configuration Release
```
Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/
git commit -m "feat: add required keyword to entity strings, update column length constraints"
```

---

## Task 6: Sorting cleanup

Removes status sorting, defines allowed sort fields explicitly, throws on unknown sort input.

**Files:**
- Modify: `backend/Yaam.Application/Applications/Queries/GetApplicationsQuery.cs`
- Modify: `backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs`

**Interfaces:**
- Produces: `GetApplicationsQueryValidator` that validates sort/order; repository that throws `ArgumentOutOfRangeException` for unknown sort fields

- [ ] **Step 1: Add validator and allowed sort constants to GetApplicationsQuery**

In `backend/Yaam.Application/Applications/Queries/GetApplicationsQuery.cs`, add a validator:

```csharp
using FluentValidation;
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

public static class ApplicationSortFields
{
    public const string DateApplied = "dateApplied";
    public const string CompanyName = "companyName";
    public static readonly IReadOnlySet<string> All = new HashSet<string>
        { DateApplied, CompanyName };
}

public class GetApplicationsQueryValidator : AbstractValidator<GetApplicationsQuery>
{
    public GetApplicationsQueryValidator()
    {
        RuleFor(x => x.Sort)
            .Must(ApplicationSortFields.All.Contains)
            .WithMessage($"Sort must be one of: {string.Join(", ", ApplicationSortFields.All)}.");
        RuleFor(x => x.Order)
            .Must(o => o == "asc" || o == "desc")
            .WithMessage("Order must be 'asc' or 'desc'.");
    }
}

public class GetApplicationsQueryHandler(IApplicationRepository repository)
    : IRequestHandler<GetApplicationsQuery, List<ApplicationSummaryDto>>
{
    public async Task<List<ApplicationSummaryDto>> Handle(
        GetApplicationsQuery query, CancellationToken cancellationToken)
    {
        var applications = await repository.GetAllAsync(
            query.Status, query.Sort, query.Order, cancellationToken);
        return applications.Select(ToDto).ToList();
    }

    private static ApplicationSummaryDto ToDto(Application a) =>
        new(a.Id, a.CompanyName, a.Role, a.DateApplied, a.Status);
}
```

- [ ] **Step 2: Simplify the repository sort switch**

In `backend/Yaam.Infrastructure/Repositories/ApplicationRepository.cs`, replace the `GetAllAsync` sort logic:

```csharp
query = (sort, order.ToLower()) switch
{
    (ApplicationSortFields.CompanyName, "asc") => query.OrderBy(a => a.CompanyName),
    (ApplicationSortFields.CompanyName, _) => query.OrderByDescending(a => a.CompanyName),
    (ApplicationSortFields.DateApplied, "asc") => query.OrderBy(a => a.DateApplied),
    (ApplicationSortFields.DateApplied, _) => query.OrderByDescending(a => a.DateApplied),
    _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unsupported sort field.")
};
```

Add `using Yaam.Application.Applications.Queries;` at the top (for `ApplicationSortFields`).

Note: the FluentValidation pipeline behavior runs before the handler, so an invalid sort value will never reach the repository at runtime. The throw is a defensive backstop.

- [ ] **Step 3: Build and run unit tests**

```bash
dotnet build backend/Yaam.sln && dotnet test backend/Yaam.Tests.Unit --configuration Release
```
Expected: all tests pass.

- [ ] **Step 4: Commit**

```bash
git add backend/Yaam.Application/ backend/Yaam.Infrastructure/
git commit -m "refactor: remove status sort, make sort field explicit, throw on invalid input"
```

---

## Task 7: Test fixes

Moves hardcoded DB credentials to a settings file, strengthens the DELETE assertion, and removes trivial unit tests.

**Files:**
- Create: `backend/Yaam.Tests.Integration/appsettings.Test.json`
- Modify: `backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj`
- Modify: `backend/Yaam.Tests.Integration/ApiFactory.cs`
- Modify: `backend/Yaam.Tests.Integration/Applications/ApplicationsEndpointsTests.cs`
- Modify: `backend/Yaam.Tests.Unit/Applications/ApplicationNoteCommandValidatorTests.cs`
- Modify: `backend/Yaam.Tests.Unit/Applications/GetApplicationsQueryTests.cs`

**Interfaces:**
- Produces: tests that read config from a file; DELETE test that verifies entity is gone; no trivial validator tests

- [ ] **Step 1: Create `appsettings.Test.json`**

Create `backend/Yaam.Tests.Integration/appsettings.Test.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=yaam_test;Username=yaam;Password=yaam"
  }
}
```

- [ ] **Step 2: Add CopyToOutputDirectory to the .csproj**

In `backend/Yaam.Tests.Integration/Yaam.Tests.Integration.csproj`, add an `ItemGroup`:

```xml
<ItemGroup>
  <None Update="appsettings.Test.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 3: Update ApiFactory to read from appsettings**

Replace `ApiFactory.cs` entirely:

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
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddJsonFile("appsettings.Test.json", optional: false));

        builder.ConfigureServices((context, services) =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    context.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException(
                        "Test connection string 'DefaultConnection' not found in appsettings.Test.json.")));
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}
```

- [ ] **Step 4: Fix the DELETE tests to verify entity removal**

In `ApplicationsEndpointsTests.cs`, replace `DELETE_Application_Returns204` and `DELETE_Note_Returns204`:

```csharp
[Fact]
public async Task DELETE_Application_Returns204_AndRemovesEntity()
{
    var created = await CreateApplicationAsync("Delete Test Co", "PM");

    var deleteResponse = await _client.DeleteAsync($"/api/applications/{created.Id}");
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

    var getResponse = await _client.GetAsync($"/api/applications/{created.Id}");
    getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
}

[Fact]
public async Task DELETE_Note_Returns204_AndRemovesNote()
{
    var app = await CreateApplicationAsync("Note Delete Co", "Dev");
    var note = await CreateNoteAsync(app.Id, "To be deleted.");

    var deleteResponse = await _client.DeleteAsync(
        $"/api/applications/{app.Id}/notes/{note.Id}");
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

    var getResponse = await _client.GetAsync($"/api/applications/{app.Id}");
    getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await getResponse.Content.ReadFromJsonAsync<ApplicationDto>();
    body!.Notes.Should().NotContain(n => n.Id == note.Id);
}
```

- [ ] **Step 5: Remove trivial unit tests**

In `backend/Yaam.Tests.Unit/Applications/ApplicationNoteCommandValidatorTests.cs`, delete the entire file contents and replace with an empty class (or delete the file). These tests only verify `NotEmpty()` from FluentValidation, which is not our logic:

Delete all three test methods. If the file is now empty, remove it and update the project if needed — or just leave an empty class:

```csharp
namespace Yaam.Tests.Unit.Applications;

// Validator tests removed per review: only test complex/critical validation logic, not built-in rules.
```

In `backend/Yaam.Tests.Unit/Applications/GetApplicationsQueryTests.cs`, remove `Handle_ReturnsAllApplicationsAsSummaryDtos` — keep only `Handle_PassesFilterAndSortToRepository`:

```csharp
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
```

(Update the `using` alias in this file too — `DomainApp` → `Application` from `using Yaam.Domain.Entities;`.)

- [ ] **Step 6: Run all tests**

```bash
dotnet test backend/Yaam.Tests.Integration --configuration Release && dotnet test backend/Yaam.Tests.Unit --configuration Release
```
Expected: integration tests pass; unit tests (now just 1) pass.

- [ ] **Step 7: Commit**

```bash
git add backend/Yaam.Tests.Integration/ backend/Yaam.Tests.Unit/
git commit -m "fix: move test credentials to appsettings, strengthen DELETE assertions, remove trivial unit tests"
```

---

## Task 8: Frontend — convert application-form to signal forms

Replaces `ReactiveFormsModule`/`FormBuilder`/`FormGroup` with `form()` + `schema()` per ADR 004. Updates the parent component that consumes the `saved` output.

**Files:**
- Modify: `frontend/src/app/features/applications/components/application-form/application-form.component.ts`
- Modify: `frontend/src/app/features/applications/components/application-form/application-form.component.html`
- Modify: `frontend/src/app/features/applications/pages/application-detail/application-detail.component.ts`

**Interfaces:**
- Produces: `ApplicationFormData` interface exported from the component file; `saved` output emits `ApplicationFormData` instead of `FormGroup`

- [ ] **Step 1: Rewrite application-form.component.ts**

Replace the entire file:

```typescript
import { Component, input, linkedSignal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, required, schema, submit } from '@angular/forms';
import { Application, ALL_STATUSES, STATUS_LABELS } from '../../models/application.model';

export interface ApplicationFormData {
  companyName: string;
  role: string;
  dateApplied: string | null;
  status: string;
  contactName: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  jobPosting: string | null;
}

@Component({
  selector: 'app-application-form',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './application-form.component.html',
})
export class ApplicationFormComponent {
  readonly existing = input<Application | null>(null);
  readonly saved = output<ApplicationFormData>();
  readonly cancelled = output<void>();

  protected readonly ALL_STATUSES = ALL_STATUSES;
  protected readonly STATUS_LABELS = STATUS_LABELS;

  protected readonly formModel = linkedSignal<ApplicationFormData>(() => ({
    companyName: this.existing()?.companyName ?? '',
    role: this.existing()?.role ?? '',
    dateApplied: this.existing()?.dateApplied ?? null,
    status: this.existing()?.status ?? 'Draft',
    contactName: this.existing()?.contactName ?? null,
    contactEmail: this.existing()?.contactEmail ?? null,
    contactPhone: this.existing()?.contactPhone ?? null,
    jobPosting: this.existing()?.jobPosting ?? null,
  }));

  protected readonly fields = form(this.formModel, schema(({ fields }) => {
    required(fields.companyName, { message: 'Company name is required.' });
    required(fields.role, { message: 'Role is required.' });
  }));

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.saved.emit(this.formModel());
      return undefined;
    });
  }

  protected cancel(): void {
    this.cancelled.emit();
  }
}
```

- [ ] **Step 2: Rewrite application-form.component.html**

Replace the entire file:

```html
<form
  [formRoot]="fields"
  class="space-y-4"
>
  <div class="form-control">
    <label class="label" for="companyName">
      <span class="label-text">Company name *</span>
    </label>
    <input
      id="companyName"
      [formField]="fields.companyName"
      type="text"
      class="input input-bordered"
    />
    @if (fields.companyName().touched() && fields.companyName().errors().length) {
      <div class="label">
        <span class="label-text-alt text-error">{{ fields.companyName().errors()[0].message }}</span>
      </div>
    }
  </div>

  <div class="form-control">
    <label class="label" for="role"><span class="label-text">Role *</span></label>
    <input
      id="role"
      [formField]="fields.role"
      type="text"
      class="input input-bordered"
    />
    @if (fields.role().touched() && fields.role().errors().length) {
      <div class="label">
        <span class="label-text-alt text-error">{{ fields.role().errors()[0].message }}</span>
      </div>
    }
  </div>

  <div class="form-control">
    <label class="label" for="status"><span class="label-text">Status *</span></label>
    <select
      id="status"
      [formField]="fields.status"
      class="select select-bordered"
    >
      @for (s of ALL_STATUSES; track s) {
        <option [value]="s">{{ STATUS_LABELS[s] }}</option>
      }
    </select>
  </div>

  <div class="form-control">
    <label class="label" for="dateApplied"><span class="label-text">Date applied</span></label>
    <input
      id="dateApplied"
      [formField]="fields.dateApplied"
      type="date"
      class="input input-bordered"
    />
  </div>

  <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
    <div class="form-control">
      <label class="label" for="contactName"><span class="label-text">Contact name</span></label>
      <input
        id="contactName"
        [formField]="fields.contactName"
        type="text"
        class="input input-bordered"
      />
    </div>
    <div class="form-control">
      <label class="label" for="contactEmail"><span class="label-text">Contact email</span></label>
      <input
        id="contactEmail"
        [formField]="fields.contactEmail"
        type="email"
        class="input input-bordered"
      />
    </div>
    <div class="form-control">
      <label class="label" for="contactPhone"><span class="label-text">Contact phone</span></label>
      <input
        id="contactPhone"
        [formField]="fields.contactPhone"
        type="text"
        class="input input-bordered"
      />
    </div>
  </div>

  <div class="form-control">
    <label class="label" for="jobPosting"><span class="label-text">Job posting</span></label>
    <textarea
      id="jobPosting"
      [formField]="fields.jobPosting"
      rows="6"
      class="textarea textarea-bordered"
    ></textarea>
  </div>

  <div class="flex gap-2 justify-end">
    <button type="button" class="btn btn-ghost" (click)="cancel()">Cancel</button>
    <button type="button" class="btn btn-primary" (click)="onSubmit()">Save</button>
  </div>
</form>
```

- [ ] **Step 3: Update application-detail.component.ts**

In `application-detail.component.ts`:
1. Remove `import { FormGroup } from '@angular/forms';`
2. Import `ApplicationFormData` from the form component
3. Change `onSave(form: FormGroup)` to `onSave(formData: ApplicationFormData)`:

```typescript
import { ApplicationFormData } from '../../components/application-form/application-form.component';

// ...

protected async onSave(formData: ApplicationFormData): Promise<void> {
  try {
    if (this.isNew()) {
      const created = await firstValueFrom(this.api.createApplication(formData));
      await this.router.navigate(['/applications', created.id]);
    } else {
      await firstValueFrom(this.api.updateApplication(this.applicationId()!, formData));
      this.applicationResource.reload();
      this.editMode.set(false);
    }
  } catch (err: unknown) {
    const apiErr = err as { errors?: Record<string, string[]> };
    this.serverErrors.set(apiErr?.errors ?? {});
  }
}
```

- [ ] **Step 4: Build and verify**

```bash
cd frontend && npx ng build --configuration production
```
Expected: no errors.

- [ ] **Step 5: Start dev server and verify the form manually**

```bash
cd frontend && npm start
```

Open `http://localhost:4200/applications/new`. Verify:
- Submitting empty form shows "Company name is required." and "Role is required." errors.
- Filling in Company name + Role + selecting Draft status and saving redirects to the detail page.
- Editing an existing application pre-populates the form fields.
- Cancel button works on both new and edit.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/
git commit -m "feat: convert application-form to Angular signal forms"
```

---

## Task 9: Frontend — convert notes component to signal forms

Replaces raw signal + manual `[value]`/`(input)` bindings with `form()` + `[formField]`. Removes the outer `<div>` wrapper.

**Files:**
- Modify: `frontend/src/app/features/applications/components/application-notes/application-notes.component.ts`
- Modify: `frontend/src/app/features/applications/components/application-notes/application-notes.component.html`

**Interfaces:**
- Consumes: `form()`, `schema()`, `required()`, `submit()`, `FormField`, `FormRoot` from `@angular/forms`

- [ ] **Step 1: Rewrite application-notes.component.ts**

Replace the entire file:

```typescript
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, required, schema, submit } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApplicationsService } from '../../../../generated/api';
import { ApplicationNote } from '../../models/application-note.model';

@Component({
  selector: 'app-application-notes',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './application-notes.component.html',
})
export class ApplicationNotesComponent implements OnInit {
  readonly applicationId = input.required<string>();
  readonly initialNotes = input<ApplicationNote[]>([]);

  private readonly api = inject(ApplicationsService);

  protected notes = signal<ApplicationNote[]>([]);
  protected deleteConfirmId = signal<string | null>(null);
  protected editingNoteId = signal<string | null>(null);

  protected readonly newNoteModel = signal({ body: '' });
  protected readonly newNoteFields = form(this.newNoteModel, schema(({ fields }) => {
    required(fields.body);
  }));

  protected readonly editingModel = signal({ body: '' });
  protected readonly editingFields = form(this.editingModel, schema(({ fields }) => {
    required(fields.body);
  }));

  ngOnInit(): void {
    this.notes.set([...this.initialNotes()]);
  }

  protected async addNote(): Promise<void> {
    await submit(this.newNoteFields, async () => {
      const note = await firstValueFrom(
        this.api.addApplicationNote(this.applicationId(), { body: this.newNoteModel().body }),
      );
      this.notes.update((n) => [note, ...n]);
      this.newNoteModel.set({ body: '' });
      return undefined;
    });
  }

  protected startEdit(note: ApplicationNote): void {
    this.editingNoteId.set(note.id);
    this.editingModel.set({ body: note.body });
  }

  protected cancelEdit(): void {
    this.editingNoteId.set(null);
    this.editingModel.set({ body: '' });
  }

  protected async saveEdit(noteId: string): Promise<void> {
    await submit(this.editingFields, async () => {
      const updated = await firstValueFrom(
        this.api.updateApplicationNote(this.applicationId(), noteId, {
          body: this.editingModel().body,
        }),
      );
      this.notes.update((notes) => notes.map((n) => (n.id === noteId ? updated : n)));
      this.editingNoteId.set(null);
      return undefined;
    });
  }

  protected async deleteNote(noteId: string): Promise<void> {
    await firstValueFrom(this.api.deleteApplicationNote(this.applicationId(), noteId));
    this.notes.update((notes) => notes.filter((n) => n.id !== noteId));
    this.deleteConfirmId.set(null);
  }

  protected isEdited(note: ApplicationNote): boolean {
    return note.updatedAt !== note.createdAt;
  }
}
```

- [ ] **Step 2: Rewrite application-notes.component.html**

Replace the entire file (note: no wrapping `<div>` — reviewer requested its removal):

```html
<h2 class="text-lg font-semibold mb-4">Notes</h2>

<form [formRoot]="newNoteFields" class="form-control mb-4">
  <textarea
    class="textarea textarea-bordered"
    rows="3"
    placeholder="Add a note…"
    [formField]="newNoteFields.body"
  ></textarea>
  <div class="mt-2 flex justify-end">
    <button
      type="button"
      class="btn btn-sm btn-primary"
      [disabled]="!newNoteModel().body.trim()"
      (click)="addNote()"
    >
      Add note
    </button>
  </div>
</form>

@if (!notes().length) {
  <p class="text-base-content/50 text-sm text-center py-6">No notes yet.</p>
}

<div class="space-y-3">
  @for (note of notes(); track note.id) {
    <div class="card bg-base-200 p-4">
      @if (editingNoteId() === note.id) {
        <form [formRoot]="editingFields">
          <textarea
            class="textarea textarea-bordered w-full mb-2"
            rows="3"
            [formField]="editingFields.body"
          ></textarea>
          <div class="flex gap-2 justify-end">
            <button class="btn btn-ghost btn-xs" type="button" (click)="cancelEdit()">Cancel</button>
            <button class="btn btn-primary btn-xs" type="button" (click)="saveEdit(note.id)">Save</button>
          </div>
        </form>
      } @else {
        <p class="text-sm whitespace-pre-wrap mb-2">{{ note.body }}</p>
        <div class="flex items-center justify-between text-xs text-base-content/50">
          <span>
            {{ note.createdAt | date: 'medium' }}
            @if (isEdited(note)) {
              <span class="ml-1">(edited {{ note.updatedAt | date: 'medium' }})</span>
            }
          </span>
          <div class="flex gap-2">
            <button class="btn btn-ghost btn-xs" (click)="startEdit(note)">Edit</button>
            @if (deleteConfirmId() === note.id) {
              <button class="btn btn-error btn-xs" (click)="deleteNote(note.id)">
                Confirm delete
              </button>
              <button class="btn btn-ghost btn-xs" (click)="deleteConfirmId.set(null)">
                Cancel
              </button>
            } @else {
              <button
                class="btn btn-ghost btn-xs text-error"
                (click)="deleteConfirmId.set(note.id)"
              >
                Delete
              </button>
            }
          </div>
        </div>
      }
    </div>
  }
</div>
```

- [ ] **Step 3: Build and verify**

```bash
cd frontend && npx ng build --configuration production
```
Expected: no errors.

- [ ] **Step 4: Smoke test the notes UI**

With the dev server running, navigate to an existing application's detail page. Verify:
- Add note textarea uses `[formField]` binding (no `[value]`/`(input)` attributes in the DOM).
- Adding a note appends it to the list and clears the textarea.
- Editing a note shows the pre-filled textarea; save updates the note; cancel discards the change.
- Delete shows confirmation; confirming removes the note.
- The "Notes" heading renders correctly (outer `<div>` removed — check that no layout broke).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/
git commit -m "feat: convert application-notes to Angular signal forms, remove outer div wrapper"
```

---

## Task 10: Docs, future notes & GitHub replies

Captures two "note for later" items, replies to the reviewer's open question about `errors`.

**Files:**
- Create: `docs/specs/future-enhancements.md`

- [ ] **Step 1: Create future-enhancements.md**

Create `docs/specs/future-enhancements.md`:

```markdown
# Future Enhancements

Items noted during code review as valuable but out of scope for the current story.

## Job posting enrichment

Currently `JobPosting` is stored as a plain URL string. A future story should:
- Extract structured metadata (company name, role title, salary range) from the URL.
- Store and display extracted metadata alongside the raw URL.
- Possibly support pasting raw job description text as an alternative input.

## Frontend form validation

Form fields currently rely solely on backend validation (FluentValidation returning 400s). A future story should:
- Add client-side validation to the application form (required, max length) using signal forms validators.
- Display field-level errors inline, not just as a generic "Could not save" banner.
- Mirror backend max-length constraints so the UX fails fast without a round-trip.
```

- [ ] **Step 2: Reply to GitHub comment about `errors` in ProblemDetails interceptor**

Use `gh api` to reply to comment ID `3582649504`:

```bash
gh api repos/ugGit/yaam/pulls/comments/3582649504/replies \
  --method POST \
  --field body="The \`errors\` field is populated on FluentValidation 400 responses — the backend returns \`ValidationProblemDetails\` with a field-keyed \`errors\` dictionary. It is read in \`application-detail.component.ts:55-56\` to populate \`serverErrors\`, which drives the 'Could not save' alert in the detail template."
```

- [ ] **Step 3: Reply to GitHub comment about signal forms**

Reply to comment ID `3584692729`:

```bash
gh api repos/ugGit/yaam/pulls/comments/3584692729/replies \
  --method POST \
  --field body="Done — ADR 004 (\`docs/architecture/decisions/004-angular-signal-forms.md\`) establishes signal forms as the only permitted form API. Both \`application-form\` and \`application-notes\` are now converted to \`form()\` + \`[formField]\` in this PR."
```

- [ ] **Step 4: Commit**

```bash
git add docs/specs/future-enhancements.md
git commit -m "docs: add future-enhancements notes from PR review"
```

---

## Self-Review Checklist

**Spec coverage:**
- ✅ ct → cancellationToken, = default removed (Task 1)
- ✅ Multi-line command instantiation (Task 1)
- ✅ request → command/query in handlers (Task 1)
- ✅ Aliases removed (Task 1)
- ✅ Backend naming guideline updated (Task 1)
- ✅ Input models + split NoteBodyRequest (Task 2)
- ✅ ADR 005 input models (Task 2)
- ✅ NotFoundException + middleware (Task 3)
- ✅ Mapper class (Task 4)
- ✅ required keyword on entity strings (Task 5)
- ✅ Role MaxLength 200→350, ContactPhone 50→20 (Task 5)
- ✅ DB migration (Task 5)
- ✅ Status sort removed, sort field validated, throw on unknown (Task 6)
- ✅ Test credentials in appsettings (Task 7)
- ✅ DELETE tests verify entity removal (Task 7)
- ✅ Trivial validator tests removed (Task 7)
- ✅ application-form converted to signal forms (Task 8)
- ✅ application-detail updated for new output type (Task 8)
- ✅ application-notes converted to signal forms (Task 9)
- ✅ Outer div removed from notes template (Task 9)
- ✅ ADR 004 angular signal forms (already written before this plan)
- ✅ Future enhancements noted (Task 10)
- ✅ GitHub replies posted (Task 10)
