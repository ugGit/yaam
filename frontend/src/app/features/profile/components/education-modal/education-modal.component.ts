import { Component, ElementRef, input, linkedSignal, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, maxLength, required, submit } from '@angular/forms/signals';
import { EducationViewModel } from '../../../../generated/api';

export interface EducationFormData {
  institution: string;
  degree: string;
  fieldOfStudy: string;
  startDate: string;
  endDate: string;
}

@Component({
  selector: 'app-education-modal',
  standalone: true,
  imports: [CommonModule, FormField],
  templateUrl: './education-modal.component.html',
})
export class EducationModalComponent {
  readonly item = input<EducationViewModel | null>(null);
  readonly saved = output<EducationFormData>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('modal');

  protected readonly formModel = linkedSignal<EducationFormData>(() => ({
    institution: this.item()?.institution ?? '',
    degree: this.item()?.degree ?? '',
    fieldOfStudy: this.item()?.fieldOfStudy ?? '',
    startDate: this.item()?.startDate ?? '',
    endDate: this.item()?.endDate ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.institution, { message: 'Institution is required.' });
    maxLength(f.institution, 250, { message: 'Institution must be 250 characters or fewer.' });
    maxLength(f.degree, 250, { message: 'Degree must be 250 characters or fewer.' });
    maxLength(f.fieldOfStudy, 250, { message: 'Field of study must be 250 characters or fewer.' });
  });

  show(): void {
    this.formModel.set({
      institution: this.item()?.institution ?? '',
      degree: this.item()?.degree ?? '',
      fieldOfStudy: this.item()?.fieldOfStudy ?? '',
      startDate: this.item()?.startDate ?? '',
      endDate: this.item()?.endDate ?? '',
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
