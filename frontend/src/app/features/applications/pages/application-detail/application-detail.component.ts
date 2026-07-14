import { Component, computed, inject, resource, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { firstValueFrom } from 'rxjs';
import { ApplicationsService } from '../../../../generated/api';
import { Application, STATUS_LABELS, ALL_STATUSES } from '../../models/application.model';
import { ApplicationFormComponent } from '../../components/application-form/application-form.component';
import { ApplicationNotesComponent } from '../../components/application-notes/application-notes.component';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-application-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ApplicationFormComponent, ApplicationNotesComponent],
  templateUrl: './application-detail.component.html',
})
export class ApplicationDetailComponent {
  private readonly api = inject(ApplicationsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly STATUS_LABELS = STATUS_LABELS;
  protected readonly ALL_STATUSES = ALL_STATUSES;

  protected readonly editMode = signal(false);
  protected readonly statusSaving = signal(false);
  protected readonly deleteModalOpen = signal(false);
  protected readonly serverErrors = signal<Record<string, string[]>>({});

  protected readonly applicationId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? undefined)),
  );
  protected readonly isNew = computed(() => !this.applicationId());

  protected readonly applicationResource = resource<Application | undefined, string | undefined>({
    params: () => this.applicationId(),
    loader: ({ params }) =>
      params ? firstValueFrom(this.api.getApplication(params)) : Promise.resolve(undefined),
  });

  protected async onSave(form: FormGroup): Promise<void> {
    const value = form.value;
    try {
      if (this.isNew()) {
        const created = await firstValueFrom(this.api.createApplication(value));
        await this.router.navigate(['/applications', created.id]);
      } else {
        await firstValueFrom(this.api.updateApplication(this.applicationId()!, value));
        this.applicationResource.reload();
        this.editMode.set(false);
      }
    } catch (err: unknown) {
      const apiErr = err as { errors?: Record<string, string[]> };
      this.serverErrors.set(apiErr?.errors ?? {});
    }
  }

  protected onCancelEdit(): void {
    if (this.isNew()) {
      this.router.navigate(['/applications']);
    } else {
      this.editMode.set(false);
    }
  }

  protected async onStatusChange(event: Event): Promise<void> {
    const status = (event.target as HTMLSelectElement).value;
    this.statusSaving.set(true);
    try {
      await firstValueFrom(this.api.patchApplicationStatus(this.applicationId()!, { status }));
      this.applicationResource.reload();
    } finally {
      this.statusSaving.set(false);
    }
  }

  protected async onDelete(): Promise<void> {
    await firstValueFrom(this.api.deleteApplication(this.applicationId()!));
    await this.router.navigate(['/applications']);
  }
}
