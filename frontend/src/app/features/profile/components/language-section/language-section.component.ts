import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileService, LanguageDto } from '../../../../generated/api';
import { LanguageModalComponent, LanguageFormData, LANGUAGE_PROFICIENCY_LABELS } from '../language-modal/language-modal.component';

@Component({
  selector: 'app-language-section',
  standalone: true,
  imports: [CommonModule, LanguageModalComponent],
  templateUrl: './language-section.component.html',
})
export class LanguageSectionComponent {
  readonly items = input.required<LanguageDto[]>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  protected readonly modalOpen = signal(false);
  protected readonly editingItem = signal<LanguageDto | null>(null);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly saving = signal(false);
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
    this.saving.set(true);
    try {
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
      await firstValueFrom(this.profileService.deleteLanguage(id));
      this.changed.emit();
    } finally {
      this.deletingId.set(null);
    }
  }
}
