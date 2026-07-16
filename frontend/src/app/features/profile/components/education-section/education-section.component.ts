import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Education } from '../../models/profile.model';

@Component({
  selector: 'app-education-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './education-section.component.html',
})
export class EducationSectionComponent {
  readonly items = input.required<Education[]>();

  protected degreeAndField(item: Education): string {
    return [item.degree, item.fieldOfStudy].filter((v): v is string => !!v).join(', ');
  }
}
