import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { resource } from '@angular/core';
import {
  ParsedCvViewModel,
  ProfileViewModel,
  CvService,
  ProfileService,
} from '../../../../generated/api';
import { ProfileInfoSectionComponent } from '../../components/profile-info-section/profile-info-section.component';
import { ProfileSkillsSectionComponent } from '../../components/profile-skills-section/profile-skills-section.component';
import { WorkExperienceSectionComponent } from '../../components/work-experience-section/work-experience-section.component';
import { EducationSectionComponent } from '../../components/education-section/education-section.component';
import { LanguageSectionComponent } from '../../components/language-section/language-section.component';
import { CertificationSectionComponent } from '../../components/certification-section/certification-section.component';
import { ProfileLinkSectionComponent } from '../../components/profile-link-section/profile-link-section.component';
import { CustomFieldsSectionComponent } from '../../components/custom-fields-section/custom-fields-section.component';
import { CvUploadComponent } from '../../components/cv-upload/cv-upload.component';
import {
  CvReviewComponent,
  CvReviewConfirmation,
} from '../../components/cv-review/cv-review.component';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [
    CommonModule,
    ProfileInfoSectionComponent,
    ProfileSkillsSectionComponent,
    WorkExperienceSectionComponent,
    EducationSectionComponent,
    LanguageSectionComponent,
    CertificationSectionComponent,
    ProfileLinkSectionComponent,
    CustomFieldsSectionComponent,
    CvUploadComponent,
    CvReviewComponent,
  ],
  templateUrl: './profile-page.component.html',
})
export class ProfilePageComponent {
  private readonly profileService = inject(ProfileService);
  private readonly cvService = inject(CvService);

  protected readonly profileResource = resource<ProfileViewModel, void>({
    loader: () => firstValueFrom(this.profileService.getProfile()),
  });

  protected readonly showCvModal = signal(false);
  protected readonly parsedCv = signal<ParsedCvViewModel | null>(null);
  protected readonly cvError = signal<string | null>(null);
  protected readonly isApplying = signal(false);

  protected get hasExistingProfileData(): boolean {
    const profile = this.profileResource.value();
    return !!(
      profile &&
      ((profile.workExperiences?.length ?? 0) > 0 ||
        (profile.educations?.length ?? 0) > 0 ||
        (profile.skills?.length ?? 0) > 0)
    );
  }

  protected onProfileChanged(profile: ProfileViewModel): void {
    this.profileResource.set(profile);
  }

  protected onOpenCvModal(): void {
    this.parsedCv.set(null);
    this.cvError.set(null);
    this.showCvModal.set(true);
  }

  protected onCvParsed(result: ParsedCvViewModel): void {
    this.parsedCv.set(result);
  }

  protected onCvParseError(message: string): void {
    this.cvError.set(message);
  }

  protected onCvReviewCancelled(): void {
    this.showCvModal.set(false);
  }

  protected async onCvReviewConfirmed(confirmation: CvReviewConfirmation): Promise<void> {
    this.isApplying.set(true);
    try {
      const profile = (await firstValueFrom(
        this.cvService.applyCv({
          selectedItems: confirmation.selectedItems,
          mode: confirmation.mode === 'Replace' ? 1 : 0,
        }),
      )) as ProfileViewModel;
      this.profileResource.set(profile);
      this.showCvModal.set(false);
    } catch {
      this.cvError.set('Failed to apply CV. Please try again.');
      this.parsedCv.set(null);
    } finally {
      this.isApplying.set(false);
    }
  }
}
