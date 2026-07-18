import { Component, ElementRef, input, linkedSignal, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, maxLength, required, submit } from '@angular/forms/signals';
import { WorkExperienceViewModel } from '../../../../generated/api';

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
  readonly item = input<WorkExperienceViewModel | null>(null);
  readonly saved = output<WorkExperienceFormData>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('modal');

  protected readonly formModel = linkedSignal<WorkExperienceFormData>(() => ({
    company: this.item()?.company ?? '',
    title: this.item()?.title ?? '',
    startDate: this.item()?.startDate ?? '',
    endDate: this.item()?.endDate ?? '',
    description: this.item()?.description ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.company, { message: 'Company is required.' });
    maxLength(f.company, 250, { message: 'Company must be 250 characters or fewer.' });
    required(f.title, { message: 'Title is required.' });
    maxLength(f.title, 250, { message: 'Title must be 250 characters or fewer.' });
    required(f.startDate, { message: 'Start date is required.' });
    maxLength(f.description, 1000, { message: 'Description must be 1000 characters or fewer.' });
  });

  show(): void {
    this.formModel.set({
      company: this.item()?.company ?? '',
      title: this.item()?.title ?? '',
      startDate: this.item()?.startDate ?? '',
      endDate: this.item()?.endDate ?? '',
      description: this.item()?.description ?? '',
    });
    this.fields().reset();
    this.dialogEl().nativeElement.showModal();
  }

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.saved.emit(this.formModel());
      this.dialogEl().nativeElement.close();
    });
  }

  protected onClose(): void {
    this.dismissed.emit();
  }

  protected get isEditMode(): boolean {
    return this.item() !== null;
  }
}
