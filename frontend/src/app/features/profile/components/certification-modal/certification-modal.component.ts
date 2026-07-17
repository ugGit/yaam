import { Component, input, linkedSignal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
import { CertificationDto } from '../../../../generated/api';

export interface CertificationFormData {
  name: string;
  issuer: string;
  date: string;
}

@Component({
  selector: 'app-certification-modal',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './certification-modal.component.html',
})
export class CertificationModalComponent {
  readonly open = input.required<boolean>();
  readonly item = input<CertificationDto | null>(null);
  readonly saved = output<CertificationFormData>();
  readonly dismissed = output<void>();

  protected readonly formModel = linkedSignal<CertificationFormData>(() => ({
    name: this.item()?.name ?? '',
    issuer: this.item()?.issuer ?? '',
    date: this.item()?.date ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.name, { message: 'Certification name is required.' });
    required(f.date, { message: 'Date is required.' });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.saved.emit(this.formModel());
    });
  }

  protected onDismiss(): void {
    this.dismissed.emit();
  }

  protected get isEditMode(): boolean {
    return this.item() !== null;
  }
}
