import { Component, ElementRef, effect, input, linkedSignal, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, required, submit } from '@angular/forms/signals';
import { LanguageDto, LanguageProficiency } from '../../../../generated/api';

export interface LanguageFormData {
  name: string;
  proficiency: string;
}

export const ALL_LANGUAGE_PROFICIENCIES: LanguageProficiency[] = [
  LanguageProficiency.Basic,
  LanguageProficiency.BusinessProficiency,
  LanguageProficiency.Fluent,
  LanguageProficiency.Native,
];

export const LANGUAGE_PROFICIENCY_LABELS: Record<LanguageProficiency, string> = {
  [LanguageProficiency.Basic]: 'Basic',
  [LanguageProficiency.BusinessProficiency]: 'Business Proficiency',
  [LanguageProficiency.Fluent]: 'Fluent',
  [LanguageProficiency.Native]: 'Native',
};

@Component({
  selector: 'app-language-modal',
  standalone: true,
  imports: [CommonModule, FormField],
  templateUrl: './language-modal.component.html',
})
export class LanguageModalComponent {
  readonly open = input.required<boolean>();
  readonly item = input<LanguageDto | null>(null);
  readonly saved = output<LanguageFormData>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild<ElementRef<HTMLDialogElement>>('modal');

  protected readonly formModel = linkedSignal<LanguageFormData>(() => ({
    name: this.item()?.name ?? '',
    proficiency: this.item()?.proficiency ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.name, { message: 'Language name is required.' });
    required(f.proficiency, { message: 'Proficiency is required.' });
  });

  protected readonly allProficiencies = ALL_LANGUAGE_PROFICIENCIES;
  protected readonly proficiencyLabels = LANGUAGE_PROFICIENCY_LABELS;

  constructor() {
    effect(() => {
      const dialogEl = this.dialogEl()?.nativeElement;
      if (!dialogEl) return;
      if (this.open()) {
        dialogEl.showModal();
      } else {
        dialogEl.close();
      }
    });
  }

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.saved.emit(this.formModel());
    });
  }

  protected onClose(): void {
    this.dismissed.emit();
  }

  protected get isEditMode(): boolean {
    return this.item() !== null;
  }
}
