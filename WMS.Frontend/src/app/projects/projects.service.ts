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

export interface Project {
  projectId: number;
  projectName: string;
  clientId?: number;
  clientName?: string;
  startDate?: string;
  endDate?: string;
  status: string;
  membersCount: number;
}

export interface ProjectRequest {
  projectName: string;
  clientId?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  status: string;
  memberIds: number[];
}

export interface ProjectAllocation {
  allocationId: number;
  empId: number;
  employeeName: string;
  projectId: number;
  projectName: string;
  clientName?: string;
  assignedOn: string;
  roleInProject?: string;
  allocationPercentage?: number;
  status: boolean;
  createdOn: string;
  createdBy: number;
  updatedOn?: string;
  updatedBy?: number;
}

export interface ProjectAllocationRequest {
  empId: number;
  projectId: number;
  assignedOn: string;
  roleInProject?: string;
  allocationPercentage?: number;
  status: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  getProjects(search = '', clientId?: number, status = '', pageNumber = 1, pageSize = 10): Observable<ApiResponse<PagedResult<Project>>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    if (clientId) {
      params = params.set('clientId', clientId);
    }
    if (status.trim()) {
      params = params.set('status', status.trim());
    }

    return this.http.get<ApiResponse<PagedResult<Project>>>(`${environment.apiBaseUrl}/Projects`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  getProject(projectId: number): Observable<ApiResponse<Project>> {
    return this.http.get<ApiResponse<Project>>(`${environment.apiBaseUrl}/Projects/${projectId}`, {
      headers: this.getAuthHeaders()
    });
  }

  getProjectsByClient(clientId: number, pageNumber = 1, pageSize = 10): Observable<ApiResponse<PagedResult<Project>>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResult<Project>>>(`${environment.apiBaseUrl}/Projects/by-client/${clientId}`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  createProject(request: ProjectRequest): Observable<ApiResponse<Project>> {
    return this.http.post<ApiResponse<Project>>(`${environment.apiBaseUrl}/Projects`, request, {
      headers: this.getAuthHeaders()
    });
  }

  updateProject(projectId: number, request: ProjectRequest): Observable<ApiResponse<Project>> {
    return this.http.put<ApiResponse<Project>>(`${environment.apiBaseUrl}/Projects/${projectId}`, request, {
      headers: this.getAuthHeaders()
    });
  }

  updateProjectStatus(projectId: number, status: string): Observable<ApiResponse<Project>> {
    return this.http.patch<ApiResponse<Project>>(`${environment.apiBaseUrl}/Projects/${projectId}/status`, { status }, {
      headers: this.getAuthHeaders()
    });
  }

  cancelProject(projectId: number): Observable<ApiResponse<Project>> {
    return this.http.delete<ApiResponse<Project>>(`${environment.apiBaseUrl}/Projects/${projectId}`, {
      headers: this.getAuthHeaders()
    });
  }

  getAllocationsByProject(projectId: number, pageNumber = 1, pageSize = 10): Observable<ApiResponse<PagedResult<ProjectAllocation>>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResult<ProjectAllocation>>>(`${environment.apiBaseUrl}/ProjectAllocations/project/${projectId}`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  createAllocation(request: ProjectAllocationRequest): Observable<ApiResponse<ProjectAllocation>> {
    return this.http.post<ApiResponse<ProjectAllocation>>(`${environment.apiBaseUrl}/ProjectAllocations`, request, {
      headers: this.getAuthHeaders()
    });
  }

  updateAllocation(allocationId: number, request: ProjectAllocationRequest): Observable<ApiResponse<ProjectAllocation>> {
    return this.http.put<ApiResponse<ProjectAllocation>>(`${environment.apiBaseUrl}/ProjectAllocations/${allocationId}`, request, {
      headers: this.getAuthHeaders()
    });
  }

  deleteAllocation(allocationId: number): Observable<ApiResponse<ProjectAllocation>> {
    return this.http.delete<ApiResponse<ProjectAllocation>>(`${environment.apiBaseUrl}/ProjectAllocations/${allocationId}`, {
      headers: this.getAuthHeaders()
    });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
