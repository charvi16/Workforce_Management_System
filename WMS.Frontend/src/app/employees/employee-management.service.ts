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

export interface Employee {
  employeeId: number;
  username: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  gender: number;
  genderName: string;
  dob: string;
  doj: string;
  departmentId: number;
  departmentName: string;
  roleId: number;
  roleName: string;
  status: number;
  statusName: string;
}

export interface EmployeeRequest {
  username: string;
  password?: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  gender: number;
  dob: string;
  doj: string;
  departmentId: number;
  roleId: number;
  status: number;
}

export interface Department {
  departmentId: number;
  departmentName: string;
  description?: string;
  employeeCount: number;
}

export interface DepartmentRequest {
  departmentName: string;
  description?: string;
}

export interface Role {
  roleId: number;
  roleName: string;
  description?: string;
}

@Injectable({ providedIn: 'root' })
export class EmployeeManagementService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  getEmployees(search = '', departmentId = '', roleId = '', status = '', pageNumber = 1, pageSize = 10): Observable<ApiResponse<PagedResult<Employee>>> {
    let params = new HttpParams();
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    if (departmentId) {
      params = params.set('departmentId', departmentId);
    }
    if (roleId) {
      params = params.set('roleId', roleId);
    }
    if (status) {
      params = params.set('status', status);
    }
    params = params
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http.get<ApiResponse<PagedResult<Employee>>>(`${environment.apiBaseUrl}/Employees`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  getEmployee(employeeId: number): Observable<ApiResponse<Employee>> {
    return this.http.get<ApiResponse<Employee>>(`${environment.apiBaseUrl}/Employees/${employeeId}`, {
      headers: this.getAuthHeaders()
    });
  }

  createEmployee(request: EmployeeRequest): Observable<ApiResponse<Employee>> {
    return this.http.post<ApiResponse<Employee>>(`${environment.apiBaseUrl}/Employees`, request, {
      headers: this.getAuthHeaders()
    });
  }

  updateEmployee(employeeId: number, request: EmployeeRequest): Observable<ApiResponse<Employee>> {
    return this.http.put<ApiResponse<Employee>>(`${environment.apiBaseUrl}/Employees/${employeeId}`, request, {
      headers: this.getAuthHeaders()
    });
  }

  deleteEmployee(employeeId: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${environment.apiBaseUrl}/Employees/${employeeId}`, {
      headers: this.getAuthHeaders()
    });
  }

  assignDepartment(employeeId: number, departmentId: number): Observable<ApiResponse<Employee>> {
    return this.http.put<ApiResponse<Employee>>(`${environment.apiBaseUrl}/Employees/${employeeId}/department`, { departmentId }, {
      headers: this.getAuthHeaders()
    });
  }

  assignRole(employeeId: number, roleId: number): Observable<ApiResponse<Employee>> {
    return this.http.put<ApiResponse<Employee>>(`${environment.apiBaseUrl}/Employees/${employeeId}/role`, { roleId }, {
      headers: this.getAuthHeaders()
    });
  }

  getDepartments(): Observable<ApiResponse<Department[]>> {
    return this.http.get<ApiResponse<Department[]>>(`${environment.apiBaseUrl}/Departments/options`, {
      headers: this.getAuthHeaders()
    });
  }

  getDepartmentPage(search = '', pageNumber = 1, pageSize = 10): Observable<ApiResponse<PagedResult<Department>>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<ApiResponse<PagedResult<Department>>>(`${environment.apiBaseUrl}/Departments`, {
      params,
      headers: this.getAuthHeaders()
    });
  }

  getDepartment(departmentId: number): Observable<ApiResponse<Department>> {
    return this.http.get<ApiResponse<Department>>(`${environment.apiBaseUrl}/Departments/${departmentId}`, {
      headers: this.getAuthHeaders()
    });
  }

  createDepartment(request: DepartmentRequest): Observable<ApiResponse<Department>> {
    return this.http.post<ApiResponse<Department>>(`${environment.apiBaseUrl}/Departments`, request, {
      headers: this.getAuthHeaders()
    });
  }

  updateDepartment(departmentId: number, request: DepartmentRequest): Observable<ApiResponse<Department>> {
    return this.http.put<ApiResponse<Department>>(`${environment.apiBaseUrl}/Departments/${departmentId}`, request, {
      headers: this.getAuthHeaders()
    });
  }

  deleteDepartment(departmentId: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${environment.apiBaseUrl}/Departments/${departmentId}`, {
      headers: this.getAuthHeaders()
    });
  }

  getRoles(): Observable<ApiResponse<Role[]>> {
    return this.http.get<ApiResponse<Role[]>>(`${environment.apiBaseUrl}/Roles`, {
      headers: this.getAuthHeaders()
    });
  }

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
