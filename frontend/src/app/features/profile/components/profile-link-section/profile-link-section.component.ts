import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProfileLinkDto } from '../../../../generated/api';

@Component({
  selector: 'app-profile-link-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-link-section.component.html',
})
export class ProfileLinkSectionComponent {
  readonly items = input.required<ProfileLinkDto[]>();
}
