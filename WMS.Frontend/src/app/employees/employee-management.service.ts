import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

export interface Employee {
  employeeId: number;
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

  getEmployees(search = '', departmentId = '', roleId = ''): Observable<ApiResponse<Employee[]>> {
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

    return this.http.get<ApiResponse<Employee[]>>(`${environment.apiBaseUrl}/Employees`, { params });
  }

  createEmployee(request: EmployeeRequest): Observable<ApiResponse<Employee>> {
    return this.http.post<ApiResponse<Employee>>(`${environment.apiBaseUrl}/Employees`, request);
  }

  updateEmployee(employeeId: number, request: EmployeeRequest): Observable<ApiResponse<Employee>> {
    return this.http.put<ApiResponse<Employee>>(`${environment.apiBaseUrl}/Employees/${employeeId}`, request);
  }

  deleteEmployee(employeeId: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${environment.apiBaseUrl}/Employees/${employeeId}`);
  }

  assignDepartment(employeeId: number, departmentId: number): Observable<ApiResponse<Employee>> {
    return this.http.put<ApiResponse<Employee>>(`${environment.apiBaseUrl}/Employees/${employeeId}/department`, { departmentId });
  }

  assignRole(employeeId: number, roleId: number): Observable<ApiResponse<Employee>> {
    return this.http.put<ApiResponse<Employee>>(`${environment.apiBaseUrl}/Employees/${employeeId}/role`, { roleId });
  }

  getDepartments(): Observable<ApiResponse<Department[]>> {
    return this.http.get<ApiResponse<Department[]>>(`${environment.apiBaseUrl}/Departments`);
  }

  createDepartment(request: DepartmentRequest): Observable<ApiResponse<Department>> {
    return this.http.post<ApiResponse<Department>>(`${environment.apiBaseUrl}/Departments`, request);
  }

  updateDepartment(departmentId: number, request: DepartmentRequest): Observable<ApiResponse<Department>> {
    return this.http.put<ApiResponse<Department>>(`${environment.apiBaseUrl}/Departments/${departmentId}`, request);
  }

  deleteDepartment(departmentId: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${environment.apiBaseUrl}/Departments/${departmentId}`);
  }

  getRoles(): Observable<ApiResponse<Role[]>> {
    return this.http.get<ApiResponse<Role[]>>(`${environment.apiBaseUrl}/Roles`);
  }
}
