import { Component, inject, input, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileViewModel, ProfileService } from '../../../../generated/api';
import { ProfileSkillsModalComponent } from '../profile-skills-modal/profile-skills-modal.component';

@Component({
  selector: 'app-profile-skills-section',
  standalone: true,
  imports: [CommonModule, ProfileSkillsModalComponent],
  templateUrl: './profile-skills-section.component.html',
})
export class ProfileSkillsSectionComponent {
  readonly skills = input.required<string[]>();
  readonly changed = output<ProfileViewModel>();

  private readonly profileService = inject(ProfileService);
  private readonly modal = viewChild.required<ProfileSkillsModalComponent>('modal');

  protected onEdit(): void {
    this.modal().show();
  }

  protected async onSaved(skills: string[]): Promise<void> {
    await firstValueFrom(this.profileService.updateProfileSkills({ skills }));
    const profile = await firstValueFrom(this.profileService.getProfile());
    this.changed.emit(profile);
  }
}
