export * from './applicationNotes.service';
import { ApplicationNotesService } from './applicationNotes.service';
export * from './applicationNotes.serviceInterface';
export * from './applicationReminder.service';
import { ApplicationReminderService } from './applicationReminder.service';
export * from './applicationReminder.serviceInterface';
export * from './applications.service';
import { ApplicationsService } from './applications.service';
export * from './applications.serviceInterface';
export * from './cv.service';
import { CvService } from './cv.service';
export * from './cv.serviceInterface';
export * from './profile.service';
import { ProfileService } from './profile.service';
export * from './profile.serviceInterface';
export * from './reminders.service';
import { RemindersService } from './reminders.service';
export * from './reminders.serviceInterface';
export const APIS = [
  ApplicationNotesService,
  ApplicationReminderService,
  ApplicationsService,
  CvService,
  ProfileService,
  RemindersService,
];
