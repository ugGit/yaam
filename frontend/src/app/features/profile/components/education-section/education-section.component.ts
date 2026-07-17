import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileService, EducationDto } from '../../../../generated/api';
import { EducationModalComponent, EducationFormData } from '../education-modal/education-modal.component';

@Component({
  selector: 'app-education-section',
  standalone: true,
  imports: [CommonModule, EducationModalComponent],
  templateUrl: './education-section.component.html',
})
export class EducationSectionComponent {
  readonly items = input.required<EducationDto[]>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  protected readonly modalOpen = signal(false);
  protected readonly editingItem = signal<EducationDto | null>(null);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modalOpen.set(true);
  }

  protected onEdit(item: EducationDto): void {
    this.editingItem.set(item);
    this.modalOpen.set(true);
  }

  protected onModalDismissed(): void {
    this.modalOpen.set(false);
    this.editingItem.set(null);
  }

  protected async onSaved(data: EducationFormData): Promise<void> {
    const id = this.editingItem()?.id;
    this.saving.set(true);
    try {
      const payload = {
        institution: data.institution,
        degree: data.degree || null,
        fieldOfStudy: data.fieldOfStudy || null,
        startDate: data.startDate || null,
        endDate: data.endDate || null,
      };
      if (id) {
        await firstValueFrom(this.profileService.updateEducation(id, payload));
      } else {
        await firstValueFrom(this.profileService.addEducation(payload));
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
      await firstValueFrom(this.profileService.deleteEducation(id));
      this.changed.emit();
    } finally {
      this.deletingId.set(null);
    }
  }

  protected degreeAndField(item: EducationDto): string {
    return [item.degree, item.fieldOfStudy].filter((v): v is string => !!v).join(', ');
  }
}
