import {
  Component,
  ElementRef,
  input,
  linkedSignal,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, maxLength } from '@angular/forms/signals';

@Component({
  selector: 'app-profile-skills-modal',
  standalone: true,
  imports: [CommonModule, FormField],
  templateUrl: './profile-skills-modal.component.html',
})
export class ProfileSkillsModalComponent {
  readonly items = input.required<string[]>();
  readonly saved = output<string[]>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('modal');

  protected readonly editingSkills = linkedSignal<string[]>(() => [...this.items()]);

  protected readonly newSkillModel = signal({ newSkill: '' });
  protected readonly newSkillFields = form(this.newSkillModel, (f) => {
    maxLength(f.newSkill, 100, { message: 'Skill must be 100 characters or fewer.' });
  });

  show(): void {
    this.dialogEl().nativeElement.showModal();
  }

  protected onAddSkill(event: Event): void {
    event.preventDefault();
    this.commitPendingSkill();
  }

  protected removeSkill(skill: string): void {
    this.editingSkills.update((skills) => skills.filter((s) => s !== skill));
  }

  protected onSubmit(): void {
    this.commitPendingSkill();
    this.saved.emit(this.editingSkills());
    this.dialogEl().nativeElement.close();
  }

  protected onClose(): void {
    this.newSkillModel.set({ newSkill: '' });
    this.dismissed.emit();
  }

  private commitPendingSkill(): void {
    const value = this.newSkillModel().newSkill.trim().replace(/,$/, '');
    if (value && !this.editingSkills().includes(value)) {
      this.editingSkills.update((skills) => [...skills, value]);
    }
    this.newSkillModel.set({ newSkill: '' });
  }
}
