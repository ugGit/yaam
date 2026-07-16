import { Component, inject, signal, resource } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApplicationsService } from '../../../../generated/api';
import { ApplicationSummary, ALL_STATUSES, STATUS_LABELS } from '../../models/application.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-application-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './application-list.component.html',
})
export class ApplicationListComponent {
  private readonly api = inject(ApplicationsService);
  protected readonly router = inject(Router);

  protected readonly STATUS_LABELS = STATUS_LABELS;
  protected readonly ALL_STATUSES = ALL_STATUSES;

  protected readonly filterStatus = signal('');
  protected readonly sort = signal('dateApplied');
  protected readonly order = signal<'asc' | 'desc'>('desc');

  protected readonly SORT_OPTIONS: { value: string; label: string }[] = [
    { value: 'dateApplied', label: 'Date applied' },
    { value: 'companyName', label: 'Company' },
  ];

  protected readonly applications = resource<
    ApplicationSummary[],
    { status: string; sort: string; order: 'asc' | 'desc' }
  >({
    params: () => ({
      status: this.filterStatus(),
      sort: this.sort(),
      order: this.order(),
    }),
    loader: ({ params }) =>
      firstValueFrom(
        this.api.listApplications(params.status || undefined, params.sort, params.order),
      ),
  });

  protected setFilter(status: string): void {
    this.filterStatus.set(status);
  }

  protected setSort(sortField: string): void {
    this.sort.set(sortField);
  }

  protected toggleOrder(): void {
    this.order.update((o) => (o === 'desc' ? 'asc' : 'desc'));
  }

  protected statusBadgeClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'badge-ghost',
      Applied: 'badge-info',
      InterviewScheduled: 'badge-primary',
      Interviewed: 'badge-primary',
      OfferReceived: 'badge-success',
      Accepted: 'badge-success',
      Rejected: 'badge-error',
      Withdrawn: 'badge-warning',
    };
    return map[status] ?? 'badge-ghost';
  }
}
