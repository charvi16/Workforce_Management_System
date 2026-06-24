import { HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
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
  private readonly authService = inject(AuthService);

  getDashboard(role: 'Admin' | 'Manager' | 'Employee'): Promise<ApiResponse<DashboardResponse>> {
    const headers = this.getAuthHeaders();
    const requestHeaders = Object.fromEntries(headers.keys().map((key) => [key, headers.get(key) ?? '']));
    const tokenRole = this.getRoleFromToken() ?? role;
    const roleUrl = `${environment.apiBaseUrl}/dashboard/${tokenRole.toLowerCase()}`;
    const meUrl = `${environment.apiBaseUrl}/dashboard/me`;

    return this.fetchDashboard(roleUrl, requestHeaders)
      .catch((error) => {
        if (error?.status === 403 || error?.status === 404) {
          return this.fetchDashboard(meUrl, requestHeaders);
        }

        throw error;
      });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  private async fetchDashboard(url: string, headers: Record<string, string>): Promise<ApiResponse<DashboardResponse>> {
    const controller = new AbortController();
    const timeoutId = window.setTimeout(() => controller.abort(), 8000);

    try {
      const response = await fetch(url, { headers, signal: controller.signal });
      const body = await response.json().catch(() => null);

      if (!response.ok) {
        throw { status: response.status, error: body };
      }

      return body as ApiResponse<DashboardResponse>;
    } finally {
      window.clearTimeout(timeoutId);
    }
  }

  private getRoleFromToken(): 'Admin' | 'Manager' | 'Employee' | null {
    const token = this.authService.getToken();
    const payload = token?.split('.')[1];
    if (!payload) {
      return null;
    }

    try {
      const normalizedPayload = payload.replace(/-/g, '+').replace(/_/g, '/');
      const decoded = JSON.parse(atob(normalizedPayload.padEnd(Math.ceil(normalizedPayload.length / 4) * 4, '='))) as Record<string, unknown>;
      const role = String(decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? decoded['role'] ?? '').trim().toLowerCase();

      if (role === 'admin') {
        return 'Admin';
      }
      if (role === 'manager') {
        return 'Manager';
      }
      if (role === 'employee') {
        return 'Employee';
      }
    } catch {
      return null;
    }

    return null;
  }
}
