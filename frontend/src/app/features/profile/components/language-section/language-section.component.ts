import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Language, LANGUAGE_PROFICIENCY_LABELS } from '../../models/profile.model';

@Component({
  selector: 'app-language-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './language-section.component.html',
})
export class LanguageSectionComponent {
  readonly items = input.required<Language[]>();
  protected readonly proficiencyLabels = LANGUAGE_PROFICIENCY_LABELS;
}
