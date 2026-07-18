import { Component, computed, inject, resource, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationReminderService,
  RemindersService,
  ReminderViewModel,
} from '../../../../generated/api/index';

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
      const [y, m, d] = (r.dueDate as unknown as string).split('-').map(Number);
      const due = new Date(y, m - 1, d);
      const diff = Math.round((due.getTime() - today.getTime()) / 86400000);
      if (diff < 0) overdue.push(r);
      else if (diff === 0) dueToday.push(r);
      else upcoming.push(r);
    }

    return { overdue, dueToday, upcoming };
  });

  protected async complete(applicationId: string): Promise<void> {
    this.saving.set(applicationId);
    try {
      await firstValueFrom(this.applicationReminderApi.completeReminder(applicationId));
      this.remindersResource.reload();
    } finally {
      this.saving.set(null);
    }
  }

  protected async deleteReminder(applicationId: string): Promise<void> {
    this.saving.set(applicationId);
    try {
      await firstValueFrom(this.applicationReminderApi.deleteReminder(applicationId));
      this.remindersResource.reload();
    } finally {
      this.saving.set(null);
    }
  }

  protected dueDaysLabel(reminder: ReminderViewModel): string {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const [y, m, d] = (reminder.dueDate as unknown as string).split('-').map(Number);
    const due = new Date(y, m - 1, d);
    const diff = Math.round((due.getTime() - today.getTime()) / 86400000);
    if (diff < 0) return `${-diff}d overdue`;
    if (diff === 0) return 'Today';
    return `In ${diff}d`;
  }
}
