import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkExperienceDto } from '../../../../generated/api';

@Component({
  selector: 'app-work-experience-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './work-experience-section.component.html',
})
export class WorkExperienceSectionComponent {
  readonly items = input.required<WorkExperienceDto[]>();
}
