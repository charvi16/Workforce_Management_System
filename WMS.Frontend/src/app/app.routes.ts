import { Routes } from '@angular/router';
import { Dashboard } from './dashboard/dashboard';
import { Login } from './auth/login/login';
import { Home } from './home/home';
import { Attendance } from './attendance/attendance';
import { Leave } from './leaves/leave';
import { Clients } from './clients/clients';
import { Projects } from './projects/projects';
import { Reports } from './reports/reports';
import { Announcements } from './announcements/announcements';
import { AuditLogs } from './audit-logs/audit-logs';
import { EmployeeManagement } from './employees/employee-management';
import { MainLayout } from './layout/main-layout';
import { Departments } from './departments/departments';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'login', component: Login },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'employees', component: EmployeeManagement },
      { path: 'employees/add', component: EmployeeManagement },
      { path: 'employees/edit/:id', component: EmployeeManagement },
      { path: 'employees/:id', component: EmployeeManagement },
      { path: 'attendance', component: Attendance },
      { path: 'attendance/monthly', component: Attendance },
      { path: 'attendance/team', component: Attendance },
      { path: 'leaves', component: Leave },
      { path: 'leaves/apply', component: Leave },
      { path: 'leaves/status', component: Leave },
      { path: 'leaves/approvals', component: Leave },
      { path: 'clients', component: Clients },
      { path: 'clients/add', component: Clients },
      { path: 'clients/edit/:id', component: Clients },
      { path: 'clients/:id', component: Clients },
      { path: 'departments', component: Departments },
      { path: 'departments/add', component: Departments },
      { path: 'departments/edit/:id', component: Departments },
      { path: 'departments/:id', component: Departments },
      { path: 'projects', component: Projects },
      { path: 'projects/my', component: Projects },
      { path: 'projects/add', component: Projects },
      { path: 'projects/edit/:id', component: Projects },
      { path: 'projects/:id', component: Projects },
      { path: 'reports', component: Reports },
      { path: 'reports/add', component: Reports },
      { path: 'reports/generate', component: Reports },
      { path: 'reports/:id', component: Reports },
      { path: 'announcements', component: Announcements },
      { path: 'announcements/add', component: Announcements },
      { path: 'announcements/edit/:id', component: Announcements },
      { path: 'announcements/:id', component: Announcements },
      { path: 'audit-logs', component: AuditLogs }
    ]
  },
  { path: '**', redirectTo: '' }
];
