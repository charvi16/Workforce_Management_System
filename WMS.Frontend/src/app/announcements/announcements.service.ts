import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/auth.service';

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface Announcement {
  announcementId: number;
  title: string;
  message: string;
  createdBy: number;
  createdByName?: string;
  createdOn: string;
  updatedOn?: string;
  isActive: boolean;
  targetRole?: string;
  expiryDate?: string;
}

export interface AnnouncementRequest {
  title: string;
  message: string;
  targetRole?: string;
  expiryDate?: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class AnnouncementsService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  getAnnouncements(pageNumber = 1, pageSize = 10): Observable<ApiResponse<PagedResult<Announcement>>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http.get<ApiResponse<PagedResult<Announcement>>>(`${environment.apiBaseUrl}/Announcements`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  getAllVisibleAnnouncements(): Observable<ApiResponse<PagedResult<Announcement>>> {
    return this.getAnnouncements(1, 100);
  }

  getAnnouncement(id: number): Observable<ApiResponse<Announcement>> {
    return this.http.get<ApiResponse<Announcement>>(`${environment.apiBaseUrl}/Announcements/${id}`, {
      headers: this.getAuthHeaders()
    });
  }

  createAnnouncement(request: AnnouncementRequest): Observable<ApiResponse<Announcement>> {
    return this.http.post<ApiResponse<Announcement>>(`${environment.apiBaseUrl}/Announcements`, request, {
      headers: this.getAuthHeaders()
    });
  }

  updateAnnouncement(id: number, request: AnnouncementRequest): Observable<ApiResponse<Announcement>> {
    return this.http.put<ApiResponse<Announcement>>(`${environment.apiBaseUrl}/Announcements/${id}`, request, {
      headers: this.getAuthHeaders()
    });
  }

  deactivateAnnouncement(id: number): Observable<ApiResponse<Announcement>> {
    return this.http.delete<ApiResponse<Announcement>>(`${environment.apiBaseUrl}/Announcements/${id}`, {
      headers: this.getAuthHeaders()
    });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
