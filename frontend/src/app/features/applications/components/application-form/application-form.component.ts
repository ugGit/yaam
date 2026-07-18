import { Component, input, linkedSignal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, maxLength, required, submit } from '@angular/forms/signals';
import { Application, ALL_STATUSES, STATUS_LABELS } from '../../models/application.model';

export interface ApplicationFormData {
  companyName: string;
  role: string;
  dateApplied: string;
  status: string;
  contactName: string;
  contactEmail: string;
  contactPhone: string;
  jobPosting: string;
}

@Component({
  selector: 'app-application-form',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './application-form.component.html',
})
export class ApplicationFormComponent {
  readonly existing = input<Application | null>(null);
  readonly save = output<ApplicationFormData>();
  readonly cancelled = output<void>();

  protected readonly ALL_STATUSES = ALL_STATUSES;
  protected readonly STATUS_LABELS = STATUS_LABELS;

  protected readonly formModel = linkedSignal<ApplicationFormData>(() => ({
    companyName: this.existing()?.companyName ?? '',
    role: this.existing()?.role ?? '',
    dateApplied: this.existing()?.dateApplied ?? '',
    status: this.existing()?.status ?? 'Draft',
    contactName: this.existing()?.contactName ?? '',
    contactEmail: this.existing()?.contactEmail ?? '',
    contactPhone: this.existing()?.contactPhone ?? '',
    jobPosting: this.existing()?.jobPosting ?? '',
  }));

  protected readonly fields = form(this.formModel, (fields) => {
    required(fields.companyName, { message: 'Company name is required.' });
    maxLength(fields.companyName, 250, {
      message: 'Company name must be 250 characters or fewer.',
    });
    required(fields.role, { message: 'Role is required.' });
    maxLength(fields.role, 250, { message: 'Role must be 250 characters or fewer.' });
    maxLength(fields.contactName, 250, {
      message: 'Contact name must be 250 characters or fewer.',
    });
    maxLength(fields.contactEmail, 250, {
      message: 'Contact email must be 250 characters or fewer.',
    });
    maxLength(fields.contactPhone, 50, {
      message: 'Contact phone must be 50 characters or fewer.',
    });
    maxLength(fields.jobPosting, 2000, {
      message: 'Job posting URL must be 2000 characters or fewer.',
    });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.save.emit(this.formModel());
    });
  }

  protected cancel(): void {
    this.cancelled.emit();
  }
}
