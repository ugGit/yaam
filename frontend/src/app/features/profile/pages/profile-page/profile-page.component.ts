import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { resource } from '@angular/core';
import { ProfileDto, ProfileService } from '../../../../generated/api/index';
import { ProfileInfoSectionComponent } from '../../components/profile-info-section/profile-info-section.component';
import { ProfileSkillsSectionComponent } from '../../components/profile-skills-section/profile-skills-section.component';
import { WorkExperienceSectionComponent } from '../../components/work-experience-section/work-experience-section.component';
import { EducationSectionComponent } from '../../components/education-section/education-section.component';
import { LanguageSectionComponent } from '../../components/language-section/language-section.component';
import { CertificationSectionComponent } from '../../components/certification-section/certification-section.component';
import { ProfileLinkSectionComponent } from '../../components/profile-link-section/profile-link-section.component';
import { CustomFieldsSectionComponent } from '../../components/custom-fields-section/custom-fields-section.component';

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
  ],
  templateUrl: './profile-page.component.html',
})
export class ProfilePageComponent {
  private readonly profileService = inject(ProfileService);

  protected readonly profileResource = resource<ProfileDto, void>({
    loader: () => firstValueFrom(this.profileService.getProfile()),
  });
}
