import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import {
  Department,
  Employee,
  EmployeeManagementService,
  EmployeeRequest,
  Role
} from './employee-management.service';

@Component({
  selector: 'app-employee-management',
  imports: [ReactiveFormsModule],
  templateUrl: './employee-management.html',
  styleUrl: './employee-management.scss'
})
export class EmployeeManagement implements OnInit {
  private readonly employeeService = inject(EmployeeManagementService);
  private readonly formBuilder = inject(FormBuilder);

  protected employees: Employee[] = [];
  protected departments: Department[] = [];
  protected roles: Role[] = [];
  protected editingEmployeeId: number | null = null;
  protected editingDepartmentId: number | null = null;
  protected isLoading = false;
  protected message = '';
  protected errorMessage = '';

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
    roleId: ['']
  });

  protected readonly employeeForm = this.formBuilder.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(80)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(15)]],
    gender: [1, [Validators.required]],
    dob: ['', [Validators.required]],
    doj: ['', [Validators.required]],
    departmentId: [1, [Validators.required]],
    roleId: [3, [Validators.required]],
    status: [1, [Validators.required]]
  });

  protected readonly departmentForm = this.formBuilder.group({
    departmentName: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(255)]]
  });

  ngOnInit(): void {
    this.loadLookups();
    this.loadEmployees();
    this.filterForm.valueChanges
      .pipe(
        debounceTime(250),
        distinctUntilChanged((previous, current) => JSON.stringify(previous) === JSON.stringify(current))
      )
      .subscribe(() => this.loadEmployees());
  }

  loadEmployees(): void {
    const { search, departmentId, roleId } = this.filterForm.getRawValue();
    this.isLoading = true;
    this.employeeService.getEmployees(search ?? '', departmentId ?? '', roleId ?? '').subscribe({
      next: (response) => {
        this.employees = response.data ?? [];
        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to load employees.';
      }
    });
  }

  clearFilters(): void {
    this.filterForm.reset({ search: '', departmentId: '', roleId: '' });
    this.loadEmployees();
  }

  saveEmployee(): void {
    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      return;
    }

    const request = this.getEmployeeRequest();
    const isEditing = this.editingEmployeeId !== null;
    const save$ = this.editingEmployeeId
      ? this.employeeService.updateEmployee(this.editingEmployeeId, request)
      : this.employeeService.createEmployee(request);

    save$.subscribe({
      next: (response) => {
        this.message = response.message;
        this.errorMessage = '';
        this.cancelEmployeeEdit();
        if (!isEditing) {
          this.filterForm.reset({ search: '', departmentId: '', roleId: '' });
        }
        this.loadEmployees();
        this.loadDepartments();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to save employee.';
      }
    });
  }

  editEmployee(employee: Employee): void {
    this.editingEmployeeId = employee.employeeId;
    this.employeeForm.patchValue({
      firstName: employee.firstName,
      lastName: employee.lastName,
      email: employee.email,
      phoneNumber: employee.phoneNumber,
      gender: employee.gender,
      dob: employee.dob.slice(0, 10),
      doj: employee.doj.slice(0, 10),
      departmentId: employee.departmentId,
      roleId: employee.roleId,
      status: employee.status
    });
  }

  cancelEmployeeEdit(): void {
    this.editingEmployeeId = null;
    this.employeeForm.reset({
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      gender: 1,
      dob: '',
      doj: '',
      departmentId: this.departments[0]?.departmentId ?? 1,
      roleId: this.roles.find((role) => role.roleName === 'Employee')?.roleId ?? 3,
      status: 1
    });
  }

  deleteEmployee(employee: Employee): void {
    this.employeeService.deleteEmployee(employee.employeeId).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadEmployees();
        this.loadDepartments();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to delete employee.';
      }
    });
  }

  assignDepartment(employee: Employee, departmentId: string): void {
    this.employeeService.assignDepartment(employee.employeeId, Number(departmentId)).subscribe({
      next: () => this.loadEmployees(),
      error: (error) => this.errorMessage = error.error?.errors?.[0] ?? 'Unable to assign department.'
    });
  }

  assignRole(employee: Employee, roleId: string): void {
    this.employeeService.assignRole(employee.employeeId, Number(roleId)).subscribe({
      next: () => this.loadEmployees(),
      error: (error) => this.errorMessage = error.error?.errors?.[0] ?? 'Unable to assign role.'
    });
  }

  saveDepartment(): void {
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
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to save department.';
      }
    });
  }

  editDepartment(department: Department): void {
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
    this.employeeService.deleteDepartment(department.departmentId).subscribe({
      next: (response) => {
        this.message = response.message;
        this.loadDepartments();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to delete department.';
      }
    });
  }

  private loadLookups(): void {
    this.loadDepartments();
    this.employeeService.getRoles().subscribe({
      next: (response) => {
        this.roles = response.data ?? [];
        this.cancelEmployeeEdit();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to load roles.';
      }
    });
  }

  private loadDepartments(): void {
    this.employeeService.getDepartments().subscribe({
      next: (response) => {
        this.departments = response.data ?? [];
        this.cancelEmployeeEdit();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to load departments.';
      }
    });
  }

  private getEmployeeRequest(): EmployeeRequest {
    const value = this.employeeForm.getRawValue();
    return {
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
}
