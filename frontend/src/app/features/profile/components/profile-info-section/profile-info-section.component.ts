import { Component, input, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProfileDto } from '../../../../generated/api';
import { ProfileInfoModalComponent } from '../profile-info-modal/profile-info-modal.component';

@Component({
  selector: 'app-profile-info-section',
  standalone: true,
  imports: [CommonModule, ProfileInfoModalComponent],
  templateUrl: './profile-info-section.component.html',
})
export class ProfileInfoSectionComponent {
  readonly profile = input.required<ProfileDto>();
  readonly changed = output<ProfileDto>();

  private readonly modal = viewChild.required<ProfileInfoModalComponent>('modal');

  protected onEdit(): void {
    this.modal().show();
  }

  protected onSaved(profile: ProfileDto): void {
    this.changed.emit(profile);
  }
}
