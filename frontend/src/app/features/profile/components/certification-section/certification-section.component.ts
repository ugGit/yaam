import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Certification } from '../../models/profile.model';

@Component({
  selector: 'app-certification-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './certification-section.component.html',
})
export class CertificationSectionComponent {
  readonly items = input.required<Certification[]>();
}
