import { Component, computed, inject, resource, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationReminderService,
  RemindersService,
  ReminderViewModel,
} from '../../../../generated/api/index';
import { parseDateOnly } from '../../../../shared/date.util';

@Component({
  selector: 'app-reminders-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './reminders-list.component.html',
})
export class RemindersListComponent {
  private readonly remindersApi = inject(RemindersService);
  private readonly applicationReminderApi = inject(ApplicationReminderService);

  protected readonly saving = signal<string | null>(null);

  protected readonly remindersResource = resource<ReminderViewModel[], unknown>({
    loader: () => firstValueFrom(this.remindersApi.listReminders()),
  });

  protected readonly grouped = computed(() => {
    const all = this.remindersResource.value() ?? [];
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const overdue: ReminderViewModel[] = [];
    const dueToday: ReminderViewModel[] = [];
    const upcoming: ReminderViewModel[] = [];

    for (const r of all) {
      const due = parseDateOnly(r.dueDate as unknown as string);
      const diff = Math.round((due.getTime() - today.getTime()) / 86400000);
      if (diff < 0) overdue.push(r);
      else if (diff === 0) dueToday.push(r);
      else upcoming.push(r);
    }

    return { overdue, dueToday, upcoming };
  });

  protected async complete(reminder: ReminderViewModel): Promise<void> {
    this.saving.set(reminder.id);
    try {
      await firstValueFrom(
        this.applicationReminderApi.completeReminder(reminder.applicationId, reminder.id),
      );
      this.remindersResource.reload();
    } finally {
      this.saving.set(null);
    }
  }

  protected async deleteReminder(reminder: ReminderViewModel): Promise<void> {
    this.saving.set(reminder.id);
    try {
      await firstValueFrom(
        this.applicationReminderApi.deleteReminder(reminder.applicationId, reminder.id),
      );
      this.remindersResource.reload();
    } finally {
      this.saving.set(null);
    }
  }

  protected dueDaysLabel(reminder: ReminderViewModel): string {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const due = parseDateOnly(reminder.dueDate as unknown as string);
    const diff = Math.round((due.getTime() - today.getTime()) / 86400000);
    if (diff < 0) return `${-diff}d overdue`;
    if (diff === 0) return 'Today';
    return `In ${diff}d`;
  }
}
