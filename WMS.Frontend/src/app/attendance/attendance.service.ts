import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { environment } from '../../environments/environment';

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

export interface AttendanceRecord {
  attendanceId: number;
  employeeId: number;
  employeeName: string;
  attendanceDate: string;
  checkIn: string;
  checkOut?: string;
  totalHours?: number;
  workMode: number;
  workModeName: string;
  status: string;
}

export interface AttendanceEmployee {
  employeeId: number;
  fullName: string;
  email: string;
  roleName: string;
}

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  getEmployees(): Observable<ApiResponse<AttendanceEmployee[]>> {
    return this.http.get<ApiResponse<AttendanceEmployee[]>>(
      `${environment.apiBaseUrl}/Attendance/employees`,
      { headers: this.getAuthHeaders() }
    );
  }

  checkIn(employeeId: number, workMode: number): Observable<ApiResponse<AttendanceRecord>> {
    return this.http.post<ApiResponse<AttendanceRecord>>(
      `${environment.apiBaseUrl}/Attendance/check-in`,
      { employeeId, workMode },
      { headers: this.getAuthHeaders() }
    );
  }

  checkOut(employeeId: number): Observable<ApiResponse<AttendanceRecord>> {
    return this.http.post<ApiResponse<AttendanceRecord>>(
      `${environment.apiBaseUrl}/Attendance/check-out`,
      { employeeId },
      { headers: this.getAuthHeaders() }
    );
  }

  getMonthlyAttendance(employeeId: number, month: number, year: number, pageNumber = 1, pageSize = 10): Observable<ApiResponse<PagedResult<AttendanceRecord>>> {
    const params = new HttpParams()
      .set('employeeId', employeeId)
      .set('month', month)
      .set('year', year)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http.get<ApiResponse<PagedResult<AttendanceRecord>>>(`${environment.apiBaseUrl}/Attendance/monthly`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
