import { Component, computed, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, required, validate } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationReminderService,
  ApplicationReminderViewModel,
} from '../../../../generated/api/index';
import { parseDateOnly } from '../../../../shared/date.util';

@Component({
  selector: 'app-reminder-section',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './reminder-section.component.html',
})
export class ReminderSectionComponent {
  readonly applicationId = input.required<string>();
  readonly reminder = input<ApplicationReminderViewModel | null | undefined>();

  readonly reminderChanged = output<void>();

  private readonly api = inject(ApplicationReminderService);

  protected readonly showAddForm = signal(false);
  protected readonly showRescheduleForm = signal(false);
  protected readonly deleteConfirm = signal(false);
  protected readonly saving = signal(false);

  protected readonly addModel = signal({ delayDays: 7, customDays: '', note: '' });

  protected readonly rescheduleModel = signal({ newDueDate: '' });
  protected readonly rescheduleFields = form(this.rescheduleModel, (fields) => {
    required(fields.newDueDate, { message: 'New due date is required.' });
    validate(fields.newDueDate, (ctx) => {
      const value = ctx.value() as string;
      if (!value) return null;
      const [y, m, d] = value.split('-').map(Number);
      const selected = new Date(y, m - 1, d);
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      tomorrow.setHours(0, 0, 0, 0);
      return selected < tomorrow ? { message: 'Due date must be at least tomorrow.' } : null;
    });
  });

  protected readonly presetDays = [7, 14, 30] as const;

  protected readonly today = computed(() => new Date().toISOString().split('T')[0]);
  protected readonly tomorrow = computed(() => {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  });

  protected readonly dueDateDisplay = computed(() => {
    const r = this.reminder();
    if (!r) return null;
    const due = parseDateOnly(r.dueDate as unknown as string);
    const todayDate = new Date();
    todayDate.setHours(0, 0, 0, 0);
    const diff = Math.round((due.getTime() - todayDate.getTime()) / 86400000);
    if (diff < 0)
      return { label: `Overdue by ${-diff} day${-diff === 1 ? '' : 's'}`, cls: 'badge-error' };
    if (diff === 0) return { label: 'Due today', cls: 'badge-warning' };
    return { label: `Due in ${diff} day${diff === 1 ? '' : 's'}`, cls: 'badge-info' };
  });

  protected selectPreset(days: number): void {
    this.addModel.update((m) => ({ ...m, delayDays: days, customDays: '' }));
  }

  protected onCustomDaysInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    const parsed = parseInt(value, 10);
    if (!isNaN(parsed) && parsed > 0) {
      this.addModel.update((m) => ({ ...m, delayDays: parsed, customDays: value }));
    } else {
      this.addModel.update((m) => ({ ...m, customDays: value }));
    }
  }

  protected previewDate(): string {
    const days = this.addModel().delayDays;
    if (!days || days < 1) return '';
    const d = new Date();
    d.setDate(d.getDate() + days);
    return d.toLocaleDateString('en-CH', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  protected async setReminder(): Promise<void> {
    const { delayDays, note } = this.addModel();
    if (!delayDays || delayDays < 1) return;
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.setReminder(this.applicationId(), { delayDays, note: note || null }),
      );
      this.showAddForm.set(false);
      this.addModel.set({ delayDays: 7, customDays: '', note: '' });
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected async completeReminder(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.completeReminder(this.applicationId()));
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected async rescheduleReminder(): Promise<void> {
    const { newDueDate } = this.rescheduleModel();
    if (!newDueDate) return;
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.rescheduleReminder(this.applicationId(), { newDueDate }));
      this.showRescheduleForm.set(false);
      this.rescheduleModel.set({ newDueDate: '' });
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected async deleteReminder(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.deleteReminder(this.applicationId()));
      this.deleteConfirm.set(false);
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }
}
