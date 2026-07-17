import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EducationDto } from '../../../../generated/api';

@Component({
  selector: 'app-education-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './education-section.component.html',
})
export class EducationSectionComponent {
  readonly items = input.required<EducationDto[]>();

  protected degreeAndField(item: EducationDto): string {
    return [item.degree, item.fieldOfStudy].filter((v): v is string => !!v).join(', ');
  }
}
