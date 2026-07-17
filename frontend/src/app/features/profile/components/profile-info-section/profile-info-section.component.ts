import { Component, input, output, signal } from '@angular/core';
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

  protected readonly modalOpen = signal(false);

  protected onEdit(): void {
    this.modalOpen.set(true);
  }

  protected onModalDismissed(): void {
    this.modalOpen.set(false);
  }

  protected onSaved(profile: ProfileDto): void {
    this.modalOpen.set(false);
    this.changed.emit(profile);
  }
}
