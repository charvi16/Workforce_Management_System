import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly modules = [
    { name: 'Employees', status: 'Planned', detail: 'Employee master data, search, status, and department mapping.' },
    { name: 'Attendance', status: 'Planned', detail: 'Check-in, check-out, work mode, and monthly attendance.' },
    { name: 'Leaves', status: 'Planned', detail: 'Apply, cancel, approve, reject, and track leave status.' },
    { name: 'Projects', status: 'Planned', detail: 'Clients, projects, and employee allocation tracking.' },
    { name: 'Dashboard', status: 'Planned', detail: 'Role-based summaries and charts for workforce operations.' },
    { name: 'Reports', status: 'Planned', detail: 'Attendance, timesheet, leave, employee, and project reports.' }
  ];
}
