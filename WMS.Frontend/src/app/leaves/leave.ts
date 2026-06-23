import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../auth/auth.service';
import { LeaveEmployee, LeaveRecord, LeaveService, LeaveStatistics } from './leave.service';

@Component({
  selector: 'app-leave',
  imports: [ReactiveFormsModule],
  templateUrl: './leave.html',
  styleUrl: './leave.scss'
})
export class Leave implements OnInit {
  private readonly leaveService = inject(LeaveService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private leaveRequestId = 0;
  private statisticsRequestId = 0;

  protected employees: LeaveEmployee[] = [];
  protected applyEmployees: LeaveEmployee[] = [];
  protected leaves: LeaveRecord[] = [];
  protected leavePageNumber = 1;
  protected leavePageSize = 10;
  protected leaveTotalPages = 0;
  protected leaveTotalCount = 0;
  protected statistics: LeaveStatistics = {
    totalRequests: 0,
    pendingRequests: 0,
    approvedRequests: 0,
    rejectedRequests: 0,
    cancelledRequests: 0,
    approvedDays: 0
  };
  protected message = '';
  protected errorMessage = '';
  protected isLoading = false;
  protected isStatsLoading = false;
  protected readonly currentUser = this.authService.getCurrentUser();
  protected readonly isEmployee = this.currentUser?.role === 'Employee';
  protected currentEmployee?: LeaveEmployee;
  protected readonly canReview = this.currentUser?.role === 'Admin' || this.currentUser?.role === 'Manager';

  protected get leaveScopeLabel(): string {
    if (this.currentUser?.role === 'Admin') {
      return 'Track your leave and review employee or manager leave requests.';
    }

    if (this.currentUser?.role === 'Manager') {
      return 'Track your leave and review team leave requests.';
    }

    return 'Track your leave requests.';
  }

  protected readonly leaveTypes = [
    { id: 1, name: 'Sick' },
    { id: 2, name: 'Casual' },
    { id: 3, name: 'Earned' },
    { id: 4, name: 'Unpaid' }
  ];

  protected readonly statuses = [
    { id: 0, name: 'All' },
    { id: 1, name: 'Pending' },
    { id: 2, name: 'Approved' },
    { id: 3, name: 'Rejected' },
    { id: 4, name: 'Cancelled' }
  ];

  protected readonly applyForm = this.formBuilder.group({
    employeeId: [0, [Validators.required, Validators.min(1)]],
    leaveType: [1, [Validators.required]],
    fromDate: ['', [Validators.required]],
    toDate: ['', [Validators.required]],
    reason: ['', [Validators.maxLength(255)]]
  });

  protected readonly filterForm = this.formBuilder.group({
    employeeId: [0],
    status: [0],
    fromDate: [''],
    toDate: [''],
    year: [new Date().getFullYear(), [Validators.min(2020)]]
  });

  ngOnInit(): void {
    this.loadEmployees();
  }

  applyLeave(): void {
    if (this.applyForm.invalid) {
      this.applyForm.markAllAsTouched();
      return;
    }

    const value = this.applyForm.getRawValue();
    this.leaveService
      .applyLeave(
        Number(value.employeeId),
        Number(value.leaveType),
        value.fromDate ?? '',
        value.toDate ?? '',
        value.reason?.trim() || undefined
      )
      .subscribe({
        next: (response) => {
          this.message = response.message;
          this.errorMessage = '';
          this.applyForm.patchValue({ leaveType: 1, fromDate: '', toDate: '', reason: '' });
          this.loadLeaves();
        },
        error: (error) => {
          this.errorMessage = error.error?.errors?.[0] ?? 'Unable to apply leave.';
          this.message = '';
        }
      });
  }

  loadLeaves(): void {
    const leaveRequestId = ++this.leaveRequestId;
    const statisticsRequestId = ++this.statisticsRequestId;
    const value = this.filterForm.getRawValue();
    const employeeId = Number(value.employeeId);
    const status = Number(value.status) || undefined;
    const fromDate = value.fromDate || undefined;
    const toDate = value.toDate || undefined;
    const year = Number(value.year) || undefined;

    this.isLoading = true;
    this.leaveService.getLeaves(employeeId, status, fromDate, toDate, this.leavePageNumber, this.leavePageSize).subscribe({
      next: (response) => {
        if (leaveRequestId !== this.leaveRequestId) {
          return;
        }

        const page = response.data;
        this.leaves = page?.items ?? [];
        this.leavePageNumber = page?.pageNumber ?? this.leavePageNumber;
        this.leavePageSize = page?.pageSize ?? this.leavePageSize;
        this.leaveTotalPages = page?.totalPages ?? 0;
        this.leaveTotalCount = page?.totalCount ?? 0;
        this.isLoading = false;
        this.errorMessage = '';
      },
      error: (error) => {
        if (leaveRequestId !== this.leaveRequestId) {
          return;
        }

        this.leaves = [];
        this.isLoading = false;
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to load leave requests.';
      }
    });

    this.isStatsLoading = true;
    this.leaveService.getStatistics(employeeId, year).subscribe({
      next: (response) => {
        if (statisticsRequestId !== this.statisticsRequestId) {
          return;
        }

        this.statistics = response.data ?? this.statistics;
        this.isStatsLoading = false;
      },
      error: () => {
        if (statisticsRequestId !== this.statisticsRequestId) {
          return;
        }

        this.statistics = {
          totalRequests: 0,
          pendingRequests: 0,
          approvedRequests: 0,
          rejectedRequests: 0,
          cancelledRequests: 0,
          approvedDays: 0
        };
        this.isStatsLoading = false;
      }
    });
  }

  viewLeaves(): void {
    this.leavePageNumber = 1;
    this.loadLeaves();
  }

  changeLeavePage(delta: number): void {
    const nextPage = this.leavePageNumber + delta;
    if (nextPage < 1 || (this.leaveTotalPages > 0 && nextPage > this.leaveTotalPages)) {
      return;
    }

    this.leavePageNumber = nextPage;
    this.loadLeaves();
  }

  cancelLeave(leaveId: number): void {
    this.leaveService.cancelLeave(leaveId).subscribe({
      next: (response) => {
        this.message = response.message;
        this.errorMessage = '';
        this.loadLeaves();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to cancel leave.';
        this.message = '';
      }
    });
  }

  reviewLeave(leaveId: number, isApproved: boolean): void {
    this.leaveService.reviewLeave(leaveId, isApproved).subscribe({
      next: (response) => {
        this.message = response.message;
        this.errorMessage = '';
        this.loadLeaves();
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to review leave.';
        this.message = '';
      }
    });
  }

  protected canCancel(leave: LeaveRecord): boolean {
    return leave.employeeId === this.currentEmployee?.employeeId && (leave.status === 1 || leave.status === 2);
  }

  protected canReviewLeave(leave: LeaveRecord): boolean {
    if (!this.canReview || leave.status !== 1 || leave.employeeId === this.currentEmployee?.employeeId) {
      return false;
    }

    const applicantRole = this.getEmployeeRoleName(leave.employeeId);
    if (this.currentUser?.role === 'Manager') {
      return applicantRole === 'Employee';
    }

    return applicantRole === 'Employee' || applicantRole === 'Manager';
  }

  protected formatDate(value: string): string {
    return new Date(value).toLocaleDateString();
  }

  private loadEmployees(): void {
    this.leaveService.getEmployees().subscribe({
      next: (response) => {
        this.employees = response.data ?? [];
        this.currentEmployee = this.employees.find((employee) => employee.employeeId === this.currentUser?.employeeId)
          ?? this.employees.find((employee) => employee.email === this.currentUser?.username);
        this.applyEmployees = this.currentEmployee ? [this.currentEmployee] : [];
        const applyEmployeeId = this.currentEmployee?.employeeId ?? 0;
        const firstEmployeeId = this.employees[0]?.employeeId ?? 0;
        const filterEmployeeId = this.canReview ? 0 : applyEmployeeId || firstEmployeeId;
        this.applyForm.patchValue({ employeeId: applyEmployeeId });
        this.applyForm.controls.employeeId.disable();
        this.filterForm.patchValue({ employeeId: filterEmployeeId });

        if (!this.canReview) {
          this.filterForm.controls.employeeId.disable();
        }

        if (applyEmployeeId || firstEmployeeId || !this.isEmployee) {
          this.loadLeaves();
        } else {
          this.errorMessage = 'No employee profile is linked to this login. Use the employee email as the username or create the matching employee profile.';
        }
      },
      error: (error) => {
        this.errorMessage = error.error?.errors?.[0] ?? 'Unable to load employees.';
      }
    });
  }

  private getEmployeeRoleName(employeeId: number): string {
    return this.employees.find((employee) => employee.employeeId === employeeId)?.roleName ?? '';
  }
}
