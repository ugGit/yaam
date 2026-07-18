import { Component, ElementRef, input, linkedSignal, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, maxLength, required, submit } from '@angular/forms/signals';
import { CustomFieldViewModel } from '../../../../generated/api';

export interface CustomFieldFormData {
  label: string;
  value: string;
}

@Component({
  selector: 'app-custom-field-modal',
  standalone: true,
  imports: [CommonModule, FormField],
  templateUrl: './custom-field-modal.component.html',
})
export class CustomFieldModalComponent {
  readonly item = input<CustomFieldViewModel | null>(null);
  readonly saved = output<CustomFieldFormData>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('modal');

  protected readonly formModel = linkedSignal<CustomFieldFormData>(() => ({
    label: this.item()?.label ?? '',
    value: this.item()?.value ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.label, { message: 'Label is required.' });
    maxLength(f.label, 250, { message: 'Label must be 250 characters or fewer.' });
    required(f.value, { message: 'Value is required.' });
    maxLength(f.value, 1000, { message: 'Value must be 1000 characters or fewer.' });
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
