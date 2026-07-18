import { Component, inject, input, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileDto, ProfileService, WorkExperienceDto } from '../../../../generated/api';
import {
  WorkExperienceModalComponent,
  WorkExperienceFormData,
} from '../work-experience-modal/work-experience-modal.component';

@Component({
  selector: 'app-work-experience-section',
  standalone: true,
  imports: [CommonModule, WorkExperienceModalComponent],
  templateUrl: './work-experience-section.component.html',
})
export class WorkExperienceSectionComponent {
  readonly items = input.required<WorkExperienceDto[]>();
  readonly changed = output<ProfileDto>();

  private readonly profileService = inject(ProfileService);
  private readonly modal = viewChild.required<WorkExperienceModalComponent>('modal');

  protected readonly editingItem = signal<WorkExperienceDto | null>(null);
  protected readonly deletingId = signal<string | null>(null);

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modal().show();
  }

  protected onEdit(item: WorkExperienceDto): void {
    this.editingItem.set(item);
    this.modal().show();
  }

  protected onModalDismissed(): void {
    this.editingItem.set(null);
  }

  protected async onSaved(data: WorkExperienceFormData): Promise<void> {
    const id = this.editingItem()?.id;
    const payload = {
      company: data.company,
      title: data.title,
      startDate: data.startDate,
      endDate: data.endDate || null,
      description: data.description || null,
    };
    if (id) {
      await firstValueFrom(this.profileService.updateWorkExperience(id, payload));
    } else {
      await firstValueFrom(this.profileService.addWorkExperience(payload));
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
      await firstValueFrom(this.profileService.deleteWorkExperience(id));
      const profile = await firstValueFrom(this.profileService.getProfile());
      this.changed.emit(profile);
    } finally {
      this.deletingId.set(null);
    }
  }
}
