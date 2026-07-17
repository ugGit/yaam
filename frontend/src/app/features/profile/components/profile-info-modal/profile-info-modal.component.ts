import {
  Component,
  ElementRef,
  effect,
  inject,
  input,
  linkedSignal,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
import { ProfileDto, ProfileService } from '../../../../generated/api';

@Component({
  selector: 'app-profile-info-modal',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './profile-info-modal.component.html',
})
export class ProfileInfoModalComponent {
  readonly open = input.required<boolean>();
  readonly profile = input.required<ProfileDto>();
  readonly saved = output<ProfileDto>();
  readonly dismissed = output<void>();

  private readonly profileService = inject(ProfileService);
  private readonly dialogEl = viewChild<ElementRef<HTMLDialogElement>>('modal');

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

  constructor() {
    effect(() => {
      const dialogEl = this.dialogEl()?.nativeElement;
      if (!dialogEl) return;
      if (this.open()) {
        dialogEl.showModal();
      } else {
        dialogEl.close();
      }
    });
  }

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.saving.set(true);
      try {
        const model = this.formModel();
        await firstValueFrom(
          this.profileService.updateProfileInfo({
            firstName: model.firstName,
            lastName: model.lastName,
            email: model.email,
            phone: model.phone,
            location: model.location || null,
            summary: model.summary || null,
          }),
        );
        this.serverErrors.set({});
        const profile = await firstValueFrom(this.profileService.getProfile());
        this.saved.emit(profile);
      } catch (err: unknown) {
        const apiErr = err as { errors?: Record<string, string[]> };
        this.serverErrors.set(apiErr?.errors ?? {});
      } finally {
        this.saving.set(false);
      }
    });
  }

  protected onClose(): void {
    this.serverErrors.set({});
    this.dismissed.emit();
  }
}
