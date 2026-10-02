import { Component, computed, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, min, required, validate } from '@angular/forms/signals';
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
  readonly reminders = input<ApplicationReminderViewModel[]>([]);

  readonly reminderChanged = output<void>();

  private readonly api = inject(ApplicationReminderService);

  protected readonly showAddForm = signal(false);
  protected readonly reschedulingReminderId = signal<string | null>(null);
  protected readonly deletingReminderId = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected readonly addModel = signal<{
    presetDays: number;
    customDays: number | null;
    note: string;
  }>({ presetDays: 7, customDays: null, note: '' });
  protected readonly addFields = form(this.addModel, (fields) => {
    min(fields.customDays, 1, { message: 'Custom delay must be at least 1 day.' });
    validate(fields.customDays, (ctx) => {
      const value = ctx.value();
      return value !== null && !Number.isInteger(value)
        ? { kind: 'integer', message: 'Custom delay must be a whole number of days.' }
        : null;
    });
  });
  protected readonly delayDays = computed(
    () => this.addModel().customDays ?? this.addModel().presetDays,
  );

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
      return selected < tomorrow
        ? { kind: 'minDate', message: 'Due date must be at least tomorrow.' }
        : null;
    });
  });

  protected readonly presetDays = [7, 14, 30] as const;

  protected readonly tomorrow = computed(() => {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  });

  protected dueDateDisplay(reminder: ApplicationReminderViewModel): {
    label: string;
    cls: string;
  } {
    const due = parseDateOnly(reminder.dueDate as unknown as string);
    const todayDate = new Date();
    todayDate.setHours(0, 0, 0, 0);
    const diff = Math.round((due.getTime() - todayDate.getTime()) / 86400000);
    if (diff < 0)
      return { label: `Overdue by ${-diff} day${-diff === 1 ? '' : 's'}`, cls: 'badge-error' };
    if (diff === 0) return { label: 'Due today', cls: 'badge-warning' };
    return { label: `Due in ${diff} day${diff === 1 ? '' : 's'}`, cls: 'badge-info' };
  }

  protected selectPreset(days: number): void {
    this.addModel.update((m) => ({ ...m, presetDays: days, customDays: null }));
  }

  protected previewDate(): string {
    if (this.addFields.customDays().errors().length) return '';
    const days = this.delayDays();
    const d = new Date();
    d.setDate(d.getDate() + days);
    return d.toLocaleDateString('en-CH', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  protected async setReminder(): Promise<void> {
    if (this.addFields.customDays().errors().length) return;
    const delayDays = this.delayDays();
    const { note } = this.addModel();
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.setReminder(this.applicationId(), { delayDays, note: note || null }),
      );
      this.showAddForm.set(false);
      this.addModel.set({ presetDays: 7, customDays: null, note: '' });
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected async completeReminder(reminderId: string): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.completeReminder(this.applicationId(), reminderId));
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected startReschedule(reminderId: string): void {
    this.reschedulingReminderId.set(reminderId);
    this.rescheduleModel.set({ newDueDate: '' });
  }

  protected cancelReschedule(): void {
    this.reschedulingReminderId.set(null);
    this.rescheduleModel.set({ newDueDate: '' });
  }

  protected async rescheduleReminder(reminderId: string): Promise<void> {
    const { newDueDate } = this.rescheduleModel();
    if (!newDueDate) return;
    this.saving.set(true);
    try {
      await firstValueFrom(
        this.api.rescheduleReminder(this.applicationId(), reminderId, { newDueDate }),
      );
      this.reschedulingReminderId.set(null);
      this.rescheduleModel.set({ newDueDate: '' });
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }

  protected async deleteReminder(reminderId: string): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.api.deleteReminder(this.applicationId(), reminderId));
      this.deletingReminderId.set(null);
      this.reminderChanged.emit();
    } finally {
      this.saving.set(false);
    }
  }
}
