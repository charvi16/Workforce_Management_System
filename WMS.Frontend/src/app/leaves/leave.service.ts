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

export interface LeaveEmployee {
  employeeId: number;
  fullName: string;
  email: string;
  roleName: string;
}

export interface LeaveRecord {
  leaveId: number;
  employeeId: number;
  employeeName: string;
  leaveType: number;
  leaveTypeName: string;
  reason?: string;
  fromDate: string;
  toDate: string;
  totalDays: number;
  status: number;
  statusName: string;
  appliedOn: string;
  approvedBy?: number;
  approverName?: string;
  approvedOn?: string;
}

export interface LeaveStatistics {
  totalRequests: number;
  pendingRequests: number;
  approvedRequests: number;
  rejectedRequests: number;
  cancelledRequests: number;
  approvedDays: number;
}

@Injectable({ providedIn: 'root' })
export class LeaveService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  getEmployees(): Observable<ApiResponse<LeaveEmployee[]>> {
    return this.http.get<ApiResponse<LeaveEmployee[]>>(`${environment.apiBaseUrl}/Leaves/employees`, {
      headers: this.getAuthHeaders()
    });
  }

  getLeaves(employeeId?: number, status?: number, fromDate?: string, toDate?: string, pageNumber = 1, pageSize = 10): Observable<ApiResponse<PagedResult<LeaveRecord>>> {
    let params = new HttpParams();
    if (employeeId !== undefined && employeeId !== null) {
      params = params.set('employeeId', employeeId);
    }
    if (status) {
      params = params.set('status', status);
    }
    if (fromDate) {
      params = params.set('fromDate', fromDate);
    }
    if (toDate) {
      params = params.set('toDate', toDate);
    }
    params = params
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http.get<ApiResponse<PagedResult<LeaveRecord>>>(`${environment.apiBaseUrl}/Leaves/status`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  getStatistics(employeeId?: number, year?: number): Observable<ApiResponse<LeaveStatistics>> {
    let params = new HttpParams();
    if (employeeId !== undefined && employeeId !== null) {
      params = params.set('employeeId', employeeId);
    }
    if (year) {
      params = params.set('year', year);
    }

    return this.http.get<ApiResponse<LeaveStatistics>>(`${environment.apiBaseUrl}/Leaves/statistics`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  applyLeave(employeeId: number, leaveType: number, fromDate: string, toDate: string, reason?: string): Observable<ApiResponse<LeaveRecord>> {
    return this.http.post<ApiResponse<LeaveRecord>>(
      `${environment.apiBaseUrl}/Leaves/apply`,
      { employeeId, leaveType, fromDate, toDate, reason },
      { headers: this.getAuthHeaders() }
    );
  }

  cancelLeave(leaveId: number): Observable<ApiResponse<LeaveRecord>> {
    return this.http.put<ApiResponse<LeaveRecord>>(
      `${environment.apiBaseUrl}/Leaves/${leaveId}/cancel`,
      {},
      { headers: this.getAuthHeaders() }
    );
  }

  reviewLeave(leaveId: number, isApproved: boolean): Observable<ApiResponse<LeaveRecord>> {
    return this.http.put<ApiResponse<LeaveRecord>>(
      `${environment.apiBaseUrl}/Leaves/${leaveId}/review`,
      { isApproved },
      { headers: this.getAuthHeaders() }
    );
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
