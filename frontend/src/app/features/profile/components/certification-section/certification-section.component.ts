import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileService, CertificationDto } from '../../../../generated/api';
import { CertificationModalComponent, CertificationFormData } from '../certification-modal/certification-modal.component';

@Component({
  selector: 'app-certification-section',
  standalone: true,
  imports: [CommonModule, CertificationModalComponent],
  templateUrl: './certification-section.component.html',
})
export class CertificationSectionComponent {
  readonly items = input.required<CertificationDto[]>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  protected readonly modalOpen = signal(false);
  protected readonly editingItem = signal<CertificationDto | null>(null);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modalOpen.set(true);
  }

  protected onEdit(item: CertificationDto): void {
    this.editingItem.set(item);
    this.modalOpen.set(true);
  }

  protected onModalDismissed(): void {
    this.modalOpen.set(false);
    this.editingItem.set(null);
  }

  protected async onSaved(data: CertificationFormData): Promise<void> {
    const id = this.editingItem()?.id;
    this.saving.set(true);
    try {
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
      await firstValueFrom(this.profileService.deleteCertification(id));
      this.changed.emit();
    } finally {
      this.deletingId.set(null);
    }
  }
}
