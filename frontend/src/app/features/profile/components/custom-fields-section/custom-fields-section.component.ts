import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CustomFieldViewModel } from '../../../../generated/api';

@Component({
  selector: 'app-custom-fields-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-fields-section.component.html',
})
export class CustomFieldsSectionComponent {
  readonly fields = input.required<CustomFieldViewModel[]>();
}
