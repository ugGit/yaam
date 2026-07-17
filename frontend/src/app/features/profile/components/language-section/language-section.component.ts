import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageDto, LanguageProficiency } from '../../../../generated/api';

const PROFICIENCY_LABELS: Record<LanguageProficiency, string> = {
  [LanguageProficiency.Basic]: 'Basic',
  [LanguageProficiency.BusinessProficiency]: 'Business Proficiency',
  [LanguageProficiency.Fluent]: 'Fluent',
  [LanguageProficiency.Native]: 'Native',
};

@Component({
  selector: 'app-language-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './language-section.component.html',
})
export class LanguageSectionComponent {
  readonly items = input.required<LanguageDto[]>();
  protected readonly proficiencyLabels = PROFICIENCY_LABELS;
}
