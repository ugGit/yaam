import {
  Component,
  ElementRef,
  effect,
  input,
  linkedSignal,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form } from '@angular/forms/signals';

@Component({
  selector: 'app-profile-skills-modal',
  standalone: true,
  imports: [CommonModule, FormField],
  templateUrl: './profile-skills-modal.component.html',
})
export class ProfileSkillsModalComponent {
  readonly open = input.required<boolean>();
  readonly items = input.required<string[]>();
  readonly saved = output<string[]>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild<ElementRef<HTMLDialogElement>>('modal');

  protected readonly editingSkills = linkedSignal<string[]>(() => [...this.items()]);

  protected readonly newSkillModel = signal({ newSkill: '' });
  protected readonly newSkillFields = form(this.newSkillModel);

  constructor() {
    effect(() => {
      const dialogEl = this.dialogEl()?.nativeElement;
      if (!dialogEl) return;
      if (this.open()) {
        dialogEl.showModal();
      } else {
        dialogEl.close();
      }
    });
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
