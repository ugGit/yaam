import { Component, input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApplicationsService } from '../../../../generated/api';
import { ApplicationNote } from '../../models/application-note.model';

@Component({
  selector: 'app-application-notes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './application-notes.component.html',
})
export class ApplicationNotesComponent implements OnInit {
  readonly applicationId = input.required<string>();
  readonly initialNotes = input<ApplicationNote[]>([]);

  private readonly api = inject(ApplicationsService);

  protected notes = signal<ApplicationNote[]>([]);
  protected newNoteBody = signal('');
  protected editingNoteId = signal<string | null>(null);
  protected editingBody = signal('');
  protected deleteConfirmId = signal<string | null>(null);

  ngOnInit(): void {
    this.notes.set([...this.initialNotes()]);
  }

  protected async addNote(): Promise<void> {
    const body = this.newNoteBody().trim();
    if (!body) return;
    const note = await firstValueFrom(this.api.addApplicationNote(this.applicationId(), { body }));
    this.notes.update((n) => [note, ...n]);
    this.newNoteBody.set('');
  }

  protected startEdit(note: ApplicationNote): void {
    this.editingNoteId.set(note.id);
    this.editingBody.set(note.body);
  }

  protected cancelEdit(): void {
    this.editingNoteId.set(null);
    this.editingBody.set('');
  }

  protected async saveEdit(noteId: string): Promise<void> {
    const body = this.editingBody().trim();
    if (!body) return;
    const updated = await firstValueFrom(
      this.api.updateApplicationNote(this.applicationId(), noteId, { body }),
    );
    this.notes.update((notes) => notes.map((n) => (n.id === noteId ? updated : n)));
    this.editingNoteId.set(null);
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
