import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CustomField } from '../../models/profile.model';

@Component({
  selector: 'app-custom-fields-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-fields-section.component.html',
})
export class CustomFieldsSectionComponent {
  readonly fields = input.required<CustomField[]>();
}
