import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApplicationNote } from '../../models/application-note.model';

@Component({
  selector: 'app-application-notes',
  standalone: true,
  imports: [CommonModule],
  template: `<!-- Notes component — implemented in Task 13 -->`,
})
export class ApplicationNotesComponent {
  readonly applicationId = input.required<string>();
  readonly initialNotes = input<ApplicationNote[]>([]);
}
