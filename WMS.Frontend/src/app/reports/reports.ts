import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EmployeeManagementService, Department, Employee } from '../employees/employee-management.service';
import { ReportsService } from './reports.service';

@Component({
  selector: 'app-reports',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reports.html',
  styleUrl: './reports.scss'
})
export class Reports implements OnInit {
  private readonly reportsService = inject(ReportsService);
  private readonly employeeService = inject(EmployeeManagementService);
  private readonly formBuilder = inject(FormBuilder);

  protected employees: Employee[] = [];
  protected departments: Department[] = [];
  protected message = '';
  protected errorMessage = '';
  protected isGenerating = false;

  protected readonly reportTypes = [
    { id: 'Attendance', name: 'Attendance Report' },
    { id: 'Timesheet', name: 'Timesheet Report' },
    { id: 'Leave', name: 'Leave Report' },
    { id: 'ProjectAllocation', name: 'Project Allocation Report' }
  ];

  protected readonly form = this.formBuilder.group({
    reportType: ['Attendance', [Validators.required]],
    employeeId: [0],
    departmentId: [0],
    fromDate: ['', [Validators.required]],
    toDate: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.loadLookups();
  }

  generateReport(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.isGenerating = true;
    this.reportsService.generateAttendanceReport({
      reportType: value.reportType ?? 'Attendance',
      employeeId: Number(value.employeeId) || null,
      departmentId: Number(value.departmentId) || null,
      fromDate: value.fromDate ?? '',
      toDate: value.toDate ?? ''
    }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'attendance-report.pdf';
        link.click();
        window.URL.revokeObjectURL(url);
        this.isGenerating = false;
        this.message = 'Report request completed.';
        this.errorMessage = '';
      },
      error: (error) => {
        this.isGenerating = false;
        this.errorMessage = error.error?.message ?? 'Crystal Reports integration is not configured yet.';
      }
    });
  }

  private loadLookups(): void {
    this.employeeService.getEmployees('', '', '', '', 1, 100).subscribe({
      next: (response) => this.employees = response.data?.items ?? [],
      error: () => this.employees = []
    });

    this.employeeService.getDepartments().subscribe({
      next: (response) => this.departments = response.data ?? [],
      error: () => this.departments = []
    });
  }
}
