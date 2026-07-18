import { Component, ElementRef, input, linkedSignal, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, form, required, submit } from '@angular/forms/signals';
import { ProfileLinkDto } from '../../../../generated/api';

export interface ProfileLinkFormData {
  label: string;
  url: string;
}

@Component({
  selector: 'app-profile-link-modal',
  standalone: true,
  imports: [CommonModule, FormField],
  templateUrl: './profile-link-modal.component.html',
})
export class ProfileLinkModalComponent {
  readonly item = input<ProfileLinkDto | null>(null);
  readonly saved = output<ProfileLinkFormData>();
  readonly dismissed = output<void>();

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('modal');

  protected readonly formModel = linkedSignal<ProfileLinkFormData>(() => ({
    label: this.item()?.label ?? '',
    url: this.item()?.url ?? '',
  }));

  protected readonly fields = form(this.formModel, (f) => {
    required(f.label, { message: 'Label is required.' });
    required(f.url, { message: 'URL is required.' });
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
