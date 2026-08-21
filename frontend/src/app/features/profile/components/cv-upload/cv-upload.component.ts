import { Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { CvService, ParsedCvViewModel } from '../../../../generated/api';

@Component({
  selector: 'app-cv-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cv-upload.component.html',
})
export class CvUploadComponent {
  readonly parsed = output<ParsedCvViewModel>();
  readonly parseError = output<string>();

  private readonly cvService = inject(CvService);

  protected readonly selectedFile = signal<File | null>(null);
  protected readonly isParsing = signal(false);
  protected readonly validationError = signal<string | null>(null);

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.validationError.set(null);

    if (!file) return;
    if (file.type !== 'application/pdf') {
      this.validationError.set('Only PDF files are accepted.');
      return;
    }
    if (file.size > 2 * 1024 * 1024) {
      this.validationError.set('File exceeds the 2 MB limit.');
      return;
    }
    this.selectedFile.set(file);
  }

  protected async onUpload(): Promise<void> {
    const file = this.selectedFile();
    if (!file) return;

    this.isParsing.set(true);
    this.validationError.set(null);
    try {
      const result = await firstValueFrom(this.cvService.parseCv(file));
      this.parsed.emit(result);
    } catch {
      this.parseError.emit(
        'Parsing failed. Please check that the PDF contains text and try again.',
      );
    } finally {
      this.isParsing.set(false);
    }
  }
}
