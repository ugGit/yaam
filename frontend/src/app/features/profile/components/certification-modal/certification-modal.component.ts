import { Component, ElementRef, input, linkedSignal, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, maxLength, required, submit } from '@angular/forms/signals';
import { CertificationViewModel } from '../../../../generated/api';

export interface CertificationFormData {
  name: string;
  issuer: string;
  date: string;
}

@Component({
  selector: 'app-certification-modal',
  standalone: true,
  imports: [CommonModule, FormField],
  templateUrl: './certification-modal.component.html',
})
export class CertificationModalComponent {
  readonly item = input<CertificationViewModel | null>(null);
  readonly saved = output<CertificationFormData>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('modal');

  protected readonly formModel = linkedSignal<CertificationFormData>(() => ({
    name: this.item()?.name ?? '',
    issuer: this.item()?.issuer ?? '',
    date: this.item()?.date ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.name, { message: 'Certification name is required.' });
    maxLength(f.name, 250, { message: 'Certification name must be 250 characters or fewer.' });
    required(f.date, { message: 'Date is required.' });
    maxLength(f.issuer, 250, { message: 'Issuer must be 250 characters or fewer.' });
  });

  show(): void {
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
