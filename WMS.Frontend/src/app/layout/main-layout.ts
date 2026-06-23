import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../auth/auth.service';

interface NavItem {
  label: string;
  route: string;
  roles: string[];
}

@Component({
  selector: 'app-main-layout',
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss'
})
export class MainLayout {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly currentUser = this.authService.getCurrentUser();
  protected readonly role = this.normalizeRole(this.currentUser?.role ?? 'Employee');

  private readonly allNavItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', roles: ['Admin', 'Manager', 'Employee'] },
    { label: 'Employees', route: '/employees', roles: ['Admin', 'Manager'] },
    { label: 'Departments', route: '/departments', roles: ['Admin'] },
    { label: 'Attendance', route: '/attendance', roles: ['Admin', 'Manager', 'Employee'] },
    { label: 'Leaves', route: '/leaves', roles: ['Admin', 'Manager', 'Employee'] },
    { label: 'Clients', route: '/clients', roles: ['Admin', 'Manager', 'Employee'] },
    { label: 'Projects', route: '/projects', roles: ['Admin', 'Manager', 'Employee'] },
    { label: 'Reports', route: '/reports', roles: ['Admin', 'Manager'] },
    { label: 'Announcements', route: '/announcements', roles: ['Admin', 'Manager', 'Employee'] },
    { label: 'Audit Logs', route: '/audit-logs', roles: ['Admin'] }
  ];

  protected get navItems(): NavItem[] {
    return this.allNavItems.filter((item) => item.roles.includes(this.role));
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('/login');
  }

  private normalizeRole(role: string): string {
    const normalized = role.trim().toLowerCase();
    if (normalized === 'admin') {
      return 'Admin';
    }
    if (normalized === 'manager') {
      return 'Manager';
    }
    return 'Employee';
  }
}
