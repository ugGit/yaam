import { Component, ElementRef, effect, input, linkedSignal, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, required, submit } from '@angular/forms/signals';
import { WorkExperienceDto } from '../../../../generated/api';

export interface WorkExperienceFormData {
  company: string;
  title: string;
  startDate: string;
  endDate: string;
  description: string;
}

@Component({
  selector: 'app-work-experience-modal',
  standalone: true,
  imports: [CommonModule, FormField],
  templateUrl: './work-experience-modal.component.html',
})
export class WorkExperienceModalComponent {
  readonly open = input.required<boolean>();
  readonly item = input<WorkExperienceDto | null>(null);
  readonly saved = output<WorkExperienceFormData>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild<ElementRef<HTMLDialogElement>>('modal');

  protected readonly formModel = linkedSignal<WorkExperienceFormData>(() => ({
    company: this.item()?.company ?? '',
    title: this.item()?.title ?? '',
    startDate: this.item()?.startDate ?? '',
    endDate: this.item()?.endDate ?? '',
    description: this.item()?.description ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.company, { message: 'Company is required.' });
    required(f.title, { message: 'Title is required.' });
    required(f.startDate, { message: 'Start date is required.' });
  });

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

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.saved.emit(this.formModel());
    });
  }

  protected onClose(): void {
    this.dismissed.emit();
  }

  protected get isEditMode(): boolean {
    return this.item() !== null;
  }
}
