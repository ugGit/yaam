export interface Profile {
  id: string;
  firstName: string | null;
  lastName: string | null;
  email: string | null;
  phone: string | null;
  location: string | null;
  summary: string | null;
  skills: string[];
  workExperiences: WorkExperience[];
  educations: Education[];
  languages: Language[];
  certifications: Certification[];
  links: ProfileLink[];
  customFields: CustomField[];
}

export interface WorkExperience {
  id: string;
  company: string;
  title: string;
  startDate: string;
  endDate: string | null;
  description: string | null;
}

export interface Education {
  id: string;
  institution: string;
  degree: string | null;
  fieldOfStudy: string | null;
  startDate: string | null;
  endDate: string | null;
}

export type LanguageProficiency = 'Basic' | 'BusinessProficiency' | 'Fluent' | 'Native';

export const LANGUAGE_PROFICIENCY_LABELS: Record<LanguageProficiency, string> = {
  Basic: 'Basic',
  BusinessProficiency: 'Business Proficiency',
  Fluent: 'Fluent',
  Native: 'Native',
};

export const ALL_LANGUAGE_PROFICIENCIES: LanguageProficiency[] = [
  'Basic',
  'BusinessProficiency',
  'Fluent',
  'Native',
];

export interface Language {
  id: string;
  name: string;
  proficiency: LanguageProficiency;
}

export interface Certification {
  id: string;
  name: string;
  issuer: string | null;
  date: string;
}

export interface ProfileLink {
  id: string;
  label: string;
  url: string;
}

export interface CustomField {
  id: string;
  label: string;
  value: string;
}
