import { Component, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileDto, ProfileService } from '../../../../generated/api';
import { ProfileSkillsModalComponent } from '../profile-skills-modal/profile-skills-modal.component';

@Component({
  selector: 'app-profile-skills-section',
  standalone: true,
  imports: [CommonModule, ProfileSkillsModalComponent],
  templateUrl: './profile-skills-section.component.html',
})
export class ProfileSkillsSectionComponent {
  readonly skills = input.required<string[]>();
  readonly changed = output<ProfileDto>();

  private readonly profileService = inject(ProfileService);

  protected readonly modalOpen = signal(false);

  protected onEdit(): void {
    this.modalOpen.set(true);
  }

  protected onModalDismissed(): void {
    this.modalOpen.set(false);
  }

  protected async onSaved(skills: string[]): Promise<void> {
    await firstValueFrom(this.profileService.updateProfileSkills({ skills }));
    this.modalOpen.set(false);
    const profile = await firstValueFrom(this.profileService.getProfile());
    this.changed.emit(profile);
  }
}
