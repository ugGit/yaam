import { Component, inject, input, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileDto, ProfileService, EducationDto } from '../../../../generated/api';
import {
  EducationModalComponent,
  EducationFormData,
} from '../education-modal/education-modal.component';

@Component({
  selector: 'app-education-section',
  standalone: true,
  imports: [CommonModule, EducationModalComponent],
  templateUrl: './education-section.component.html',
})
export class EducationSectionComponent {
  readonly items = input.required<EducationDto[]>();
  readonly changed = output<ProfileDto>();

  private readonly profileService = inject(ProfileService);
  private readonly modal = viewChild.required<EducationModalComponent>('modal');

  protected readonly editingItem = signal<EducationDto | null>(null);
  protected readonly deletingId = signal<string | null>(null);

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modal().show();
  }

  protected onEdit(item: EducationDto): void {
    this.editingItem.set(item);
    this.modal().show();
  }

  protected onModalDismissed(): void {
    this.editingItem.set(null);
  }

  protected async onSaved(data: EducationFormData): Promise<void> {
    const id = this.editingItem()?.id;
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
      await firstValueFrom(this.profileService.deleteEducation(id));
      const profile = await firstValueFrom(this.profileService.getProfile());
      this.changed.emit(profile);
    } finally {
      this.deletingId.set(null);
    }
  }

  protected degreeAndField(item: EducationDto): string {
    return [item.degree, item.fieldOfStudy].filter((value): value is string => !!value).join(', ');
  }
}
