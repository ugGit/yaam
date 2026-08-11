import { Component, ElementRef, OnInit, input, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ParsedCvData,
  ParsedCertificationData,
  ParsedEducationData,
  ParsedLanguageData,
  ParsedWorkExperienceData,
} from '../../../../generated/api';

export interface CvReviewConfirmation {
  selectedItems: ParsedCvData;
  mode: 'Add' | 'Replace';
}

interface CheckedItem<T> {
  checked: boolean;
  data: T;
}

@Component({
  selector: 'app-cv-review',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cv-review.component.html',
})
export class CvReviewComponent implements OnInit {
  readonly parsedData = input.required<ParsedCvData>();
  readonly hasExistingData = input.required<boolean>();
  readonly confirmed = output<CvReviewConfirmation>();
  readonly cancelled = output<void>();

  private readonly replaceConfirmDialog =
    viewChild.required<ElementRef<HTMLDialogElement>>('replaceConfirmDialog');

  protected readonly mode = signal<'Add' | 'Replace'>('Add');
  protected readonly infoChecked = signal(true);
  private readonly _workExperiences = signal<CheckedItem<ParsedWorkExperienceData>[]>([]);
  private readonly _educations = signal<CheckedItem<ParsedEducationData>[]>([]);
  private readonly _languages = signal<CheckedItem<ParsedLanguageData>[]>([]);
  private readonly _certifications = signal<CheckedItem<ParsedCertificationData>[]>([]);
  private readonly _skills = signal<CheckedItem<string>[]>([]);

  protected get workExperiences(): CheckedItem<ParsedWorkExperienceData>[] {
    return this._workExperiences();
  }
  protected get educations(): CheckedItem<ParsedEducationData>[] {
    return this._educations();
  }
  protected get languages(): CheckedItem<ParsedLanguageData>[] {
    return this._languages();
  }
  protected get certifications(): CheckedItem<ParsedCertificationData>[] {
    return this._certifications();
  }
  protected get skills(): CheckedItem<string>[] {
    return this._skills();
  }

  ngOnInit(): void {
    const data = this.parsedData();
    this._workExperiences.set(data.workExperiences.map((item) => ({ checked: true, data: item })));
    this._educations.set(data.educations.map((item) => ({ checked: true, data: item })));
    this._languages.set(data.languages.map((item) => ({ checked: true, data: item })));
    this._certifications.set(data.certifications.map((item) => ({ checked: true, data: item })));
    this._skills.set(data.skills.map((item) => ({ checked: true, data: item })));
  }

  protected toggleWorkExperience(index: number): void {
    const items = [...this._workExperiences()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this._workExperiences.set(items);
  }

  protected toggleEducation(index: number): void {
    const items = [...this._educations()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this._educations.set(items);
  }

  protected toggleLanguage(index: number): void {
    const items = [...this._languages()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this._languages.set(items);
  }

  protected toggleCertification(index: number): void {
    const items = [...this._certifications()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this._certifications.set(items);
  }

  protected toggleSkill(index: number): void {
    const items = [...this._skills()];
    items[index] = { ...items[index], checked: !items[index].checked };
    this._skills.set(items);
  }

  protected onConfirm(): void {
    if (this.mode() === 'Replace') {
      this.replaceConfirmDialog().nativeElement.showModal();
      return;
    }
    this.emitConfirmed();
  }

  protected onReplaceConfirmed(): void {
    this.replaceConfirmDialog().nativeElement.close();
    this.emitConfirmed();
  }

  protected onReplaceCancelled(): void {
    this.replaceConfirmDialog().nativeElement.close();
  }

  private emitConfirmed(): void {
    const data = this.parsedData();
    const selectedItems: ParsedCvData = {
      info: this.infoChecked() ? data.info : null,
      workExperiences: this._workExperiences().filter((item) => item.checked).map((item) => item.data),
      educations: this._educations().filter((item) => item.checked).map((item) => item.data),
      skills: this._skills().filter((item) => item.checked).map((item) => item.data),
      languages: this._languages().filter((item) => item.checked).map((item) => item.data),
      certifications: this._certifications().filter((item) => item.checked).map((item) => item.data),
    };
    this.confirmed.emit({ selectedItems, mode: this.mode() });
  }
}
