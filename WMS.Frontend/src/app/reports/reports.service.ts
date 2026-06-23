import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/auth.service';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  generateAttendanceReport(payload: {
    employeeId?: number | null;
    departmentId?: number | null;
    fromDate: string;
    toDate: string;
    reportType?: string | null;
  }): Observable<Blob> {
    return this.http.post(`${environment.apiBaseUrl}/Reports/attendance`, payload, {
      headers: this.getAuthHeaders(),
      responseType: 'blob'
    });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
