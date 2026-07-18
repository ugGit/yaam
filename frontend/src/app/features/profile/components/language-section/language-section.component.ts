import { Component, inject, input, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileViewModel, ProfileService, LanguageViewModel } from '../../../../generated/api';
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
  readonly items = input.required<LanguageViewModel[]>();
  readonly changed = output<ProfileViewModel>();

  private readonly profileService = inject(ProfileService);
  private readonly modal = viewChild.required<LanguageModalComponent>('modal');

  protected readonly editingItem = signal<LanguageViewModel | null>(null);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly proficiencyLabels = LANGUAGE_PROFICIENCY_LABELS;

  protected onAdd(): void {
    this.editingItem.set(null);
    this.modal().show();
  }

  protected onEdit(item: LanguageViewModel): void {
    this.editingItem.set(item);
    this.modal().show();
  }

  protected onModalDismissed(): void {
    this.editingItem.set(null);
  }

  protected async onSaved(data: LanguageFormData): Promise<void> {
    const id = this.editingItem()?.id;
    const payload = {
      name: data.name,
      proficiency: data.proficiency as LanguageViewModel['proficiency'],
    };
    if (id) {
      await firstValueFrom(this.profileService.updateLanguage(id, payload));
    } else {
      await firstValueFrom(this.profileService.addLanguage(payload));
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
      await firstValueFrom(this.profileService.deleteLanguage(id));
      const profile = await firstValueFrom(this.profileService.getProfile());
      this.changed.emit(profile);
    } finally {
      this.deletingId.set(null);
    }
  }
}
