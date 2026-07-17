import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileDto, ProfileService, LanguageDto } from '../../../../generated/api';
import {
  LanguageModalComponent,
  LanguageFormData,
  LANGUAGE_PROFICIENCY_LABELS,
} from '../language-modal/language-modal.component';

@Component({
  selector: 'app-language-section',
  standalone: true,
  imports: [CommonModule, LanguageModalComponent],
  templateUrl: './language-section.component.html',
})
export class LanguageSectionComponent {
  readonly items = input.required<LanguageDto[]>();
  readonly changed = output<ProfileDto>();

  private readonly profileService = inject(ProfileService);

  protected readonly modalOpen = signal(false);
  protected readonly editingItem = signal<LanguageDto | null>(null);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly proficiencyLabels = LANGUAGE_PROFICIENCY_LABELS;

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modalOpen.set(true);
  }

  protected onEdit(item: LanguageDto): void {
    this.editingItem.set(item);
    this.modalOpen.set(true);
  }

  protected onModalDismissed(): void {
    this.modalOpen.set(false);
    this.editingItem.set(null);
  }

  protected async onSaved(data: LanguageFormData): Promise<void> {
    const id = this.editingItem()?.id;
    const payload = {
      name: data.name,
      proficiency: data.proficiency as LanguageDto['proficiency'],
    };
    if (id) {
      await firstValueFrom(this.profileService.updateLanguage(id, payload));
    } else {
      await firstValueFrom(this.profileService.addLanguage(payload));
    }
    this.modalOpen.set(false);
    this.editingItem.set(null);
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
      await firstValueFrom(this.profileService.deleteLanguage(id));
      const profile = await firstValueFrom(this.profileService.getProfile());
      this.changed.emit(profile);
    } finally {
      this.deletingId.set(null);
    }
  }
}
