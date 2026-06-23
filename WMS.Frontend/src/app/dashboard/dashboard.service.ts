import { HttpClient, HttpHeaders } from '@angular/common/http';
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

export interface DashboardKpis {
  totalEmployees: number;
  totalDepartments: number;
  activeEmployees: number;
  presentToday: number;
  absentToday: number;
  onLeaveToday: number;
  attendanceRate: number;
  pendingLeaves: number;
  activeProjects: number;
  delayedProjects: number;
  totalClients: number;
  unallocatedEmployees: number;
  averageWorkingHours: number;
  lateCheckInsToday: number;
}

export interface DashboardChartPoint {
  label: string;
  value: number;
}

export interface DashboardAlert {
  type: string;
  message: string;
}

export interface DashboardTableRow {
  name: string;
  detail: string;
  status: string;
}

export interface DashboardResponse {
  kpis: DashboardKpis;
  attendanceTrend: DashboardChartPoint[];
  attendanceDistribution: DashboardChartPoint[];
  leaveStatistics: DashboardChartPoint[];
  projectStatusDistribution: DashboardChartPoint[];
  departmentEmployeeCount: DashboardChartPoint[];
  workModeDistribution: DashboardChartPoint[];
  alerts: DashboardAlert[];
  todayAttendance: DashboardTableRow[];
  projectRows: DashboardTableRow[];
  pendingApprovals: string[];
  announcements: string[];
  recentActivities: string[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  getDashboard(role: 'Admin' | 'Manager' | 'Employee'): Observable<ApiResponse<DashboardResponse>> {
    return this.http.get<ApiResponse<DashboardResponse>>(`${environment.apiBaseUrl}/Dashboard/${role.toLowerCase()}`, {
      headers: this.getAuthHeaders()
    });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
