// Stub for development — will be replaced by openapi-generator-cli output

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export type ApplicationStatus =
  | 'Draft'
  | 'Applied'
  | 'InterviewScheduled'
  | 'Interviewed'
  | 'OfferReceived'
  | 'Accepted'
  | 'Rejected'
  | 'Withdrawn';

export interface ApplicationSummaryDto {
  id: string;
  companyName: string;
  role: string;
  dateApplied?: string;
  status: ApplicationStatus;
}

export interface ApplicationNoteDto {
  id: string;
  body: string;
  createdAt: string;
  updatedAt: string;
}

export interface ApplicationDto extends ApplicationSummaryDto {
  contactName?: string;
  contactEmail?: string;
  contactPhone?: string;
  jobPosting?: string;
  createdAt: string;
  updatedAt: string;
  notes: ApplicationNoteDto[];
}

@Injectable({ providedIn: 'root' })
export class ApplicationsService {
  private readonly baseUrl = '/api/applications';
  constructor(private http: HttpClient) {}

  listApplications(status?: string, sort?: string, order?: string): Observable<ApplicationSummaryDto[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    if (sort) params = params.set('sort', sort);
    if (order) params = params.set('order', order);
    return this.http.get<ApplicationSummaryDto[]>(this.baseUrl, { params });
  }

  getApplication(id: string): Observable<ApplicationDto> {
    return this.http.get<ApplicationDto>(`${this.baseUrl}/${id}`);
  }

  createApplication(body: Partial<ApplicationDto>): Observable<ApplicationDto> {
    return this.http.post<ApplicationDto>(this.baseUrl, body);
  }

  updateApplication(id: string, body: Partial<ApplicationDto>): Observable<ApplicationDto> {
    return this.http.put<ApplicationDto>(`${this.baseUrl}/${id}`, body);
  }

  patchApplicationStatus(id: string, body: { status: string }): Observable<ApplicationDto> {
    return this.http.patch<ApplicationDto>(`${this.baseUrl}/${id}/status`, body);
  }

  deleteApplication(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  addApplicationNote(id: string, body: { body: string }): Observable<ApplicationNoteDto> {
    return this.http.post<ApplicationNoteDto>(`${this.baseUrl}/${id}/notes`, body);
  }

  updateApplicationNote(id: string, noteId: string, body: { body: string }): Observable<ApplicationNoteDto> {
    return this.http.put<ApplicationNoteDto>(`${this.baseUrl}/${id}/notes/${noteId}`, body);
  }

  deleteApplicationNote(id: string, noteId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/notes/${noteId}`);
  }
}
