import { Component, inject, input, linkedSignal, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, maxLength, required, submit } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { ApplicationNotesService } from '../../../../generated/api/index';
import { ApplicationNote } from '../../models/application-note.model';

@Component({
  selector: 'app-application-notes',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './application-notes.component.html',
})
export class ApplicationNotesComponent {
  readonly applicationId = input.required<string>();
  readonly initialNotes = input<ApplicationNote[]>([]);

  private readonly api = inject(ApplicationNotesService);

  protected readonly notes = linkedSignal(() => [...this.initialNotes()]);
  protected deleteConfirmId = signal<string | null>(null);
  protected editingNoteId = signal<string | null>(null);

  protected readonly newNoteModel = signal({ body: '' });
  protected readonly newNoteFields = form(this.newNoteModel, (fields) => {
    required(fields.body);
    maxLength(fields.body, 5000, { message: 'Note must be 5000 characters or fewer.' });
  });

  protected readonly editingModel = signal({ body: '' });
  protected readonly editingFields = form(this.editingModel, (fields) => {
    required(fields.body);
    maxLength(fields.body, 5000, { message: 'Note must be 5000 characters or fewer.' });
  });

  protected async addNote(): Promise<void> {
    await submit(this.newNoteFields, async () => {
      const note = await firstValueFrom(
        this.api.addApplicationNote(this.applicationId(), { body: this.newNoteModel().body }),
      );
      this.notes.update((n) => [note, ...n]);
      this.newNoteModel.set({ body: '' });
      return undefined;
    });
  }

  protected startEdit(note: ApplicationNote): void {
    this.editingNoteId.set(note.id);
    this.editingModel.set({ body: note.body });
  }

  protected cancelEdit(): void {
    this.editingNoteId.set(null);
    this.editingModel.set({ body: '' });
  }

  protected async saveEdit(noteId: string): Promise<void> {
    await submit(this.editingFields, async () => {
      const updated = await firstValueFrom(
        this.api.updateApplicationNote(this.applicationId(), noteId, {
          body: this.editingModel().body,
        }),
      );
      this.notes.update((notes) => notes.map((n) => (n.id === noteId ? updated : n)));
      this.editingNoteId.set(null);
      return undefined;
    });
  }

  protected async deleteNote(noteId: string): Promise<void> {
    await firstValueFrom(this.api.deleteApplicationNote(this.applicationId(), noteId));
    this.notes.update((notes) => notes.filter((n) => n.id !== noteId));
    this.deleteConfirmId.set(null);
  }

  protected isEdited(note: ApplicationNote): boolean {
    return note.updatedAt !== note.createdAt;
  }
}
