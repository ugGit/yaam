import { Component, inject, input, linkedSignal, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ProfileService } from '../../../../generated/api';

@Component({
  selector: 'app-profile-skills-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-skills-section.component.html',
})
export class ProfileSkillsSectionComponent {
  readonly skills = input.required<string[]>();
  readonly changed = output<void>();

  private readonly profileService = inject(ProfileService);

  protected readonly editMode = signal(false);
  protected readonly saving = signal(false);
  protected readonly skillInput = signal('');
  protected readonly editingSkills = linkedSignal(() => [...this.skills()]);

  protected onEdit(): void {
    this.editMode.set(true);
  }

  protected onCancel(): void {
    this.editMode.set(false);
  }

  protected addSkill(event: Event): void {
    event.preventDefault();
    const value = this.skillInput().trim().replace(/,$/, '');
    if (value && !this.editingSkills().includes(value)) {
      this.editingSkills.update(s => [...s, value]);
    }
    this.skillInput.set('');
  }

  protected removeSkill(skill: string): void {
    this.editingSkills.update(s => s.filter(x => x !== skill));
  }

  protected async onSave(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.profileService.updateProfileSkills({ skills: this.editingSkills() }),
      );
      this.editMode.set(false);
      this.changed.emit();
    } finally {
      this.saving.set(false);
    }
  }
}
