import { Component, input, linkedSignal, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, required, submit } from '@angular/forms/signals';
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
  readonly saved = output<ApplicationFormData>();
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
    required(fields.role, { message: 'Role is required.' });
  });

  protected async onSubmit(): Promise<void> {
    await submit(this.fields, async () => {
      this.saved.emit(this.formModel());
    });
  }

  protected cancel(): void {
    this.cancelled.emit();
  }
}
