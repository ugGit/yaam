# User Profile — Story 3: Add and Manage Custom Fields

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add custom key-value field CRUD (backend + frontend) so users can capture job-search-specific context like "Salary expectation: CHF 120k".

**Architecture:** The `CustomField` entity and `CustomFieldDto` already exist from Story 1. Story 3 adds three commands (Add/Update/Delete), three controller endpoints, integration tests, regenerates the API client, then wires inline add/edit/delete into `CustomFieldsSectionComponent` — no modals; all interaction stays in-place.

**Prerequisites:** Story 1 fully implemented — `CustomField` entity, `CustomFieldDto`, `ProfileMapper.ToDto(CustomField)`, `IProfileRepository`, read-only `CustomFieldsSectionComponent` with `fields` input.

**Tech Stack:** .NET 10, EF Core, MediatR, FluentValidation, Angular 22, Angular Signals, daisyUI

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
Yaam.UseCases/Profile/Commands/CustomFields/
  AddCustomFieldCommand.cs
  UpdateCustomFieldCommand.cs
  DeleteCustomFieldCommand.cs
```

**Backend — modify:**
```
Yaam.API/Controllers/ProfileController.cs
Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
```

**Frontend — modify:**
```
frontend/src/app/features/profile/components/custom-fields-section/
  custom-fields-section.component.ts
  custom-fields-section.component.html
```

---

### Task 1: Custom Field CRUD — commands, controller, tests

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/CustomFields/AddCustomFieldCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/CustomFields/UpdateCustomFieldCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/CustomFields/DeleteCustomFieldCommand.cs`
- Modify: `backend/Yaam.API/Controllers/ProfileController.cs`
- Modify: `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`

**Interfaces:**
- Consumes: `IProfileRepository.GetOrCreateAsync`, `IProfileRepository.UpdateAsync`, `ProfileMapper.ToDto(CustomField)`
- Produces: `POST /api/profile/custom-fields` → 200 `CustomFieldDto`; `PUT /api/profile/custom-fields/{id}` → 200 `CustomFieldDto`; `DELETE /api/profile/custom-fields/{id}` → 204

- [ ] **Step 1: Write failing tests**

Add to `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`:

```csharp
[Fact]
public async Task CustomFieldCrudFlow()
{
    // Add
    var addPayload = new { label = "Salary expectation", value = "CHF 120k" };
    var addResponse = await _client.PostAsJsonAsync("/api/profile/custom-fields", addPayload);
    addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var added = await addResponse.Content.ReadFromJsonAsync<CustomFieldDto>();
    added!.Label.Should().Be("Salary expectation");
    added.Value.Should().Be("CHF 120k");

    // Update
    var updatePayload = new { label = "Salary expectation", value = "CHF 130k" };
    var updateResponse = await _client.PutAsJsonAsync(
        $"/api/profile/custom-fields/{added.Id}", updatePayload);
    updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var updated = await updateResponse.Content.ReadFromJsonAsync<CustomFieldDto>();
    updated!.Value.Should().Be("CHF 130k");

    // Delete
    var deleteResponse = await _client.DeleteAsync($"/api/profile/custom-fields/{added.Id}");
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

    // Verify gone
    var profile = await (await _client.GetAsync("/api/profile"))
        .Content.ReadFromJsonAsync<ProfileDto>();
    profile!.CustomFields.Should().BeEmpty();
}

[Fact]
public async Task POST_CustomField_Returns400_WhenLabelEmpty()
{
    var payload = new { label = "", value = "some value" };
    var response = await _client.PostAsJsonAsync("/api/profile/custom-fields", payload);
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}

[Fact]
public async Task POST_CustomField_Returns400_WhenValueEmpty()
{
    var payload = new { label = "My Label", value = "" };
    var response = await _client.PostAsJsonAsync("/api/profile/custom-fields", payload);
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

- [ ] **Step 2: Run tests — expect fail**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "CustomField"
```

Expected: FAIL — 404 (endpoints do not exist yet).

- [ ] **Step 3: Create AddCustomFieldCommand**

`backend/Yaam.UseCases/Profile/Commands/CustomFields/AddCustomFieldCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.CustomFields;

public record AddCustomFieldCommand(string Label, string Value) : IRequest<CustomFieldDto>;

public class AddCustomFieldCommandValidator : AbstractValidator<AddCustomFieldCommand>
{
    public AddCustomFieldCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(1000);
    }
}

public class AddCustomFieldCommandHandler(IProfileRepository repository)
    : IRequestHandler<AddCustomFieldCommand, CustomFieldDto>
{
    public async Task<CustomFieldDto> Handle(AddCustomFieldCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = new CustomField
        {
            ProfileId = profile.Id,
            Label = command.Label,
            Value = command.Value,
        };
        profile.CustomFields.Add(entry);
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 4: Create UpdateCustomFieldCommand**

`backend/Yaam.UseCases/Profile/Commands/CustomFields/UpdateCustomFieldCommand.cs`
```csharp
using FluentValidation;
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;
using Yaam.UseCases.Profile.Dtos;

namespace Yaam.UseCases.Profile.Commands.CustomFields;

public record UpdateCustomFieldCommand(Guid Id, string Label, string Value) : IRequest<CustomFieldDto>;

public class UpdateCustomFieldCommandValidator : AbstractValidator<UpdateCustomFieldCommand>
{
    public UpdateCustomFieldCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(1000);
    }
}

public class UpdateCustomFieldCommandHandler(IProfileRepository repository)
    : IRequestHandler<UpdateCustomFieldCommand, CustomFieldDto>
{
    public async Task<CustomFieldDto> Handle(UpdateCustomFieldCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.CustomFields.FirstOrDefault(c => c.Id == command.Id)
            ?? throw new NotFoundException(nameof(CustomField), command.Id);
        entry.Label = command.Label;
        entry.Value = command.Value;
        await repository.UpdateAsync(cancellationToken);
        return ProfileMapper.ToDto(entry);
    }
}
```

- [ ] **Step 5: Create DeleteCustomFieldCommand**

`backend/Yaam.UseCases/Profile/Commands/CustomFields/DeleteCustomFieldCommand.cs`
```csharp
using MediatR;
using Yaam.Domain.Entities;
using Yaam.Domain.Errors;
using Yaam.Domain.Repositories;

namespace Yaam.UseCases.Profile.Commands.CustomFields;

public record DeleteCustomFieldCommand(Guid Id) : IRequest;

public class DeleteCustomFieldCommandHandler(IProfileRepository repository)
    : IRequestHandler<DeleteCustomFieldCommand>
{
    public async Task Handle(DeleteCustomFieldCommand command, CancellationToken cancellationToken)
    {
        var profile = await repository.GetOrCreateAsync(cancellationToken);
        var entry = profile.CustomFields.FirstOrDefault(c => c.Id == command.Id)
            ?? throw new NotFoundException(nameof(CustomField), command.Id);
        profile.CustomFields.Remove(entry);
        await repository.UpdateAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Add custom field actions to ProfileController**

Add to `backend/Yaam.API/Controllers/ProfileController.cs`:

```csharp
// add to usings:
using Yaam.UseCases.Profile.Commands.CustomFields;

// --- actions ---

[HttpPost("custom-fields")]
[EndpointName("AddCustomField")]
public async Task<IActionResult> AddCustomField(
    [FromBody] CustomFieldInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new AddCustomFieldCommand(input.Label, input.Value),
        cancellationToken);
    return Ok(result);
}

[HttpPut("custom-fields/{id:guid}")]
[EndpointName("UpdateCustomField")]
public async Task<IActionResult> UpdateCustomField(
    Guid id, [FromBody] CustomFieldInputModel input, CancellationToken cancellationToken)
{
    var result = await mediator.Send(
        new UpdateCustomFieldCommand(id, input.Label, input.Value),
        cancellationToken);
    return Ok(result);
}

[HttpDelete("custom-fields/{id:guid}")]
[EndpointName("DeleteCustomField")]
public async Task<IActionResult> DeleteCustomField(Guid id, CancellationToken cancellationToken)
{
    await mediator.Send(
        new DeleteCustomFieldCommand(id),
        cancellationToken);
    return NoContent();
}

// --- input model (add at bottom of file) ---

public record CustomFieldInputModel(string Label, string Value);
```

- [ ] **Step 7: Run tests — expect pass**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "CustomField"
```

Expected: PASS — all three tests green.

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.UseCases/Profile/Commands/CustomFields \
        backend/Yaam.API/Controllers/ProfileController.cs \
        backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
git commit -m "feat: add Custom Field CRUD endpoints"
```

---

### Task 2: Regenerate API client

**Files:**
- `frontend/src/app/generated/api/` — fully regenerated (never edit manually)

**Interfaces:**
- Produces: Generated `ProfileService` with `addCustomField`, `updateCustomField`, `deleteCustomField` methods available for Task 3.

- [ ] **Step 1: Start the backend**

```bash
dotnet run --project backend/Yaam.API
```

Confirm the three new endpoints appear in Scalar UI at `https://localhost:7131/scalar/v1` under the Profile tag.

- [ ] **Step 2: Regenerate**

```bash
cd frontend && npm run generate:api
```

Expected: Files written to `src/app/generated/api/`.

- [ ] **Step 3: Verify new methods exist**

Open `frontend/src/app/generated/api/api/profile.service.ts` (or the generated service file). Confirm these methods exist:
- `addCustomField(...)`
- `updateCustomField(id, ...)`
- `deleteCustomField(id)`

- [ ] **Step 4: Build check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors. Fix any import path issues if the generator changed output locations.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/generated frontend/src/app
git commit -m "chore: regenerate API client with custom field endpoints"
```

---

### Task 3: CustomFieldsSectionComponent — inline add, inline edit, two-step delete

**Files:**
- Modify: `frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.ts`
- Modify: `frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.html`

**Interfaces:**
- Consumes: `ProfileService.addCustomField`, `updateCustomField`, `deleteCustomField` from generated client; `fields` input (already exists from Story 1)
- Produces: `changed` output emitted after every successful mutation

This section does **not** use a modal. All interaction is inline:
- "Add custom field" button reveals a two-input form (label + value) inline at the bottom of the list.
- Each existing field has Edit and Delete buttons. Edit puts that row into inline edit mode (inputs replace the text). Only one row can be in edit mode at a time.
- Delete is two-step: first click shows "Delete" + "Cancel"; second click on "Delete" calls the API.

- [ ] **Step 1: Invoke daisyUI skill before writing HTML**

Run the `daisyui` skill to get current component guidance before implementing the template.

- [ ] **Step 2: Update CustomFieldsSectionComponent**

`frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.ts`
```typescript
import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileService } from '../../../../generated/api/api'; // adjust path to match generated output
import { CustomField } from '../../models/profile.model';

interface EditState {
  label: string;
  value: string;
}

@Component({
  selector: 'app-custom-fields-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-fields-section.component.html',
})
export class CustomFieldsSectionComponent {
  readonly fields = input.required<CustomField[]>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  // Add form state
  protected readonly addFormOpen = signal(false);
  protected readonly newLabel = signal('');
  protected readonly newValue = signal('');
  protected readonly addErrors = signal<string[]>([]);
  protected readonly addSaving = signal(false);

  // Per-item edit state
  protected readonly editingId = signal<string | null>(null);
  protected readonly editState = signal<EditState>({ label: '', value: '' });
  protected readonly editErrors = signal<string[]>([]);
  protected readonly editSaving = signal(false);

  // Per-item delete state
  protected readonly deletingId = signal<string | null>(null);

  // Add form
  protected onOpenAdd(): void {
    this.addFormOpen.set(true);
    this.newLabel.set('');
    this.newValue.set('');
    this.addErrors.set([]);
    this.editingId.set(null);
    this.deletingId.set(null);
  }

  protected onCancelAdd(): void {
    this.addFormOpen.set(false);
  }

  protected async onSaveAdd(): Promise<void> {
    const label = this.newLabel().trim();
    const value = this.newValue().trim();
    const errors: string[] = [];
    if (!label) errors.push('Label is required.');
    if (!value) errors.push('Value is required.');
    if (errors.length > 0) {
      this.addErrors.set(errors);
      return;
    }
    this.addSaving.set(true);
    try {
      await firstValueFrom(this.profileService.addCustomField({ label, value }));
      this.addFormOpen.set(false);
      this.changed.emit();
    } catch {
      this.addErrors.set(['Failed to save. Please try again.']);
    } finally {
      this.addSaving.set(false);
    }
  }

  // Per-item edit
  protected onEditItem(item: CustomField): void {
    this.editingId.set(item.id);
    this.editState.set({ label: item.label, value: item.value });
    this.editErrors.set([]);
    this.deletingId.set(null);
    this.addFormOpen.set(false);
  }

  protected onCancelEdit(): void {
    this.editingId.set(null);
  }

  protected async onSaveEdit(id: string): Promise<void> {
    const label = this.editState().label.trim();
    const value = this.editState().value.trim();
    const errors: string[] = [];
    if (!label) errors.push('Label is required.');
    if (!value) errors.push('Value is required.');
    if (errors.length > 0) {
      this.editErrors.set(errors);
      return;
    }
    this.editSaving.set(true);
    try {
      await firstValueFrom(this.profileService.updateCustomField(id, { label, value }));
      this.editingId.set(null);
      this.changed.emit();
    } catch {
      this.editErrors.set(['Failed to save. Please try again.']);
    } finally {
      this.editSaving.set(false);
    }
  }

  // Per-item delete
  protected onDeleteStart(id: string): void {
    this.deletingId.set(id);
    this.editingId.set(null);
    this.addFormOpen.set(false);
  }

  protected onDeleteCancel(): void {
    this.deletingId.set(null);
  }

  protected async onDeleteConfirm(id: string): Promise<void> {
    await firstValueFrom(this.profileService.deleteCustomField(id));
    this.deletingId.set(null);
    this.changed.emit();
  }
}
```

- [ ] **Step 3: Update CustomFieldsSectionComponent template**

`frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.html`
```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <div class="flex items-center justify-between">
      <h2 class="card-title text-lg">Additional Info</h2>
      @if (!addFormOpen()) {
        <button class="btn btn-ghost btn-sm" (click)="onOpenAdd()">+ Add custom field</button>
      }
    </div>

    @if (fields().length === 0 && !addFormOpen()) {
      <p class="text-base-content/40 italic mt-2">No custom fields yet.</p>
    }

    <ul class="flex flex-col gap-3 mt-2">
      @for (field of fields(); track field.id) {
        <li>
          @if (editingId() === field.id) {
            <div class="flex flex-col gap-2">
              <div class="flex gap-2">
                <div class="flex flex-col flex-1">
                  <input type="text" class="input input-bordered input-sm w-full"
                    placeholder="Label"
                    maxlength="250"
                    [value]="editState().label"
                    (input)="editState.update(s => ({ ...s, label: $any($event.target).value }))" />
                  <span class="text-xs text-base-content/40 text-right mt-0.5">
                    {{ editState().label.length }}/250
                  </span>
                </div>
                <div class="flex flex-col flex-1">
                  <input type="text" class="input input-bordered input-sm w-full"
                    placeholder="Value"
                    maxlength="1000"
                    [value]="editState().value"
                    (input)="editState.update(s => ({ ...s, value: $any($event.target).value }))" />
                  <span class="text-xs text-base-content/40 text-right mt-0.5">
                    {{ editState().value.length }}/1000
                  </span>
                </div>
              </div>
              @if (editErrors().length > 0) {
                <ul class="text-error text-sm list-disc list-inside">
                  @for (e of editErrors(); track e) { <li>{{ e }}</li> }
                </ul>
              }
              <div class="flex gap-2 justify-end">
                <button class="btn btn-ghost btn-xs" (click)="onCancelEdit()">Cancel</button>
                <button class="btn btn-primary btn-xs" [disabled]="editSaving()"
                  (click)="onSaveEdit(field.id)">
                  @if (editSaving()) { <span class="loading loading-spinner loading-xs"></span> }
                  Save
                </button>
              </div>
            </div>
          } @else {
            <div class="flex items-start justify-between gap-2">
              <p><span class="font-medium">{{ field.label }}:</span> {{ field.value }}</p>
              <div class="flex gap-1 shrink-0">
                @if (deletingId() === field.id) {
                  <button class="btn btn-error btn-xs" (click)="onDeleteConfirm(field.id)">Delete</button>
                  <button class="btn btn-ghost btn-xs" (click)="onDeleteCancel()">Cancel</button>
                } @else {
                  <button class="btn btn-ghost btn-xs" (click)="onEditItem(field)">Edit</button>
                  <button class="btn btn-ghost btn-xs text-error"
                    (click)="onDeleteStart(field.id)">Delete</button>
                }
              </div>
            </div>
          }
        </li>
      }
    </ul>

    @if (addFormOpen()) {
      <div class="flex flex-col gap-2 mt-3 pt-3 border-t border-base-300">
        <div class="flex gap-2">
          <div class="flex flex-col flex-1">
            <input type="text" class="input input-bordered input-sm w-full"
              placeholder="Label (e.g. Salary expectation)"
              maxlength="250"
              [value]="newLabel()"
              (input)="newLabel.set($any($event.target).value)" />
            <span class="text-xs text-base-content/40 text-right mt-0.5">
              {{ newLabel().length }}/250
            </span>
          </div>
          <div class="flex flex-col flex-1">
            <input type="text" class="input input-bordered input-sm w-full"
              placeholder="Value (e.g. CHF 120k)"
              maxlength="1000"
              [value]="newValue()"
              (input)="newValue.set($any($event.target).value)" />
            <span class="text-xs text-base-content/40 text-right mt-0.5">
              {{ newValue().length }}/1000
            </span>
          </div>
        </div>
        @if (addErrors().length > 0) {
          <ul class="text-error text-sm list-disc list-inside">
            @for (e of addErrors(); track e) { <li>{{ e }}</li> }
          </ul>
        }
        <div class="flex gap-2 justify-end">
          <button class="btn btn-ghost btn-sm" (click)="onCancelAdd()">Cancel</button>
          <button class="btn btn-primary btn-sm" [disabled]="addSaving()" (click)="onSaveAdd()">
            @if (addSaving()) { <span class="loading loading-spinner loading-sm"></span> }
            Save
          </button>
        </div>
      </div>
    }
  </div>
</div>
```

- [ ] **Step 4: Build check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 5: Manual verify**

Start the app (`npm start`) and navigate to `/profile`. Test each scenario:

1. Click "Add custom field" → add form appears at bottom
2. Submit with empty label → validation error shown, no API call made
3. Submit with empty value → validation error shown, no API call made
4. Fill in label and value, click Save → field appears in the list, add form closes
5. Click "Edit" on an existing field → inline inputs appear with current values
6. Clear the label, click Save → validation error shown inline, field unchanged
7. Change the value, click Save → updated value shown in the list
8. Click "Delete" on a field → Delete + Cancel buttons appear (first step)
9. Click "Cancel" → row returns to normal, nothing deleted
10. Click "Delete" again → Delete + Cancel reappear; click "Delete" → field removed from list

- [ ] **Step 6: Commit**

```bash
git add frontend/src/app/features/profile/components/custom-fields-section
git commit -m "feat: custom fields section — inline add, inline edit, two-step delete"
```
