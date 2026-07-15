import { Component, OnInit, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormField, FormRoot, form, required, submit } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApplicationsService } from '../../../../generated/api';
import { ApplicationNote } from '../../models/application-note.model';

@Component({
  selector: 'app-application-notes',
  standalone: true,
  imports: [CommonModule, FormField, FormRoot],
  templateUrl: './application-notes.component.html',
})
export class ApplicationNotesComponent implements OnInit {
  readonly applicationId = input.required<string>();
  readonly initialNotes = input<ApplicationNote[]>([]);

  private readonly api = inject(ApplicationsService);

  protected notes = signal<ApplicationNote[]>([]);
  protected deleteConfirmId = signal<string | null>(null);
  protected editingNoteId = signal<string | null>(null);

  protected readonly newNoteModel = signal({ body: '' });
  protected readonly newNoteFields = form(this.newNoteModel, (fields) => {
    required(fields.body);
  });

  protected readonly editingModel = signal({ body: '' });
  protected readonly editingFields = form(this.editingModel, (fields) => {
    required(fields.body);
  });

  ngOnInit(): void {
    this.notes.set([...this.initialNotes()]);
  }

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
