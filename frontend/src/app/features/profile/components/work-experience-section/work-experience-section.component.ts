import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileService, WorkExperienceDto } from '../../../../generated/api';
import { WorkExperienceModalComponent, WorkExperienceFormData } from '../work-experience-modal/work-experience-modal.component';

@Component({
  selector: 'app-work-experience-section',
  standalone: true,
  imports: [CommonModule, WorkExperienceModalComponent],
  templateUrl: './work-experience-section.component.html',
})
export class WorkExperienceSectionComponent {
  readonly items = input.required<WorkExperienceDto[]>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  protected readonly modalOpen = signal(false);
  protected readonly editingItem = signal<WorkExperienceDto | null>(null);
  protected readonly deletingId = signal<string | null>(null);

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modalOpen.set(true);
  }

  protected onEdit(item: WorkExperienceDto): void {
    this.editingItem.set(item);
    this.modalOpen.set(true);
  }

  protected onModalDismissed(): void {
    this.modalOpen.set(false);
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
    this.modalOpen.set(false);
    this.editingItem.set(null);
    this.changed.emit();
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
      this.changed.emit();
    } finally {
      this.deletingId.set(null);
    }
  }
}
