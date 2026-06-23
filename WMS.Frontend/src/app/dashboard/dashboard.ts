import { Component, OnInit, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { AuthService, CurrentUser } from '../auth/auth.service';
import { Announcement, AnnouncementsService } from '../announcements/announcements.service';
import { Attendance } from '../attendance/attendance';
import { Departments } from '../departments/departments';
import { EmployeeManagement } from '../employees/employee-management';
import { Leave } from '../leaves/leave';
import { DashboardResponse, DashboardService } from './dashboard.service';
import { filter, finalize, timeout } from 'rxjs';

interface SummaryCard {
  label: string;
  value: string;
  detail: string;
}

interface QuickAction {
  label: string;
  route: string;
}

interface DashboardRow {
  name: string;
  detail: string;
  status: string;
}

interface KpiRow {
  label: string;
  value: string;
}

type DashboardSection = 'dashboard' | 'employees' | 'departments' | 'attendance' | 'leaves' | 'clients' | 'projects' | 'reports' | 'announcements' | 'audit-logs';

@Component({
  selector: 'app-dashboard',
  imports: [Attendance, Departments, EmployeeManagement, Leave],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly announcementsService = inject(AnnouncementsService);
  private readonly dashboardService = inject(DashboardService);
  private readonly router = inject(Router);

  protected readonly user: CurrentUser = this.authService.getCurrentUser() ?? {
    userId: 0,
    employeeId: 0,
    username: 'Guest',
    role: 'Employee',
    expiresAtUtc: ''
  };

  protected readonly role = this.normalizeRole(this.user.role);
  protected selectedSection: DashboardSection = 'dashboard';
  protected dashboardData: DashboardResponse | null = null;
  protected visibleAnnouncements: Announcement[] = [];
  protected announcementError = '';
  protected isDashboardLoading = false;
  protected dashboardError = '';
  protected lastUpdatedAt = '';
  protected get navItems(): { id: DashboardSection; label: string }[] {
    const items: { id: DashboardSection; label: string }[] = [
      { id: 'dashboard', label: 'Dashboard' },
      { id: 'employees', label: 'Employees' },
      { id: 'attendance', label: 'Attendance' },
      { id: 'leaves', label: 'Leaves' },
      { id: 'clients', label: 'Clients' },
      { id: 'projects', label: 'Projects' },
      { id: 'reports', label: 'Reports' },
      { id: 'announcements', label: 'Announcements' }
    ];

    if (this.role === 'Admin') {
      items.splice(2, 0, { id: 'departments', label: 'Departments' });
      items.push({ id: 'audit-logs', label: 'Audit Logs' });
    }

    return items;
  }
  protected get summaryCards(): SummaryCard[] {
    if (!this.dashboardData) {
      return this.getSummaryCards(this.role);
    }

    const kpis = this.dashboardData.kpis;
    if (this.role === 'Admin') {
      return [
        { label: 'Total Employees', value: String(kpis.totalEmployees), detail: 'All users in the company' },
        { label: 'Total Departments', value: String(kpis.totalDepartments ?? 0), detail: 'Operating units' },
        { label: 'Present Today', value: String(kpis.presentToday), detail: `${kpis.attendanceRate}% attendance rate` },
        { label: 'Absent Today', value: String(kpis.absentToday), detail: 'No attendance recorded today' },
        { label: 'On Leave Today', value: String(kpis.onLeaveToday), detail: 'Approved leave' },
        { label: 'Pending Leaves', value: String(kpis.pendingLeaves), detail: 'Awaiting review' },
        { label: 'Active Projects', value: String(kpis.activeProjects), detail: 'Running projects' },
        { label: 'Total Clients', value: String(kpis.totalClients), detail: 'Active clients' }
      ];
    }

    if (this.role === 'Manager') {
      return [
        { label: 'Total Employees', value: String(kpis.totalEmployees), detail: 'Scoped to your department' },
        { label: 'Total Departments', value: String(kpis.totalDepartments ?? 0), detail: 'Your operating unit' },
        { label: 'Present Today', value: String(kpis.presentToday), detail: `${kpis.attendanceRate}% attendance rate` },
        { label: 'Absent Today', value: String(kpis.absentToday), detail: 'No attendance recorded today' },
        { label: 'On Leave Today', value: String(kpis.onLeaveToday), detail: 'Team leave count' },
        { label: 'Pending Leaves', value: String(kpis.pendingLeaves), detail: 'Needs action' },
        { label: 'Active Projects', value: String(kpis.activeProjects), detail: 'Team projects' }
      ];
    }

    return [
      { label: 'Total Employees', value: String(kpis.totalEmployees), detail: 'Scoped employees' },
      { label: 'Total Departments', value: String(kpis.totalDepartments ?? 0), detail: 'Scoped departments' },
      { label: 'Present Today', value: String(kpis.presentToday), detail: 'Today' },
      { label: 'Absent Today', value: String(kpis.absentToday), detail: 'Today' },
      { label: 'Attendance Rate', value: `${kpis.attendanceRate}%`, detail: 'This month' },
      { label: 'Monthly Hours', value: kpis.averageWorkingHours.toFixed(1), detail: 'Average working hours' },
      { label: 'Pending Leaves', value: String(kpis.pendingLeaves), detail: 'Awaiting approval' },
      { label: 'Assigned Projects', value: String(kpis.activeProjects), detail: 'Active allocations' }
    ];
  }

  protected get quickActions(): QuickAction[] {
    return this.getQuickActions(this.role);
  }

  protected get kpiRows(): KpiRow[] {
    const kpis = this.dashboardData?.kpis;
    if (!kpis) {
      return [];
    }

    return [
      { label: 'Active Employees', value: String(kpis.activeEmployees) },
      { label: 'On Leave Today', value: String(kpis.onLeaveToday) },
      { label: 'Pending Leaves', value: String(kpis.pendingLeaves) },
      { label: 'Active Projects', value: String(kpis.activeProjects) },
      { label: 'Delayed Projects', value: String(kpis.delayedProjects) },
      { label: 'Total Clients', value: String(kpis.totalClients) },
      { label: 'Unallocated Employees', value: String(kpis.unallocatedEmployees) },
      { label: 'Average Working Hours', value: kpis.averageWorkingHours.toFixed(1) },
      { label: 'Late Check-ins Today', value: String(kpis.lateCheckInsToday) },
      { label: 'Attendance Rate', value: `${kpis.attendanceRate}%` }
    ];
  }

  protected get attendanceRows(): DashboardRow[] {
    return this.dashboardData?.todayAttendance?.map((row) => ({
      name: row.name,
      detail: row.detail || '--',
      status: row.status
    })) ?? [];
  }

  protected get approvalRows() {
    if (this.dashboardData?.pendingApprovals?.length) {
      return this.dashboardData.pendingApprovals.map((item) => ({ person: item, type: 'Pending', status: 'Pending' }));
    }

    return this.role === 'Employee'
      ? []
      : [];
  }

  protected get projectRows(): DashboardRow[] {
    return this.dashboardData?.projectRows?.map((row) => ({
      name: row.name,
      detail: row.detail || '--',
      status: row.status
    })) ?? [];
  }

  protected get attendanceOverviewRows(): { label: string; value: number; percent: number; className: string }[] {
    const distribution = this.dashboardData?.attendanceDistribution ?? [];
    const total = distribution.reduce((sum, item) => sum + Number(item.value), 0);
    return [
      { label: 'Present', className: 'present' },
      { label: 'Absent', className: 'absent' },
      { label: 'Leave', className: 'leave' }
    ].map((item) => {
      const value = Number(distribution.find((point) => point.label === item.label)?.value ?? 0);
      return {
        ...item,
        value,
        percent: total === 0 ? 0 : Math.round((value * 100) / total)
      };
    });
  }

  protected get announcements(): string[] {
    if (this.visibleAnnouncements.length) {
      return this.visibleAnnouncements.map((announcement) => `${announcement.title}: ${announcement.message}`);
    }

    return this.dashboardData?.announcements ?? [];
  }

  protected get alerts(): { type: string; message: string }[] {
    return this.dashboardData?.alerts ?? [];
  }

  ngOnInit(): void {
    this.refreshDashboard();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd && event.urlAfterRedirects === '/dashboard'))
      .subscribe(() => this.refreshDashboard());
  }

  protected get recentActivity(): string[] {
    if (this.dashboardData?.recentActivities?.length) {
      return this.dashboardData.recentActivities;
    }

    return [];
  }

  protected get activeNavLabel(): string {
    return this.navItems.find((item) => item.id === this.selectedSection)?.label ?? 'Dashboard';
  }

  selectSection(section: DashboardSection): void {
    if (['clients', 'projects', 'reports', 'announcements', 'audit-logs'].includes(section)) {
      void this.router.navigateByUrl(`/${section}`);
      return;
    }

    this.selectedSection = section;
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('/login');
  }

  private loadDashboard(): void {
    this.isDashboardLoading = true;
    this.dashboardError = '';
    this.dashboardService.getDashboard(this.role as 'Admin' | 'Manager' | 'Employee')
      .pipe(
        timeout(10000),
        finalize(() => {
          this.isDashboardLoading = false;
        })
      )
      .subscribe({
      next: (response) => {
        const dashboard = this.normalizeDashboardResponse(response);
        this.dashboardData = dashboard;
        this.visibleAnnouncements = [];
        this.announcementError = '';
        this.dashboardError = dashboard ? '' : (response.message || 'Dashboard data is not available.');
        this.lastUpdatedAt = dashboard ? new Date().toLocaleTimeString() : this.lastUpdatedAt;
        if (dashboard) {
          this.loadAnnouncements();
        }
      },
      error: (error) => {
        this.redirectIfUnauthorized(error);
        this.dashboardData = null;
        this.dashboardError = this.getDashboardError(error);
      }
    });
  }

  protected refreshDashboard(): void {
    this.loadDashboard();
  }

  private normalizeDashboardResponse(response: unknown): DashboardResponse | null {
    const source = response as { data?: unknown; Data?: unknown };
    const payload = source.data ?? source.Data ?? response;
    const normalized = this.toCamelCaseObject(payload) as Partial<DashboardResponse> | null;

    if (!normalized?.kpis) {
      return null;
    }

    return {
      kpis: {
        totalEmployees: Number(normalized.kpis.totalEmployees ?? 0),
        totalDepartments: Number(normalized.kpis.totalDepartments ?? 0),
        activeEmployees: Number(normalized.kpis.activeEmployees ?? 0),
        presentToday: Number(normalized.kpis.presentToday ?? 0),
        absentToday: Number(normalized.kpis.absentToday ?? 0),
        onLeaveToday: Number(normalized.kpis.onLeaveToday ?? 0),
        attendanceRate: Number(normalized.kpis.attendanceRate ?? 0),
        pendingLeaves: Number(normalized.kpis.pendingLeaves ?? 0),
        activeProjects: Number(normalized.kpis.activeProjects ?? 0),
        delayedProjects: Number(normalized.kpis.delayedProjects ?? 0),
        totalClients: Number(normalized.kpis.totalClients ?? 0),
        unallocatedEmployees: Number(normalized.kpis.unallocatedEmployees ?? 0),
        averageWorkingHours: Number(normalized.kpis.averageWorkingHours ?? 0),
        lateCheckInsToday: Number(normalized.kpis.lateCheckInsToday ?? 0)
      },
      attendanceTrend: normalized.attendanceTrend ?? [],
      attendanceDistribution: normalized.attendanceDistribution ?? [],
      leaveStatistics: normalized.leaveStatistics ?? [],
      projectStatusDistribution: normalized.projectStatusDistribution ?? [],
      departmentEmployeeCount: normalized.departmentEmployeeCount ?? [],
      workModeDistribution: normalized.workModeDistribution ?? [],
      alerts: normalized.alerts ?? [],
      todayAttendance: normalized.todayAttendance ?? [],
      projectRows: normalized.projectRows ?? [],
      pendingApprovals: normalized.pendingApprovals ?? [],
      announcements: normalized.announcements ?? [],
      recentActivities: normalized.recentActivities ?? []
    };
  }

  private toCamelCaseObject(value: unknown): unknown {
    if (Array.isArray(value)) {
      return value.map((item) => this.toCamelCaseObject(item));
    }

    if (!value || typeof value !== 'object') {
      return value;
    }

    return Object.entries(value as Record<string, unknown>).reduce<Record<string, unknown>>((result, [key, item]) => {
      const normalizedKey = key.length ? `${key[0].toLowerCase()}${key.slice(1)}` : key;
      result[normalizedKey] = this.toCamelCaseObject(item);
      return result;
    }, {});
  }

  private loadAnnouncements(): void {
    this.announcementsService.getAllVisibleAnnouncements().subscribe({
      next: (response) => {
        this.visibleAnnouncements = response.data?.items ?? [];
        this.announcementError = '';
      },
      error: (error) => {
        this.visibleAnnouncements = [];
        this.announcementError = this.getAnnouncementsError(error);
      }
    });
  }

  protected getSectionCards(section: DashboardSection): SummaryCard[] {
    const sectionData: Record<DashboardSection, SummaryCard[]> = {
      dashboard: this.summaryCards,
      employees: this.role === 'Employee'
        ? [
            { label: 'My Profile', value: 'Active', detail: 'Employee profile and assigned role' },
            { label: 'Department', value: String(this.dashboardData?.kpis?.totalDepartments ?? 0), detail: 'Current department assignment' },
            { label: 'Assigned Projects', value: String(this.dashboardData?.kpis?.activeProjects ?? 0), detail: 'Current allocations' }
          ]
        : [
            { label: 'Employee Directory', value: String(this.dashboardData?.kpis?.totalEmployees ?? 0), detail: 'Search, view, and maintain employees' },
            { label: 'Active Employees', value: String(this.dashboardData?.kpis?.activeEmployees ?? 0), detail: 'Currently active' },
            { label: 'Departments', value: String(this.dashboardData?.kpis?.totalDepartments ?? 0), detail: 'Operating units' }
          ],
      departments: [
        { label: 'Departments', value: String(this.dashboardData?.kpis?.totalDepartments ?? 0), detail: 'Operating units' },
        { label: 'Employees', value: String(this.dashboardData?.kpis?.totalEmployees ?? 0), detail: 'Assigned to departments' },
        { label: 'Unallocated', value: String(this.dashboardData?.kpis?.unallocatedEmployees ?? 0), detail: 'Not assigned to active projects' }
      ],
      attendance: this.role === 'Employee'
        ? [
            { label: 'Today', value: this.attendanceRows[0]?.status ?? 'Not checked in', detail: this.attendanceRows[0]?.detail ?? 'Today' },
            { label: 'Month Hours', value: String(this.dashboardData?.kpis?.averageWorkingHours ?? 0), detail: 'Average tracked hours' },
            { label: 'Late Marks', value: String(this.dashboardData?.kpis?.lateCheckInsToday ?? 0), detail: 'Today' }
          ]
        : [
            { label: 'Present Today', value: String(this.dashboardData?.kpis?.presentToday ?? 0), detail: 'Live attendance count' },
            { label: 'Absent Today', value: String(this.dashboardData?.kpis?.absentToday ?? 0), detail: 'Leave and no-show combined' },
            { label: 'Late Check-ins', value: String(this.dashboardData?.kpis?.lateCheckInsToday ?? 0), detail: 'Needs manager review' }
          ],
      leaves: this.role === 'Employee'
        ? [
            { label: 'Pending Requests', value: String(this.dashboardData?.kpis?.pendingLeaves ?? 0), detail: 'Awaiting approval' },
            { label: 'On Leave Today', value: String(this.dashboardData?.kpis?.onLeaveToday ?? 0), detail: 'Approved leave' },
            { label: 'Leave Records', value: String(this.dashboardData?.leaveStatistics?.reduce((sum, item) => sum + Number(item.value), 0) ?? 0), detail: 'Scoped requests' }
          ]
        : [
            { label: 'Pending Approvals', value: String(this.dashboardData?.kpis?.pendingLeaves ?? 0), detail: 'Awaiting action' },
            { label: 'On Leave Today', value: String(this.dashboardData?.kpis?.onLeaveToday ?? 0), detail: 'Approved today' },
            { label: 'Leave Records', value: String(this.dashboardData?.leaveStatistics?.reduce((sum, item) => sum + Number(item.value), 0) ?? 0), detail: 'Scoped requests' }
          ],
      clients: this.role === 'Admin'
        ? [
            { label: 'Total Clients', value: String(this.dashboardData?.kpis?.totalClients ?? 0), detail: 'Active accounts' },
            { label: 'Client Projects', value: String(this.dashboardData?.kpis?.activeProjects ?? 0), detail: 'Active project count' },
            { label: 'Delayed Projects', value: String(this.dashboardData?.kpis?.delayedProjects ?? 0), detail: 'Needs review' }
          ]
        : [
            { label: 'Accessible Clients', value: String(this.dashboardData?.kpis?.totalClients ?? 0), detail: 'Assigned project clients' },
            { label: 'Active Projects', value: String(this.dashboardData?.kpis?.activeProjects ?? 0), detail: 'Current work' },
            { label: 'Delayed Projects', value: String(this.dashboardData?.kpis?.delayedProjects ?? 0), detail: 'Needs review' }
          ],
      projects: this.role === 'Employee'
        ? [
            { label: 'Assigned Projects', value: String(this.dashboardData?.kpis?.activeProjects ?? 0), detail: 'Current allocations' },
            { label: 'Delayed Projects', value: String(this.dashboardData?.kpis?.delayedProjects ?? 0), detail: 'Needs attention' },
            { label: 'Project Rows', value: String(this.projectRows.length), detail: 'Shown below' }
          ]
        : [
            { label: 'Active Projects', value: String(this.dashboardData?.kpis?.activeProjects ?? 0), detail: 'Currently running' },
            { label: 'Delayed Projects', value: String(this.dashboardData?.kpis?.delayedProjects ?? 0), detail: 'Needs attention' },
            { label: 'Unallocated Employees', value: String(this.dashboardData?.kpis?.unallocatedEmployees ?? 0), detail: 'Across active employees' }
          ],
      reports: [
        { label: 'Attendance Reports', value: 'Ready', detail: 'Monthly attendance export' },
        { label: 'Leave Reports', value: 'Ready', detail: 'Leave usage and approvals' },
        { label: 'Employee Reports', value: 'Ready', detail: 'Directory and role reports' }
      ],
      announcements: [
        { label: 'Active Notices', value: String(this.dashboardData?.announcements?.length ?? 0), detail: 'Visible announcements' },
        { label: 'Audience', value: this.role, detail: 'Role-filtered notice board' },
        { label: 'Latest', value: this.dashboardData?.announcements?.[0] ? 'Available' : 'None', detail: 'Most recent announcement' }
      ],
      'audit-logs': [
        { label: 'Audit Logs', value: 'Admin', detail: 'System activity history' },
        { label: 'Client Actions', value: 'Tracked', detail: 'Create, update, deactivate' },
        { label: 'Announcement Actions', value: 'Tracked', detail: 'Create, update, deactivate' }
      ]
    };

    return sectionData[section];
  }

  private getSummaryCards(role: string): SummaryCard[] {
    if (role === 'Admin') {
      return [
        { label: 'Total Employees', value: '...', detail: 'Across departments' },
        { label: 'Total Departments', value: '...', detail: 'Operating units' },
        { label: 'Present Today', value: '...', detail: 'Loading attendance' },
        { label: 'Absent Today', value: '...', detail: 'Loading attendance' },
        { label: 'Pending Leaves', value: '...', detail: 'Awaiting approval' },
        { label: 'Active Projects', value: '...', detail: 'Current projects' }
      ];
    }

    if (role === 'Manager') {
      return [
        { label: 'Total Employees', value: '...', detail: 'Direct and project reports' },
        { label: 'Total Departments', value: '...', detail: 'Scoped departments' },
        { label: 'Present Today', value: '...', detail: 'Loading attendance' },
        { label: 'Absent Today', value: '...', detail: 'Loading attendance' },
        { label: 'Pending Approvals', value: '...', detail: 'Leave and attendance' },
        { label: 'Active Projects', value: '...', detail: 'Team allocations' },
        { label: 'On Leave Today', value: '...', detail: 'Approved requests' }
      ];
    }

    return [
      { label: 'Total Employees', value: '...', detail: 'Scoped employees' },
      { label: 'Total Departments', value: '...', detail: 'Scoped departments' },
      { label: 'Present Today', value: '...', detail: 'Today' },
      { label: 'Absent Today', value: '...', detail: 'Today' },
      { label: 'Attendance Status', value: 'Not checked in', detail: 'Today' },
      { label: 'Hours This Month', value: '0', detail: 'Tracked hours' },
      { label: 'Leave Balance', value: '0', detail: 'Available days' },
      { label: 'Pending Leaves', value: '0', detail: 'Awaiting approval' },
      { label: 'Assigned Projects', value: '0', detail: 'Current allocations' }
    ];
  }

  protected openQuickAction(action: QuickAction): void {
    void this.router.navigateByUrl(action.route);
  }

  private getQuickActions(role: string): QuickAction[] {
    if (role === 'Admin') {
      return [
        { label: 'Add Employee', route: '/employees/add' },
        { label: 'View Employees', route: '/employees' },
        { label: 'Add Department', route: '/departments/add' },
        { label: 'View Departments', route: '/departments' },
        { label: 'Add Client', route: '/clients/add' },
        { label: 'View Clients', route: '/clients' },
        { label: 'Add Project', route: '/projects/add' },
        { label: 'View Projects', route: '/projects' },
        { label: 'Reports', route: '/reports' },
        { label: 'Create Announcement', route: '/announcements/add' }
      ];
    }

    if (role === 'Manager') {
      return [
        { label: 'Team Attendance', route: '/attendance' },
        { label: 'Leave Approvals', route: '/leaves' },
        { label: 'Team Projects', route: '/projects' },
        { label: 'Reports', route: '/reports' },
        { label: 'Announcements', route: '/announcements' }
      ];
    }

    return [
      { label: 'Check In / Check Out', route: '/attendance' },
      { label: 'Apply Leave', route: '/leaves' },
      { label: 'My Leave Status', route: '/leaves' },
      { label: 'My Attendance', route: '/attendance' },
      { label: 'My Projects', route: '/projects' },
      { label: 'Announcements', route: '/announcements' }
    ];
  }

  private normalizeRole(role: string): 'Admin' | 'Manager' | 'Employee' {
    const normalized = role.trim().toLowerCase();
    if (normalized === 'admin') {
      return 'Admin';
    }
    if (normalized === 'manager') {
      return 'Manager';
    }
    return 'Employee';
  }

  private getDashboardError(error: unknown): string {
    const response = error as {
      name?: string;
      status?: number;
      error?: { errors?: unknown; message?: string; Message?: string; title?: string; Title?: string };
      message?: string;
    };

    if (response.status === 0) {
      return 'Cannot reach the backend API. Make sure the API is running on http://localhost:5000.';
    }

    if (response.status === 401 || response.status === 403) {
      return 'Your session is not authorized to load dashboard metrics. Login again with the correct role.';
    }

    if (response.name === 'TimeoutError') {
      return 'Dashboard metrics request timed out. Retry after confirming the API is running on http://localhost:5000.';
    }

    const errors = response.error?.errors;
    if (Array.isArray(errors) && errors.length > 0) {
      return String(errors[0]);
    }

    return response.error?.message
      ?? response.error?.Message
      ?? response.error?.title
      ?? response.error?.Title
      ?? response.message
      ?? 'Unable to load dashboard metrics.';
  }

  private getAnnouncementsError(error: unknown): string {
    const response = error as { status?: number };
    if (response.status === 401 || response.status === 403) {
      return 'Login again to refresh announcements.';
    }

    if (response.status === 0) {
      return 'Cannot reach the announcements API.';
    }

    return 'Unable to refresh announcements.';
  }

  private redirectIfUnauthorized(error: unknown): void {
    const response = error as { status?: number };
    if (response.status === 401 || response.status === 403) {
      this.authService.logout();
      void this.router.navigateByUrl('/login');
    }
  }
}
