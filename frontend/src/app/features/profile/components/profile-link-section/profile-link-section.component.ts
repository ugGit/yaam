import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileService, ProfileLinkDto } from '../../../../generated/api';
import { ProfileLinkModalComponent, ProfileLinkFormData } from '../profile-link-modal/profile-link-modal.component';

@Component({
  selector: 'app-profile-link-section',
  standalone: true,
  imports: [CommonModule, ProfileLinkModalComponent],
  templateUrl: './profile-link-section.component.html',
})
export class ProfileLinkSectionComponent {
  readonly items = input.required<ProfileLinkDto[]>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  protected readonly modalOpen = signal(false);
  protected readonly editingItem = signal<ProfileLinkDto | null>(null);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modalOpen.set(true);
  }

  protected onEdit(item: ProfileLinkDto): void {
    this.editingItem.set(item);
    this.modalOpen.set(true);
  }

  protected onModalDismissed(): void {
    this.modalOpen.set(false);
    this.editingItem.set(null);
  }

  protected async onSaved(data: ProfileLinkFormData): Promise<void> {
    const id = this.editingItem()?.id;
    this.saving.set(true);
    try {
      const payload = {
        label: data.label,
        url: data.url,
      };
      if (id) {
        await firstValueFrom(this.profileService.updateProfileLink(id, payload));
      } else {
        await firstValueFrom(this.profileService.addProfileLink(payload));
      }
      this.modalOpen.set(false);
      this.editingItem.set(null);
      this.changed.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected onDeleteStart(id: string): void {
    this.deletingId.set(id);
  }

  protected onDeleteCancel(): void {
    this.deletingId.set(null);
  }

  protected async onDeleteConfirm(id: string): Promise<void> {
    try {
      await firstValueFrom(this.profileService.deleteProfileLink(id));
      this.changed.emit();
    } finally {
      this.deletingId.set(null);
    }
  }
}
