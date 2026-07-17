import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProfileDto } from '../../../../generated/api';

@Component({
  selector: 'app-profile-info-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-info-section.component.html',
})
export class ProfileInfoSectionComponent {
  readonly profile = input.required<ProfileDto>();
}
