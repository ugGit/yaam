import { Component, inject, input, linkedSignal, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
import { ProfileDto, ProfileService } from '../../../../generated/api/index';

@Component({
  selector: 'app-profile-info-section',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './profile-info-section.component.html',
})
export class ProfileInfoSectionComponent {
  readonly profile = input.required<ProfileDto>();
  readonly changed = output<ProfileDto>();

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
        const profile = await firstValueFrom(this.profileService.getProfile());
        this.changed.emit(profile);
      } catch (err: unknown) {
        const apiErr = err as { errors?: Record<string, string[]> };
        this.serverErrors.set(apiErr?.errors ?? {});
      } finally {
        this.saving.set(false);
      }
    });
  }
}
