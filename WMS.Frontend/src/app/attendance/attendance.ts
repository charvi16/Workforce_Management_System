import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../auth/auth.service';
import { AttendanceEmployee, AttendanceRecord, AttendanceService } from './attendance.service';

@Component({
  selector: 'app-attendance',
  imports: [ReactiveFormsModule],
  templateUrl: './attendance.html',
  styleUrl: './attendance.scss'
})
export class Attendance implements OnInit {
  private readonly attendanceService = inject(AttendanceService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);

  protected employees: AttendanceEmployee[] = [];
  protected records: AttendanceRecord[] = [];
  protected attendancePageNumber = 1;
  protected attendancePageSize = 10;
  protected attendanceTotalPages = 0;
  protected attendanceTotalCount = 0;
  protected message = '';
  protected errorMessage = '';
  protected isLoading = false;
  protected isCheckingIn = false;
  protected isCheckingOut = false;
  protected readonly currentUser = this.authService.getCurrentUser();
  protected readonly isEmployee = this.currentUser?.role === 'Employee';
  protected readonly canViewScopedAttendance = this.currentUser?.role === 'Manager' || this.currentUser?.role === 'Admin';
  protected currentEmployee?: AttendanceEmployee;

  protected readonly workModes = [
    { id: 1, name: 'WFO' },
    { id: 2, name: 'WFH' },
    { id: 3, name: 'Hybrid' }
  ];

  protected readonly attendanceForm = this.formBuilder.group({
    employeeId: [0, [Validators.required, Validators.min(1)]],
    workMode: [1, [Validators.required]]
  });

  protected readonly monthForm = this.formBuilder.group({
    employeeId: [0, [Validators.required]],
    month: [new Date().getMonth() + 1, [Validators.required, Validators.min(1), Validators.max(12)]],
    year: [new Date().getFullYear(), [Validators.required, Validators.min(2020)]]
  });

  protected get monthlyTotalHours(): string {
    const total = this.records.reduce((sum, record) => sum + (record.totalHours ?? 0), 0);
    return total.toFixed(2);
  }

  protected get attendanceScopeLabel(): string {
    if (this.currentUser?.role === 'Admin') {
      return 'View your attendance or any employee attendance.';
    }

    if (this.currentUser?.role === 'Manager') {
      return 'View your attendance or team attendance.';
    }

    return 'View your attendance.';
  }

  ngOnInit(): void {
    this.loadEmployees();
  }

  checkIn(): void {
    if (this.attendanceForm.invalid) {
      this.attendanceForm.markAllAsTouched();
      return;
    }

    const value = this.attendanceForm.getRawValue();
    this.isCheckingIn = true;
    this.message = '';
    this.errorMessage = '';
    this.attendanceService.checkIn(Number(value.employeeId), Number(value.workMode)).subscribe({
      next: (response) => {
        this.isCheckingIn = false;
        this.message = response.message;
        this.errorMessage = '';
        this.refreshAttendanceRecords(response.data);
      },
      error: (error) => {
        this.isCheckingIn = false;
        this.errorMessage = this.getApiError(error, 'Unable to check in.');
        this.message = '';
      }
    });
  }

  checkOut(): void {
    const employeeId = Number(this.attendanceForm.getRawValue().employeeId);
    if (!employeeId) {
      this.attendanceForm.markAllAsTouched();
      return;
    }

    this.isCheckingOut = true;
    this.message = '';
    this.errorMessage = '';
    this.attendanceService.checkOut(employeeId).subscribe({
      next: (response) => {
        this.isCheckingOut = false;
        this.message = response.message;
        this.errorMessage = '';
        this.refreshAttendanceRecords(response.data);
      },
      error: (error) => {
        this.isCheckingOut = false;
        this.errorMessage = this.getApiError(error, 'Unable to check out.');
        this.message = '';
      }
    });
  }

  loadMonthlyAttendance(): void {
    if (this.monthForm.invalid) {
      this.monthForm.markAllAsTouched();
      return;
    }

    const value = this.monthForm.getRawValue();
    this.isLoading = true;
    this.attendanceService
      .getMonthlyAttendance(Number(value.employeeId), Number(value.month), Number(value.year), this.attendancePageNumber, this.attendancePageSize)
      .subscribe({
        next: (response) => {
          const page = response.data;
          this.records = page?.items ?? [];
          this.attendancePageNumber = page?.pageNumber ?? this.attendancePageNumber;
          this.attendancePageSize = page?.pageSize ?? this.attendancePageSize;
          this.attendanceTotalPages = page?.totalPages ?? 0;
          this.attendanceTotalCount = page?.totalCount ?? 0;
          this.isLoading = false;
          this.errorMessage = '';
        },
        error: (error) => {
          this.records = [];
          this.isLoading = false;
          this.errorMessage = this.getApiError(error, 'Unable to load attendance.');
        }
      });
  }

  viewMonthlyAttendance(): void {
    this.attendancePageNumber = 1;
    this.loadMonthlyAttendance();
  }

  changeAttendancePage(delta: number): void {
    const nextPage = this.attendancePageNumber + delta;
    if (nextPage < 1 || (this.attendanceTotalPages > 0 && nextPage > this.attendanceTotalPages)) {
      return;
    }

    this.attendancePageNumber = nextPage;
    this.loadMonthlyAttendance();
  }

  protected formatDate(value: string): string {
    return new Date(value).toLocaleDateString();
  }

  protected formatTime(value?: string): string {
    return value ? new Date(value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '--';
  }

  private loadEmployees(): void {
    this.attendanceService.getEmployees().subscribe({
      next: (response) => {
        this.employees = response.data ?? [];
        this.currentEmployee = this.employees.find((employee) => employee.employeeId === this.currentUser?.employeeId)
          ?? this.employees.find((employee) => employee.email === this.currentUser?.username);
        const currentEmployeeId = this.currentEmployee?.employeeId ?? 0;
        const firstEmployeeId = this.employees[0]?.employeeId ?? 0;
        this.attendanceForm.patchValue({ employeeId: currentEmployeeId });
        this.monthForm.patchValue({ employeeId: currentEmployeeId || firstEmployeeId });
        this.attendanceForm.controls.employeeId.disable();

        if (!this.canViewScopedAttendance) {
          this.monthForm.controls.employeeId.disable();
        }

        if (currentEmployeeId || firstEmployeeId) {
          this.loadMonthlyAttendance();
        } else {
          this.errorMessage = this.isEmployee
            ? 'No employee profile is linked to this login. Use the employee email as the username or create the matching employee profile.'
            : 'No employees are available for attendance.';
        }
      },
      error: (error) => {
        this.errorMessage = this.getApiError(error, 'Unable to load employees.');
      }
    });
  }

  private upsertAttendanceRecord(record: AttendanceRecord): void {
    if (!record) {
      return;
    }

    const selectedMonth = Number(this.monthForm.getRawValue().month);
    const selectedYear = Number(this.monthForm.getRawValue().year);
    const selectedEmployeeId = Number(this.monthForm.getRawValue().employeeId);
    const recordDate = new Date(record.attendanceDate);
    const isSelectedMonth = recordDate.getMonth() + 1 === selectedMonth && recordDate.getFullYear() === selectedYear;
    const isSelectedEmployee = selectedEmployeeId === 0 || selectedEmployeeId === record.employeeId;

    if (!isSelectedMonth || !isSelectedEmployee) {
      return;
    }

    const existingIndex = this.records.findIndex((item) => item.attendanceId === record.attendanceId);
    if (existingIndex >= 0) {
      this.records = this.records.map((item, index) => index === existingIndex ? record : item);
      return;
    }

    this.records = [record, ...this.records].slice(0, this.attendancePageSize);
    this.attendanceTotalCount += 1;
    this.attendanceTotalPages = Math.max(1, Math.ceil(this.attendanceTotalCount / this.attendancePageSize));
  }

  private refreshAttendanceRecords(record: AttendanceRecord): void {
    if (!record) {
      return;
    }

    const recordDate = new Date(record.attendanceDate);
    const recordMonth = recordDate.getMonth() + 1;
    const recordYear = recordDate.getFullYear();
    const currentEmployeeFilter = Number(this.monthForm.getRawValue().employeeId);

    this.monthForm.patchValue({
      employeeId: this.canViewScopedAttendance && currentEmployeeFilter === 0 ? 0 : record.employeeId,
      month: recordMonth,
      year: recordYear
    });

    this.attendancePageNumber = 1;
    this.upsertAttendanceRecord(record);
    this.loadMonthlyAttendance();
  }

  private getApiError(error: unknown, fallback: string): string {
    const response = error as {
      status?: number;
      error?: { errors?: unknown; Errors?: unknown; message?: string; Message?: string; title?: string; Title?: string };
      message?: string;
    };

    if (response.status === 0) {
      return 'Cannot reach the backend API. Make sure the backend is running.';
    }

    if (response.status === 401) {
      return 'Your login session is missing or expired. Login again.';
    }

    if (response.status === 403) {
      return 'You do not have permission to perform this attendance action.';
    }

    const errors = response.error?.errors ?? response.error?.Errors;
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

    return response.error?.message
      ?? response.error?.Message
      ?? response.error?.title
      ?? response.error?.Title
      ?? response.message
      ?? fallback;
  }
}
