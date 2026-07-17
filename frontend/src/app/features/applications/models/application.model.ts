export type {
  ApplicationDto as Application,
  ApplicationSummaryDto as ApplicationSummary,
  ApplicationStatus,
} from '../../../generated/api/index';

export const STATUS_LABELS: Record<string, string> = {
  Draft: 'Draft',
  Applied: 'Applied',
  InterviewScheduled: 'Interview Scheduled',
  Interviewed: 'Interviewed',
  OfferReceived: 'Offer Received',
  Accepted: 'Accepted',
  Rejected: 'Rejected',
  Withdrawn: 'Withdrawn',
};

export const ALL_STATUSES = Object.keys(STATUS_LABELS);
