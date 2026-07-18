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
