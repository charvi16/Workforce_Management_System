import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, filter } from 'rxjs';
import {
  Department,
  Employee,
  EmployeeManagementService,
  EmployeeRequest,
  Role
} from './employee-management.service';
import { AuthService } from '../auth/auth.service';
import { normalizePagedResponse } from '../shared/pagination';

@Component({
  selector: 'app-employee-management',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './employee-management.html',
  styleUrl: './employee-management.scss'
})
export class EmployeeManagement implements OnInit {
  private readonly employeeService = inject(EmployeeManagementService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected employees: Employee[] = [];
  protected totalEmployees = 0;
  protected employeePageNumber = 1;
  protected employeePageSize = 10;
  protected employeeTotalPages = 0;
  protected departments: Department[] = [];
  protected roles: Role[] = [];
  protected editingEmployeeId: number | null = null;
  protected editingDepartmentId: number | null = null;
  protected pageMode: 'list' | 'add' | 'edit' = 'list';
  protected isLoading = false;
  protected isSaving = false;
  protected message = '';
  protected errorMessage = '';
  protected readonly currentUser = this.authService.getCurrentUser();
  protected readonly canManageOrganization = this.currentUser?.role?.trim().toLowerCase() === 'admin';

  protected get hasPreviousEmployeePage(): boolean {
    return this.employeePageNumber > 1;
  }

  protected get hasNextEmployeePage(): boolean {
    return this.employeeTotalPages > 1 && this.employeePageNumber < this.employeeTotalPages;
  }

  protected readonly genders = [
    { id: 1, name: 'Male' },
    { id: 2, name: 'Female' },
    { id: 3, name: 'Other' }
  ];

  protected readonly statuses = [
    { id: 1, name: 'Active' },
    { id: 2, name: 'Inactive' }
  ];

  protected readonly filterForm = this.formBuilder.group({
    search: [''],
    departmentId: [''],
    roleId: [''],
    status: ['']
  });

  protected readonly employeeForm = this.formBuilder.group({
    username: ['', [Validators.required, Validators.maxLength(50)]],
    password: ['', [Validators.minLength(8)]],
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(80)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(15)]],
    gender: [1, [Validators.required]],
    dob: ['2000-01-01', [Validators.required]],
    doj: [this.today(), [Validators.required]],
    departmentId: ['', [Validators.required]],
    roleId: ['', [Validators.required]],
    status: [1, [Validators.required]]
  });

  protected readonly departmentForm = this.formBuilder.group({
    departmentName: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(255)]]
  });

  ngOnInit(): void {
    this.syncModeFromRoute();
    this.loadLookups();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.syncModeFromRoute());

    this.filterForm.valueChanges
      .pipe(
        debounceTime(250),
        distinctUntilChanged((previous, current) => JSON.stringify(previous) === JSON.stringify(current))
      )
      .subscribe(() => {
        this.employeePageNumber = 1;
        this.loadEmployees();
      });
  }

  loadEmployees(): void {
    const { search, departmentId, roleId, status } = this.filterForm.getRawValue();
    this.isLoading = true;
    this.employeeService.getEmployees(search ?? '', departmentId ?? '', roleId ?? '', status ?? '', this.employeePageNumber, this.employeePageSize).subscribe({
      next: (response) => {
        const page = this.normalizePage<Employee>(response, this.employeePageNumber, this.employeePageSize);
        this.employees = page?.items ?? [];
        this.totalEmployees = page?.totalCount ?? 0;
        this.employeePageNumber = page?.pageNumber ?? this.employeePageNumber;
        this.employeePageSize = page?.pageSize ?? this.employeePageSize;
        this.employeeTotalPages = page?.totalPages ?? 0;
        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = this.getApiError(error, 'Unable to load employees.');
      }
    });
  }

  clearFilters(): void {
    this.employeePageNumber = 1;
    this.filterForm.reset({ search: '', departmentId: '', roleId: '', status: '' });
  }

  changeEmployeePage(delta: number): void {
    const nextPage = this.employeePageNumber + delta;
    if ((delta < 0 && !this.hasPreviousEmployeePage) || (delta > 0 && !this.hasNextEmployeePage) || nextPage < 1) {
      return;
    }

    this.employeePageNumber = nextPage;
    this.loadEmployees();
  }

  saveEmployee(): void {
    if (!this.canManageOrganization) {
      return;
    }

    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      this.errorMessage = 'Please fill all required employee details before saving.';
      return;
    }

    if (this.departments.length === 0 || this.roles.length === 0) {
      this.errorMessage = 'Departments and roles are still loading. Please try again in a moment.';
      return;
    }

    const request = this.getEmployeeRequest();
    const isEditing = this.editingEmployeeId !== null;
    if (!isEditing && !request.password) {
      this.errorMessage = 'Password is required for new employees.';
      return;
    }

    const save$ = this.editingEmployeeId
      ? this.employeeService.updateEmployee(this.editingEmployeeId, request)
      : this.employeeService.createEmployee(request);

    this.isSaving = true;
    this.errorMessage = '';
    save$.subscribe({
      next: (response) => {
        this.isSaving = false;
        this.message = response.message;
        this.errorMessage = '';
        this.cancelEmployeeEdit();
        this.loadDepartments();
        void this.router.navigateByUrl('/employees');
      },
      error: (error) => {
        this.isSaving = false;
        this.errorMessage = this.getApiError(error, 'Unable to save employee.');
      }
    });
  }

  editEmployee(employee: Employee): void {
    if (!this.canManageOrganization) {
      return;
    }

    void this.router.navigateByUrl(`/employees/edit/${employee.employeeId}`);
  }

  private populateEmployeeForm(employee: Employee): void {
    this.editingEmployeeId = employee.employeeId;
    this.employeeForm.patchValue({
      username: employee.username,
      password: '',
      firstName: employee.firstName,
      lastName: employee.lastName,
      email: employee.email,
      phoneNumber: employee.phoneNumber,
      gender: employee.gender,
      dob: employee.dob.slice(0, 10),
      doj: employee.doj.slice(0, 10),
      departmentId: String(employee.departmentId),
      roleId: String(employee.roleId),
      status: employee.status
    });
  }

  cancelEmployeeEdit(): void {
    this.editingEmployeeId = null;
    this.employeeForm.reset({
      username: '',
      password: '',
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      gender: 1,
      dob: '2000-01-01',
      doj: this.today(),
      departmentId: this.departments[0] ? String(this.departments[0].departmentId) : '',
      roleId: String(this.roles.find((role) => role.roleName === 'Employee')?.roleId ?? ''),
      status: 1
    });
  }

  deleteEmployee(employee: Employee): void {
    if (!this.canManageOrganization) {
      return;
    }

    this.employeeService.deleteEmployee(employee.employeeId).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadEmployees();
        this.loadDepartments();
      },
      error: (error) => {
        this.errorMessage = this.getApiError(error, 'Unable to delete employee.');
      }
    });
  }

  assignDepartment(employee: Employee, departmentId: string): void {
    if (!this.canManageOrganization) {
      return;
    }

    this.employeeService.assignDepartment(employee.employeeId, Number(departmentId)).subscribe({
      next: () => this.loadEmployees(),
      error: (error) => this.errorMessage = this.getApiError(error, 'Unable to assign department.')
    });
  }

  assignRole(employee: Employee, roleId: string): void {
    if (!this.canManageOrganization) {
      return;
    }

    this.employeeService.assignRole(employee.employeeId, Number(roleId)).subscribe({
      next: () => this.loadEmployees(),
      error: (error) => this.errorMessage = this.getApiError(error, 'Unable to assign role.')
    });
  }

  saveDepartment(): void {
    if (!this.canManageOrganization) {
      return;
    }

    if (this.departmentForm.invalid) {
      this.departmentForm.markAllAsTouched();
      return;
    }

    const request = this.departmentForm.getRawValue();
    const save$ = this.editingDepartmentId
      ? this.employeeService.updateDepartment(this.editingDepartmentId, {
          departmentName: request.departmentName ?? '',
          description: request.description ?? ''
        })
      : this.employeeService.createDepartment({
          departmentName: request.departmentName ?? '',
          description: request.description ?? ''
        });

    save$.subscribe({
      next: (response) => {
        this.message = response.message;
        this.cancelDepartmentEdit();
        this.loadDepartments();
      },
      error: (error) => {
        this.errorMessage = this.getApiError(error, 'Unable to save department.');
      }
    });
  }

  editDepartment(department: Department): void {
    if (!this.canManageOrganization) {
      return;
    }

    this.editingDepartmentId = department.departmentId;
    this.departmentForm.patchValue({
      departmentName: department.departmentName,
      description: department.description ?? ''
    });
  }

  cancelDepartmentEdit(): void {
    this.editingDepartmentId = null;
    this.departmentForm.reset({ departmentName: '', description: '' });
  }

  deleteDepartment(department: Department): void {
    if (!this.canManageOrganization) {
      return;
    }

    this.employeeService.deleteDepartment(department.departmentId).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadDepartments();
      },
      error: (error) => {
        this.errorMessage = this.getApiError(error, 'Unable to delete department.');
      }
    });
  }

  private loadLookups(): void {
    this.loadDepartments();
    this.employeeService.getRoles().subscribe({
      next: (response) => {
        this.roles = response.data ?? [];
        this.applyLookupDefaults();
      },
      error: (error) => {
        this.errorMessage = this.getApiError(error, 'Unable to load roles.');
      }
    });
  }

  private syncModeFromRoute(): void {
    const url = this.router.url.split('?')[0];
    this.message = '';
    this.errorMessage = '';

    if (url === '/employees/add') {
      this.pageMode = 'add';
      this.cancelEmployeeEdit();
      return;
    }

    if (url.startsWith('/employees/edit/')) {
      this.pageMode = 'edit';
      const employeeId = Number(this.route.snapshot.paramMap.get('id'));
      if (Number.isFinite(employeeId) && employeeId > 0) {
        this.loadEmployeeForEdit(employeeId);
      }
      return;
    }

    this.pageMode = 'list';
    this.editingEmployeeId = null;
    this.loadEmployees();
  }

  private loadEmployeeForEdit(employeeId: number): void {
    this.isLoading = true;
    this.employeeService.getEmployee(employeeId).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.populateEmployeeForm(response.data);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = this.getApiError(error, 'Unable to load employee.');
      }
    });
  }

  private loadDepartments(): void {
    this.employeeService.getDepartments().subscribe({
      next: (response) => {
        this.departments = response.data ?? [];
        this.applyLookupDefaults();
      },
      error: (error) => {
        this.errorMessage = this.getApiError(error, 'Unable to load departments.');
      }
    });
  }

  private applyLookupDefaults(): void {
    if (this.editingEmployeeId !== null || this.employeeForm.dirty) {
      return;
    }

    const currentDepartmentId = Number(this.employeeForm.controls.departmentId.value);
    const currentRoleId = Number(this.employeeForm.controls.roleId.value);
    const fallbackDepartmentId = this.departments[0]?.departmentId;
    const fallbackRoleId = this.roles.find((role) => role.roleName === 'Employee')?.roleId;

    this.employeeForm.patchValue({
      departmentId: String(currentDepartmentId || fallbackDepartmentId || ''),
      roleId: String(currentRoleId || fallbackRoleId || '')
    });
  }

  protected isFieldInvalid(fieldName: keyof typeof this.employeeForm.controls): boolean {
    const control = this.employeeForm.controls[fieldName];
    return control.invalid && (control.dirty || control.touched);
  }

  protected fieldError(fieldName: keyof typeof this.employeeForm.controls): string {
    const control = this.employeeForm.controls[fieldName];
    if (control.hasError('required')) {
      return 'Required';
    }

    if (control.hasError('email')) {
      return 'Enter a valid email';
    }

    if (control.hasError('minlength')) {
      return 'Minimum 8 characters';
    }

    if (control.hasError('maxlength')) {
      return 'Too long';
    }

    return 'Invalid value';
  }

  private today(): string {
    return new Date().toISOString().slice(0, 10);
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

  private getEmployeeRequest(): EmployeeRequest {
    const value = this.employeeForm.getRawValue();
    return {
      username: value.username ?? '',
      password: value.password ?? undefined,
      firstName: value.firstName ?? '',
      lastName: value.lastName ?? '',
      email: value.email ?? '',
      phoneNumber: value.phoneNumber ?? '',
      gender: Number(value.gender),
      dob: value.dob ?? '',
      doj: value.doj ?? '',
      departmentId: Number(value.departmentId),
      roleId: Number(value.roleId),
      status: Number(value.status)
    };
  }

  private normalizePage<T>(response: unknown, fallbackPageNumber: number, fallbackPageSize: number): { items: T[]; totalCount: number; pageNumber: number; pageSize: number; totalPages: number } | null {
    return normalizePagedResponse<T>(response, fallbackPageNumber, fallbackPageSize);
  }
}
