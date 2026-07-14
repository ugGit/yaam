import { Component, inject, input, output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Application, ALL_STATUSES, STATUS_LABELS } from '../../models/application.model';

@Component({
  selector: 'app-application-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './application-form.component.html',
})
export class ApplicationFormComponent implements OnInit {
  readonly existing = input<Application | null>(null);
  readonly saved = output<FormGroup>();
  readonly cancelled = output<void>();

  protected readonly ALL_STATUSES = ALL_STATUSES;
  protected readonly STATUS_LABELS = STATUS_LABELS;

  protected form!: FormGroup;

  private readonly fb = inject(FormBuilder);

  ngOnInit(): void {
    const a = this.existing();
    this.form = this.fb.group({
      companyName: [a?.companyName ?? '', Validators.required],
      role: [a?.role ?? '', Validators.required],
      dateApplied: [a?.dateApplied ?? null],
      status: [a?.status ?? 'Draft', Validators.required],
      contactName: [a?.contactName ?? ''],
      contactEmail: [a?.contactEmail ?? ''],
      contactPhone: [a?.contactPhone ?? ''],
      jobPosting: [a?.jobPosting ?? ''],
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saved.emit(this.form);
  }

  protected cancel(): void {
    this.cancelled.emit();
  }

  protected fieldError(field: string): string | null {
    const control = this.form.get(field);
    if (!control?.touched || !control.errors) return null;
    if (control.errors['required']) return `${field} is required.`;
    return null;
  }
}
