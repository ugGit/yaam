import { Component, inject, input, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileViewModel, ProfileService, CertificationViewModel } from '../../../../generated/api';
import {
  CertificationModalComponent,
  CertificationFormData,
} from '../certification-modal/certification-modal.component';

@Component({
  selector: 'app-certification-section',
  standalone: true,
  imports: [CommonModule, CertificationModalComponent],
  templateUrl: './certification-section.component.html',
})
export class CertificationSectionComponent {
  readonly items = input.required<CertificationViewModel[]>();
  readonly changed = output<ProfileViewModel>();

  private readonly profileService = inject(ProfileService);
  private readonly modal = viewChild.required<CertificationModalComponent>('modal');

  protected readonly editingItem = signal<CertificationViewModel | null>(null);
  protected readonly deletingId = signal<string | null>(null);

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modal().show();
  }

  protected onEdit(item: CertificationViewModel): void {
    this.editingItem.set(item);
    this.modal().show();
  }

  protected onModalDismissed(): void {
    this.editingItem.set(null);
  }

  protected async onSaved(data: CertificationFormData): Promise<void> {
    const id = this.editingItem()?.id;
    const payload = {
      name: data.name,
      issuer: data.issuer || null,
      date: data.date,
    };
    if (id) {
      await firstValueFrom(this.profileService.updateCertification(id, payload));
    } else {
      await firstValueFrom(this.profileService.addCertification(payload));
    }
    const profile = await firstValueFrom(this.profileService.getProfile());
    this.changed.emit(profile);
  }

  protected onDeleteStart(id: string): void {
    this.deletingId.set(id);
  }

  protected onDeleteCancel(): void {
    this.deletingId.set(null);
  }

  protected async onDeleteConfirm(id: string): Promise<void> {
    try {
      await firstValueFrom(this.profileService.deleteCertification(id));
      const profile = await firstValueFrom(this.profileService.getProfile());
      this.changed.emit(profile);
    } finally {
      this.deletingId.set(null);
    }
  }
}
