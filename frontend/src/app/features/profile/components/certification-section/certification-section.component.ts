import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CertificationDto } from '../../../../generated/api';

@Component({
  selector: 'app-certification-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './certification-section.component.html',
})
export class CertificationSectionComponent {
  readonly items = input.required<CertificationDto[]>();
}
