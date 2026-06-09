import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home {
  protected readonly modules = [
    { name: 'Employees', status: 'Planned', detail: 'Employee master data, search, status, and department mapping.' },
    { name: 'Attendance', status: 'Planned', detail: 'Check-in, check-out, work mode, and monthly attendance.' },
    { name: 'Leaves', status: 'Planned', detail: 'Apply, cancel, approve, reject, and track leave status.' },
    { name: 'Projects', status: 'Planned', detail: 'Clients, projects, and employee allocation tracking.' },
    { name: 'Dashboard', status: 'Planned', detail: 'Role-based summaries and charts for workforce operations.' },
    { name: 'Reports', status: 'Planned', detail: 'Attendance, timesheet, leave, employee, and project reports.' }
  ];
}
