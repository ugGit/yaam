import {
  Component,
  ElementRef,
  effect,
  input,
  linkedSignal,
  output,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, required, submit } from '@angular/forms/signals';
import { CertificationDto } from '../../../../generated/api';

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
  readonly open = input.required<boolean>();
  readonly item = input<CertificationDto | null>(null);
  readonly saved = output<CertificationFormData>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild<ElementRef<HTMLDialogElement>>('modal');

  protected readonly formModel = linkedSignal<CertificationFormData>(() => ({
    name: this.item()?.name ?? '',
    issuer: this.item()?.issuer ?? '',
    date: this.item()?.date ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.name, { message: 'Certification name is required.' });
    required(f.date, { message: 'Date is required.' });
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
