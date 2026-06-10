import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService, CurrentUser } from '../auth/auth.service';
import { EmployeeManagement } from '../employees/employee-management';

interface SummaryCard {
  label: string;
  value: string;
  detail: string;
}

type DashboardSection = 'dashboard' | 'employees' | 'attendance' | 'leaves' | 'projects' | 'reports';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, EmployeeManagement],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user: CurrentUser = this.authService.getCurrentUser() ?? {
    userId: 0,
    username: 'Guest',
    role: 'Employee',
    expiresAtUtc: ''
  };

  protected readonly role = this.user.role;
  protected selectedSection: DashboardSection = 'dashboard';
  protected readonly navItems: { id: DashboardSection; label: string }[] = [
    { id: 'dashboard', label: 'Dashboard' },
    { id: 'employees', label: 'Employees' },
    { id: 'attendance', label: 'Attendance' },
    { id: 'leaves', label: 'Leaves' },
    { id: 'projects', label: 'Projects' },
    { id: 'reports', label: 'Reports' }
  ];
  protected readonly summaryCards = this.getSummaryCards(this.role);
  protected readonly quickActions = this.getQuickActions(this.role);
  protected readonly attendanceRows = this.role === 'Employee'
    ? [
        { name: 'Today', checkIn: '09:42 AM', status: 'Checked In' },
        { name: 'Yesterday', checkIn: '09:35 AM', status: 'Present' },
        { name: 'Monday', checkIn: '--', status: 'Leave' }
      ]
    : [
        { name: 'Ananya Sharma', checkIn: '09:31 AM', status: 'Present' },
        { name: 'Rohan Mehta', checkIn: '--', status: 'Absent' },
        { name: 'Karan Singh', checkIn: '10:15 AM', status: 'Late' }
      ];

  protected readonly approvalRows = this.role === 'Employee'
    ? [
        { person: 'Sick Leave', type: '10 Jun - 12 Jun', status: 'Pending' },
        { person: 'Casual Leave', type: '02 Jun', status: 'Approved' }
      ]
    : [
        { person: 'Riya Sharma', type: 'Sick', status: 'Pending approval' },
        { person: 'Aman Verma', type: 'Casual', status: 'Pending approval' }
      ];

  protected readonly projectRows = this.role === 'Employee'
    ? [
        { name: 'WMS Portal', client: 'Developer', status: 'Assigned' },
        { name: 'HR Analytics', client: 'Tester', status: 'Assigned' }
      ]
    : [
        { name: 'HR Portal', client: 'Infosys', status: 'Active' },
        { name: 'Payroll System', client: 'TCS', status: 'Active' },
        { name: 'CRM Upgrade', client: 'Wipro', status: 'Completed' }
      ];

  protected readonly announcements = [
    'Office closed on 15 June for maintenance.',
    'Submit monthly timesheets before 28 June.',
    'New HR policy updated.'
  ];

  protected readonly recentActivity = this.role === 'Employee'
    ? ['You checked in at 09:42 AM', 'You applied for Casual Leave', 'Your leave was approved']
    : ['Admin added new employee Riya Sharma', 'Manager approved leave for Aman Verma', 'Project WMS Portal was updated'];

  protected get activeNavLabel(): string {
    return this.navItems.find((item) => item.id === this.selectedSection)?.label ?? 'Dashboard';
  }

  selectSection(section: DashboardSection): void {
    this.selectedSection = section;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('/login');
  }

  protected getSectionCards(section: DashboardSection): SummaryCard[] {
    const sectionData: Record<DashboardSection, SummaryCard[]> = {
      dashboard: this.summaryCards,
      employees: this.role === 'Employee'
        ? [
            { label: 'My Profile', value: 'Active', detail: 'Employee profile and assigned role' },
            { label: 'Department', value: 'Engineering', detail: 'Current department assignment' },
            { label: 'Manager', value: 'Priya Nair', detail: 'Reporting manager' }
          ]
        : [
            { label: 'Employee Directory', value: '248', detail: 'Search, view, and maintain employees' },
            { label: 'New Joiners', value: '7', detail: 'Added this month' },
            { label: 'Open Updates', value: '12', detail: 'Profiles needing review' }
          ],
      attendance: this.role === 'Employee'
        ? [
            { label: 'Today', value: 'Checked In', detail: '09:42 AM, WFH' },
            { label: 'Month Hours', value: '126.5', detail: 'Tracked this month' },
            { label: 'Late Marks', value: '1', detail: 'Current month' }
          ]
        : [
            { label: 'Present Today', value: '211', detail: 'Live attendance count' },
            { label: 'Absent Today', value: '37', detail: 'Leave and no-show combined' },
            { label: 'Late Check-ins', value: '8', detail: 'Needs manager review' }
          ],
      leaves: this.role === 'Employee'
        ? [
            { label: 'Leave Balance', value: '14', detail: 'Available days' },
            { label: 'Pending Requests', value: '1', detail: 'Awaiting approval' },
            { label: 'Approved Leaves', value: '8', detail: 'This year' }
          ]
        : [
            { label: 'Pending Approvals', value: '14', detail: 'Awaiting action' },
            { label: 'Approved Leaves', value: '48', detail: 'This month' },
            { label: 'Rejected Leaves', value: '5', detail: 'This month' }
          ],
      projects: this.role === 'Employee'
        ? [
            { label: 'Assigned Projects', value: '2', detail: 'Current allocations' },
            { label: 'Primary Role', value: 'Developer', detail: 'Project contribution' },
            { label: 'Next Review', value: '15 Jun', detail: 'Allocation checkpoint' }
          ]
        : [
            { label: 'Active Projects', value: '8', detail: 'Currently running' },
            { label: 'Completed Projects', value: '3', detail: 'Closed this quarter' },
            { label: 'Allocated Employees', value: '126', detail: 'Across active projects' }
          ],
      reports: [
        { label: 'Attendance Reports', value: 'Ready', detail: 'Monthly attendance export' },
        { label: 'Leave Reports', value: 'Ready', detail: 'Leave usage and approvals' },
        { label: 'Employee Reports', value: 'Ready', detail: 'Directory and role reports' }
      ]
    };

    return sectionData[section];
  }

  private getSummaryCards(role: string): SummaryCard[] {
    if (role === 'Admin') {
      return [
        { label: 'Total Employees', value: '248', detail: 'Across 6 departments' },
        { label: 'Present Today', value: '211', detail: '82% attendance rate' },
        { label: 'Absent Today', value: '37', detail: 'Includes leave and no-show' },
        { label: 'Pending Leaves', value: '14', detail: 'Awaiting approval' },
        { label: 'Active Projects', value: '8', detail: '126 employees allocated' },
        { label: 'Departments', value: '6', detail: 'Operating units' }
      ];
    }

    if (role === 'Manager') {
      return [
        { label: 'Team Members', value: '32', detail: 'Direct and project reports' },
        { label: 'Present Today', value: '28', detail: '4 exceptions' },
        { label: 'Pending Approvals', value: '6', detail: 'Leave and attendance' },
        { label: 'Active Projects', value: '4', detail: 'Team allocations' },
        { label: 'On Leave Today', value: '3', detail: 'Approved requests' }
      ];
    }

    return [
      { label: 'Attendance Status', value: 'Checked In', detail: '09:42 AM, WFH' },
      { label: 'Hours This Month', value: '126.5', detail: 'Target 160 hrs' },
      { label: 'Leave Balance', value: '14', detail: 'Available days' },
      { label: 'Pending Leaves', value: '1', detail: 'Awaiting approval' },
      { label: 'Assigned Projects', value: '2', detail: 'Current allocations' }
    ];
  }

  private getQuickActions(role: string): string[] {
    if (role === 'Admin') {
      return ['Add Employee', 'Add Department', 'Add Project', 'Create Announcement', 'Generate Report'];
    }

    if (role === 'Manager') {
      return ['Approve Leaves', 'View Team Attendance', 'Assign Project', 'Generate Timesheet'];
    }

    return ['Check In', 'Check Out', 'Apply Leave', 'View Attendance', 'View My Projects'];
  }
}
