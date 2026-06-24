import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { Department, EmployeeManagementService } from '../employees/employee-management.service';
import { normalizePagedResponse } from '../shared/pagination';

type DepartmentMode = 'list' | 'add' | 'edit' | 'detail';

@Component({
  selector: 'app-departments',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './departments.html',
  styleUrl: './departments.scss'
})
export class Departments implements OnInit {
  private readonly employeeService = inject(EmployeeManagementService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected mode: DepartmentMode = 'list';
  protected departments: Department[] = [];
  protected currentDepartment: Department | null = null;
  protected search = '';
  protected pageNumber = 1;
  protected pageSize = 10;
  protected totalCount = 0;
  protected totalPages = 0;
  protected isLoading = false;
  protected isSaving = false;
  protected message = '';
  protected errorMessage = '';
  protected readonly canManage = this.authService.getCurrentUser()?.role?.trim().toLowerCase() === 'admin';

  protected get hasPreviousPage(): boolean {
    return this.pageNumber > 1;
  }

  protected get hasNextPage(): boolean {
    return this.totalPages > 1 && this.pageNumber < this.totalPages;
  }

  protected readonly departmentForm = this.formBuilder.group({
    departmentName: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(255)]]
  });

  ngOnInit(): void {
    this.mode = this.resolveMode();
    if (this.mode === 'list') {
      this.loadDepartments();
      return;
    }

    this.loadRouteDepartment();
  }

  loadDepartments(): void {
    this.isLoading = true;
    this.employeeService.getDepartmentPage(this.search, this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        const page = this.normalizePage<Department>(response, this.pageNumber, this.pageSize);
        this.departments = page?.items ?? [];
        this.totalCount = page?.totalCount ?? 0;
        this.pageNumber = page?.pageNumber ?? this.pageNumber;
        this.pageSize = page?.pageSize ?? this.pageSize;
        this.totalPages = page?.totalPages ?? 0;
        this.errorMessage = '';
        this.isLoading = false;
      },
      error: (error) => {
        this.departments = [];
        this.errorMessage = this.getApiError(error, 'Unable to load departments.');
        this.isLoading = false;
      }
    });
  }

  searchDepartments(): void {
    this.pageNumber = 1;
    this.loadDepartments();
  }

  clearSearch(): void {
    this.search = '';
    this.pageNumber = 1;
    this.loadDepartments();
  }

  changePage(delta: number): void {
    const nextPage = this.pageNumber + delta;
    if ((delta < 0 && !this.hasPreviousPage) || (delta > 0 && !this.hasNextPage) || nextPage < 1) {
      return;
    }

    this.pageNumber = nextPage;
    this.loadDepartments();
  }

  saveDepartment(): void {
    if (!this.canManage) {
      this.errorMessage = 'Only admins can add or edit departments.';
      return;
    }

    if (this.departmentForm.invalid || this.isSaving) {
      this.departmentForm.markAllAsTouched();
      this.errorMessage = 'Please enter a department name before saving.';
      return;
    }

    const id = Number(this.route.snapshot.paramMap.get('id'));
    const value = this.departmentForm.getRawValue();
    const request = {
      departmentName: (value.departmentName ?? '').trim(),
      description: (value.description ?? '').trim()
    };

    if (!request.departmentName) {
      this.departmentForm.markAllAsTouched();
      this.errorMessage = 'Department name is required.';
      return;
    }

    const save$ = this.mode === 'edit' && id > 0
      ? this.employeeService.updateDepartment(id, request)
      : this.employeeService.createDepartment(request);

    this.isSaving = true;
    this.message = '';
    this.errorMessage = '';
    save$.subscribe({
      next: (response) => {
        this.isSaving = false;
        this.message = response.message;
        void this.router.navigateByUrl('/departments');
      },
      error: (error) => {
        this.isSaving = false;
        this.errorMessage = this.getApiError(error, 'Unable to save department.');
      }
    });
  }

  deleteDepartment(department: Department): void {
    if (!this.canManage) {
      return;
    }

    this.employeeService.deleteDepartment(department.departmentId).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadDepartments();
      },
      error: (error) => this.errorMessage = this.getApiError(error, 'Unable to delete department.')
    });
  }

  private loadRouteDepartment(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id) || id <= 0) {
      return;
    }

    this.isLoading = true;
    this.employeeService.getDepartment(id).subscribe({
      next: (response) => {
        this.currentDepartment = response.data ?? null;
        if (this.mode === 'edit' && this.currentDepartment) {
          this.departmentForm.patchValue({
            departmentName: this.currentDepartment.departmentName,
            description: this.currentDepartment.description ?? ''
          });
        }
        this.errorMessage = '';
        this.isLoading = false;
      },
      error: (error) => {
        this.currentDepartment = null;
        this.errorMessage = this.getApiError(error, 'Unable to load department.');
        this.isLoading = false;
      }
    });
  }

  private getApiError(error: unknown, fallback: string): string {
    const response = error as {
      status?: number;
      error?: { errors?: unknown; message?: string; title?: string };
      message?: string;
    };

    if (response.status === 0) {
      return 'Cannot reach the backend API. Make sure the backend is running on the configured API URL.';
    }

    if (response.status === 401) {
      return 'Your login session is missing or expired. Login again as Admin.';
    }

    if (response.status === 403) {
      return 'Only Admin users can perform this action.';
    }

    const errors = response.error?.errors;
    if (Array.isArray(errors) && errors.length > 0) {
      return String(errors[0]);
    }

    if (errors && typeof errors === 'object') {
      const first = Object.values(errors as Record<string, unknown>)[0];
      if (Array.isArray(first) && first.length > 0) {
        return String(first[0]);
      }
      if (first) {
        return String(first);
      }
    }

    return response.error?.message ?? response.error?.title ?? response.message ?? fallback;
  }

  private resolveMode(): DepartmentMode {
    const path = this.route.snapshot.routeConfig?.path ?? 'departments';
    if (path.includes('add')) {
      return 'add';
    }
    if (path.includes('edit')) {
      return 'edit';
    }
    if (this.route.snapshot.paramMap.has('id')) {
      return 'detail';
    }
    return 'list';
  }

  private normalizePage<T>(response: unknown, fallbackPageNumber: number, fallbackPageSize: number): { items: T[]; totalCount: number; pageNumber: number; pageSize: number; totalPages: number } | null {
    return normalizePagedResponse<T>(response, fallbackPageNumber, fallbackPageSize);
  }
}
