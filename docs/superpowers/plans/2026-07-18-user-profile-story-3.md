# User Profile — Story 3: Custom Fields CRUD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add backend CRUD (commands + endpoints + tests), regenerate the API client, then build `CustomFieldModalComponent` and update `CustomFieldsSectionComponent` so users can add, edit, and delete custom key-value fields from their profile.

**Architecture:** Three MediatR commands in `Yaam.UseCases/Profile/Commands/CustomFields/` handle add/update/delete; `ProfileController` gains three matching endpoints that return `CustomFieldViewModel` via the existing `ProfileViewModelMapper`. The frontend adds a `CustomFieldModalComponent` (mirror of `ProfileLinkModalComponent`) and upgrades the existing read-only `CustomFieldsSectionComponent` to full CRUD, emitting `ProfileViewModel` on every mutation.

**Tech Stack:** .NET 10, EF Core, MediatR, FluentValidation, Angular 22, Angular Signals, daisyUI

## Global Constraints

- Handler parameters: `command` for `IRequestHandler<TCommand>` — never `request`
- `CancellationToken` always named `cancellationToken`, never `= default`
- `mediator.Send` formatting: command on its own line, `cancellationToken` on third line
- Controllers return `*ViewModel` (never `*Dto` directly); use `ProfileViewModelMapper.ToViewModel(dto)`
- No type aliases — resolve ambiguity by restructuring
- All profile endpoints under `/api/profile`
- `frontend/src/app/generated/` — never edit manually; regenerate with `npm run generate:api`
- Integration tests use real DB, `[Collection("Integration")]` + `ApiFactory` pattern
- Angular standalone components, signals-first; `firstValueFrom()` to bridge Observables
- Angular signal forms only: `form()` / `[formField]` — no `ReactiveFormsModule` or `FormGroup`
- Invoke the `daisyui` skill before writing any component HTML
- After frontend implementation, invoke the `verify` skill for browser verification
- Commit messages follow Conventional Commits (`feat:`, `test:`, `chore:`, etc.)

---

## File Structure

**Create:**
```
backend/Yaam.UseCases/Profile/Commands/CustomFields/AddCustomFieldCommand.cs
backend/Yaam.UseCases/Profile/Commands/CustomFields/UpdateCustomFieldCommand.cs
backend/Yaam.UseCases/Profile/Commands/CustomFields/DeleteCustomFieldCommand.cs
frontend/src/app/features/profile/components/custom-field-modal/custom-field-modal.component.ts
frontend/src/app/features/profile/components/custom-field-modal/custom-field-modal.component.html
```

**Modify:**
```
backend/Yaam.API/Controllers/ProfileController.cs          — add 3 endpoints + CustomFieldInputModel
backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs  — add 3 tests
frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.ts
frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.html
frontend/src/app/features/profile/pages/profile-page/profile-page.component.html  — wire (changed)
frontend/src/app/generated/api/                            — regenerated, never edit manually
```

---

### Task 1: Backend commands, controller endpoints, integration tests

**Files:**
- Create: `backend/Yaam.UseCases/Profile/Commands/CustomFields/AddCustomFieldCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/CustomFields/UpdateCustomFieldCommand.cs`
- Create: `backend/Yaam.UseCases/Profile/Commands/CustomFields/DeleteCustomFieldCommand.cs`
- Modify: `backend/Yaam.API/Controllers/ProfileController.cs`
- Modify: `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs`

**Interfaces:**
- Consumes: `IProfileRepository.GetOrCreateAsync(cancellationToken)`, `IProfileRepository.UpdateAsync(cancellationToken)`, `ProfileMapper.ToDto(CustomField cf)` (already in `ProfileMapper.cs`), `ProfileViewModelMapper.ToViewModel(CustomFieldDto dto)` (already in `ProfileViewModelMapper.cs`)
- Produces:
  - `POST /api/profile/custom-fields` → 200 `CustomFieldViewModel { Id, Label, Value }`
  - `PUT /api/profile/custom-fields/{id}` → 200 `CustomFieldViewModel { Id, Label, Value }`
  - `DELETE /api/profile/custom-fields/{id}` → 204

- [ ] **Step 1: Write the failing tests**

Add these three test methods to `backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs` inside the existing `ProfileEndpointsTests` class:

```csharp
[Fact]
public async Task CustomFieldCrudFlow()
{
    // Add
    var addPayload = new { label = "Salary expectation", value = "CHF 120k" };
    var addResponse = await _client.PostAsJsonAsync("/api/profile/custom-fields", addPayload);
    addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var added = await addResponse.Content.ReadFromJsonAsync<CustomFieldViewModel>();
    added!.Label.Should().Be("Salary expectation");
    added.Value.Should().Be("CHF 120k");

    // Update
    var updatePayload = new { label = "Salary expectation", value = "CHF 130k" };
    var updateResponse = await _client.PutAsJsonAsync(
        $"/api/profile/custom-fields/{added.Id}", updatePayload);
    updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var updated = await updateResponse.Content.ReadFromJsonAsync<CustomFieldViewModel>();
    updated!.Value.Should().Be("CHF 130k");

    // Delete
    var deleteResponse = await _client.DeleteAsync($"/api/profile/custom-fields/{added.Id}");
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

    // Verify gone
    var profile = await (await _client.GetAsync("/api/profile"))
        .Content.ReadFromJsonAsync<ProfileViewModel>();
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

- [ ] **Step 2: Run tests — expect failure**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "CustomField"
```

Expected: FAIL — 404 responses (endpoints don't exist yet).

- [ ] **Step 3: Create AddCustomFieldCommand**

Create `backend/Yaam.UseCases/Profile/Commands/CustomFields/AddCustomFieldCommand.cs`:

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

Create `backend/Yaam.UseCases/Profile/Commands/CustomFields/UpdateCustomFieldCommand.cs`:

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

Create `backend/Yaam.UseCases/Profile/Commands/CustomFields/DeleteCustomFieldCommand.cs`:

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

- [ ] **Step 6: Add endpoints and input model to ProfileController**

In `backend/Yaam.API/Controllers/ProfileController.cs`:

Add to the using block at the top:
```csharp
using Yaam.UseCases.Profile.Commands.CustomFields;
```

Add these three action methods before the closing `}` of the `ProfileController` class (after the `DeleteLink` action, before the closing brace at line 220):

```csharp
    [HttpPost("custom-fields")]
    [EndpointName("AddCustomField")]
    public async Task<ActionResult<CustomFieldViewModel>> AddCustomField(
        [FromBody] CustomFieldInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddCustomFieldCommand(input.Label, input.Value),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
    }

    [HttpPut("custom-fields/{id:guid}")]
    [EndpointName("UpdateCustomField")]
    public async Task<ActionResult<CustomFieldViewModel>> UpdateCustomField(
        Guid id, [FromBody] CustomFieldInputModel input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateCustomFieldCommand(id, input.Label, input.Value),
            cancellationToken);
        return Ok(ProfileViewModelMapper.ToViewModel(result));
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
```

Add this input model record at the bottom of the file after `ProfileLinkInputModel`:

```csharp
public record CustomFieldInputModel(string Label, string Value);
```

- [ ] **Step 7: Run tests — expect pass**

```bash
dotnet test backend/Yaam.Tests.Integration --filter "CustomField"
```

Expected: All three tests PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/Yaam.UseCases/Profile/Commands/CustomFields \
        backend/Yaam.API/Controllers/ProfileController.cs \
        backend/Yaam.Tests.Integration/Profile/ProfileEndpointsTests.cs
git commit -m "feat: add custom field CRUD commands and endpoints"
```

---

### Task 2: Regenerate API client

**Files:**
- `frontend/src/app/generated/api/` — fully regenerated (never edit manually)

**Interfaces:**
- Produces: `ProfileService` gains `addCustomField(body)`, `updateCustomField(id, body)`, `deleteCustomField(id)` methods; `CustomFieldViewModel` type available as an import from `'../../../../generated/api'`

- [ ] **Step 1: Start the backend**

```bash
dotnet run --project backend/Yaam.API
```

Wait until the backend prints `Now listening on: https://localhost:7131` (or similar). Confirm the three new endpoints appear at `https://localhost:7131/scalar/v1` under the Profile tag: `AddCustomField`, `UpdateCustomField`, `DeleteCustomField`.

- [ ] **Step 2: Regenerate**

In a separate terminal, from the `frontend/` directory:

```bash
npm run generate:api
```

Expected: Files written under `src/app/generated/api/`. No errors.

- [ ] **Step 3: Verify new methods exist**

Open `frontend/src/app/generated/api/api/profile.service.ts`. Confirm these method names exist:
- `addCustomField(`
- `updateCustomField(`
- `deleteCustomField(`

- [ ] **Step 4: TypeScript build check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors. Fix any import-path issues if the generator moved files.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/generated
git commit -m "chore: regenerate API client with custom field endpoints"
```

---

### Task 3: CustomFieldModalComponent + updated CustomFieldsSectionComponent + profile page wiring

**Files:**
- Create: `frontend/src/app/features/profile/components/custom-field-modal/custom-field-modal.component.ts`
- Create: `frontend/src/app/features/profile/components/custom-field-modal/custom-field-modal.component.html`
- Modify: `frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.ts`
- Modify: `frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.html`
- Modify: `frontend/src/app/features/profile/pages/profile-page/profile-page.component.html`

**Interfaces:**
- Consumes (modal): `CustomFieldViewModel` from `'../../../../generated/api'`; `FormField`, `form`, `required`, `submit` from `'@angular/forms/signals'`; `linkedSignal` from `'@angular/core'`
- Consumes (section): `ProfileService`, `ProfileViewModel`, `CustomFieldViewModel` from `'../../../../generated/api'`; `CustomFieldModalComponent` and `CustomFieldFormData` from the modal
- Produces (modal): `show()` method; `saved` output emitting `CustomFieldFormData { label: string; value: string }`; `dismissed` output
- Produces (section): `changed` output emitting `ProfileViewModel`

- [ ] **Step 1: Invoke daisyUI skill**

Run the `daisyui` skill to get current component guidance before writing any HTML.

- [ ] **Step 2: Create CustomFieldModalComponent TypeScript**

Create `frontend/src/app/features/profile/components/custom-field-modal/custom-field-modal.component.ts`:

```typescript
import { Component, ElementRef, input, linkedSignal, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, required, submit } from '@angular/forms/signals';
import { CustomFieldViewModel } from '../../../../generated/api';

export interface CustomFieldFormData {
  label: string;
  value: string;
}

@Component({
  selector: 'app-custom-field-modal',
  standalone: true,
  imports: [CommonModule, FormField],
  templateUrl: './custom-field-modal.component.html',
})
export class CustomFieldModalComponent {
  readonly item = input<CustomFieldViewModel | null>(null);
  readonly saved = output<CustomFieldFormData>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('modal');

  protected readonly formModel = linkedSignal<CustomFieldFormData>(() => ({
    label: this.item()?.label ?? '',
    value: this.item()?.value ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.label, { message: 'Label is required.' });
    required(f.value, { message: 'Value is required.' });
  });

  show(): void {
    this.dialogEl().nativeElement.showModal();
  }

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.saved.emit(this.formModel());
      this.dialogEl().nativeElement.close();
    });
  }

  protected onClose(): void {
    this.dismissed.emit();
  }

  protected get isEditMode(): boolean {
    return this.item() !== null;
  }
}
```

- [ ] **Step 3: Create CustomFieldModalComponent template**

Create `frontend/src/app/features/profile/components/custom-field-modal/custom-field-modal.component.html`:

```html
<dialog #modal class="modal" (close)="onClose()">
  <div class="modal-box">
    <h3 class="font-bold text-lg mb-4">{{ isEditMode ? 'Edit' : 'Add' }} Custom Field</h3>

    <div class="flex flex-col gap-3">
      <div class="form-control">
        <label class="label" for="cf-label"><span class="label-text">Label *</span></label>
        <input
          id="cf-label"
          [formField]="fields.label"
          type="text"
          class="input input-bordered w-full"
          maxlength="250"
        />
        @if (fields.label().touched() && fields.label().errors().length) {
          <div class="label">
            <span class="label-text-alt text-error">{{ fields.label().errors()[0].message }}</span>
          </div>
        }
      </div>

      <div class="form-control">
        <label class="label" for="cf-value"><span class="label-text">Value *</span></label>
        <textarea
          id="cf-value"
          [formField]="fields.value"
          class="textarea textarea-bordered w-full"
          rows="3"
          maxlength="1000"
        ></textarea>
        @if (fields.value().touched() && fields.value().errors().length) {
          <div class="label">
            <span class="label-text-alt text-error">{{ fields.value().errors()[0].message }}</span>
          </div>
        }
      </div>
    </div>

    <div class="modal-action">
      <form method="dialog">
        <button class="btn btn-ghost">Cancel</button>
      </form>
      <button type="button" class="btn btn-primary" (click)="onSubmit()">Save</button>
    </div>
  </div>
  <form method="dialog" class="modal-backdrop">
    <button aria-label="Close modal"></button>
  </form>
</dialog>
```

- [ ] **Step 4: Update CustomFieldsSectionComponent TypeScript**

Replace the entire contents of `frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.ts`:

```typescript
import { Component, inject, input, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { CustomFieldViewModel, ProfileService, ProfileViewModel } from '../../../../generated/api';
import {
  CustomFieldModalComponent,
  CustomFieldFormData,
} from '../custom-field-modal/custom-field-modal.component';

@Component({
  selector: 'app-custom-fields-section',
  standalone: true,
  imports: [CommonModule, CustomFieldModalComponent],
  templateUrl: './custom-fields-section.component.html',
})
export class CustomFieldsSectionComponent {
  readonly fields = input.required<CustomFieldViewModel[]>();
  readonly changed = output<ProfileViewModel>();

  private readonly profileService = inject(ProfileService);
  private readonly modal = viewChild.required<CustomFieldModalComponent>('modal');

  protected readonly editingItem = signal<CustomFieldViewModel | null>(null);
  protected readonly deletingId = signal<string | null>(null);

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modal().show();
  }

  protected onEdit(item: CustomFieldViewModel): void {
    this.editingItem.set(item);
    this.modal().show();
  }

  protected onModalDismissed(): void {
    this.editingItem.set(null);
  }

  protected async onSaved(data: CustomFieldFormData): Promise<void> {
    const id = this.editingItem()?.id;
    const payload = { label: data.label, value: data.value };
    if (id) {
      await firstValueFrom(this.profileService.updateCustomField(id, payload));
    } else {
      await firstValueFrom(this.profileService.addCustomField(payload));
    }
    const profile = await firstValueFrom(this.profileService.getProfile());
    this.changed.emit(profile);
  }

  protected onDeleteStart(id: string): void {
    this.deletingId.set(id);
    this.editingItem.set(null);
  }

  protected onDeleteCancel(): void {
    this.deletingId.set(null);
  }

  protected async onDeleteConfirm(id: string): Promise<void> {
    try {
      await firstValueFrom(this.profileService.deleteCustomField(id));
      const profile = await firstValueFrom(this.profileService.getProfile());
      this.changed.emit(profile);
    } finally {
      this.deletingId.set(null);
    }
  }
}
```

- [ ] **Step 5: Update CustomFieldsSectionComponent template**

Replace the entire contents of `frontend/src/app/features/profile/components/custom-fields-section/custom-fields-section.component.html`:

```html
<div class="card bg-base-100 shadow">
  <div class="card-body">
    <div class="flex items-center justify-between">
      <h2 class="card-title text-lg">Additional Info</h2>
      <button class="btn btn-ghost btn-sm" (click)="onAdd()">+ Add</button>
    </div>

    @if (fields().length > 0) {
      <ul class="flex flex-col gap-2 mt-2">
        @for (field of fields(); track field.id) {
          <li>
            <div class="flex items-start justify-between gap-2">
              <div class="flex flex-col min-w-0">
                <span class="font-medium">{{ field.label }}</span>
                <span class="text-base-content/70 text-sm whitespace-pre-wrap">{{ field.value }}</span>
              </div>
              <div class="flex gap-1 shrink-0">
                @if (deletingId() === field.id) {
                  <button class="btn btn-error btn-xs" (click)="onDeleteConfirm(field.id)">
                    Delete
                  </button>
                  <button class="btn btn-ghost btn-xs" (click)="onDeleteCancel()">Cancel</button>
                } @else {
                  <button class="btn btn-ghost btn-xs" (click)="onEdit(field)">Edit</button>
                  <button class="btn btn-ghost btn-xs text-error" (click)="onDeleteStart(field.id)">
                    Delete
                  </button>
                }
              </div>
            </div>
          </li>
        }
      </ul>
    } @else {
      <p class="text-base-content/40 italic mt-2">No additional info added yet.</p>
    }
  </div>
</div>

<app-custom-field-modal
  #modal
  [item]="editingItem()"
  (saved)="onSaved($event)"
  (dismissed)="onModalDismissed()"
/>
```

- [ ] **Step 6: Wire (changed) in profile page template**

In `frontend/src/app/features/profile/pages/profile-page/profile-page.component.html`, change line 25 from:

```html
      <app-custom-fields-section [fields]="profile.customFields" />
```

to:

```html
      <app-custom-fields-section [fields]="profile.customFields" (changed)="onProfileChanged($event)" />
```

- [ ] **Step 7: TypeScript build check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors. If the generated service method names differ from `addCustomField`/`updateCustomField`/`deleteCustomField`, update the section component to match the actual generated names.

- [ ] **Step 8: Invoke verify skill**

Run the `verify` skill to confirm the feature works in the browser. Use the manual verification checklist:

1. Add with blank label → inline validation error shown in modal, no API call
2. Add with blank value → inline validation error shown in modal, no API call
3. Add valid (e.g. "Salary expectation" / "CHF 120k") → field appears in "Additional Info" list, modal closes
4. Click "Edit" on the new field → modal opens pre-populated with current label and value
5. Clear the label, click Save → validation error shown, field unchanged
6. Change the value, click Save → updated value shown in the list
7. Click "Delete" on a field → row shows "Delete" + "Cancel" buttons
8. Click "Cancel" → row returns to normal, nothing deleted
9. Click "Delete" again → confirm appears; click "Delete" → field removed from list

- [ ] **Step 9: Commit**

```bash
git add frontend/src/app/features/profile/components/custom-field-modal \
        frontend/src/app/features/profile/components/custom-fields-section \
        frontend/src/app/features/profile/pages/profile-page/profile-page.component.html
git commit -m "feat: custom field modal and section CRUD"
```
