1. Dashboard Layout Design

Basic layout:

---------------------------------------------------------
| Sidebar        | Top Navbar                           |
|                |--------------------------------------|
|                | Welcome Section                      |
|                |--------------------------------------|
|                | Summary Cards                        |
|                |--------------------------------------|
|                | Charts + Today's Attendance          |
|                |--------------------------------------|
|                | Pending Approvals + Announcements    |
---------------------------------------------------------

Angular folder:

frontend/WMS.Frontend/src/app/dashboard/
│
├── dashboard.component.ts
├── dashboard.component.html
├── dashboard.component.scss
├── dashboard.service.ts
└── components/
    ├── summary-card/
    ├── attendance-chart/
    ├── leave-chart/
    ├── project-status-card/
    ├── pending-approvals/
    ├── announcement-widget/
    └── recent-activity/

Summary Cards

These are the most important part of the dashboard.

For Admin Dashboard

Show these cards:

Total Employees
Present Today
Absent Today
Pending Leave Requests
Active Projects
Departments

Example UI:

---------------------------------------------------------
| Total Employees | Present Today | Absent Today        |
| 248             | 211           | 37                  |
---------------------------------------------------------
| Pending Leaves  | Active Projects | Departments       |
| 14              | 8               | 6                 |
---------------------------------------------------------
For Manager Dashboard

Show:

Team Members
Present Today
Pending Approvals
Active Projects
On Leave Today
For Employee Dashboard

Show:

My Attendance Status
Total Hours This Month
Leave Balance
Pending Leave Requests
Assigned Projects

Attendance Overview Chart

Use this to show attendance statistics.

For Admin/Manager:

Present vs Absent vs Leave

Chart type:

Bar Chart or Doughnut Chart

Example:

Attendance Overview - This Month

Present     ██████████████████  82%
Absent      ███                 8%
Leave       █████               10%

Angular library:

npm install chart.js ng2-charts

Dashboard component:

attendance-chart/

Data needed from backend:

{
  "presentCount": 211,
  "absentCount": 24,
  "leaveCount": 13,
  "lateCheckInCount": 8
}

Leave Statistics Chart

This chart shows leave data.

For Admin/Manager:

Pending Leaves
Approved Leaves
Rejected Leaves

For Employee:

Used Leaves
Remaining Leaves
Pending Leaves

Example:

Leave Statistics

Sick Leave     3 used
Casual Leave   2 used
Earned Leave   5 remaining
Pending        1 request

Backend response:

{
  "sickLeaveUsed": 3,
  "casualLeaveUsed": 2,
  "earnedLeaveUsed": 4,
  "pendingLeaves": 1,
  "approvedLeaves": 8,
  "rejectedLeaves": 1
}

Today's Attendance Widget

This is very useful for employees.

For Employee:

Today's Attendance

Status: Checked In
Check-In: 09:42 AM
Check-Out: Not yet
Total Hours: 4.5 hrs
Work Mode: WFH

[Check Out]

Before check-in:

Today's Attendance

Status: Not Checked In
Work Mode: Select WFO / WFH / Hybrid

[Check In]

For Admin/Manager:

Show list:

Today's Attendance

Employee        Check-In     Status
Ananya Sharma   09:31 AM     Present
Rohan Mehta     --           Absent
Karan Singh     10:15 AM     Late

Pending Approvals Section

This is mainly for Manager/Admin.

Show pending leave requests:

Pending Approvals

Employee        Leave Type   Dates             Action
Riya Sharma     Sick         10 Jun - 12 Jun   Approve | Reject
Aman Verma      Casual       14 Jun            Approve | Reject

Backend API:

GET /api/leaves/pending
PUT /api/leaves/{id}/approve
PUT /api/leaves/{id}/reject

For employee, instead show:

My Leave Requests

Leave Type     Dates            Status
Sick           10 Jun - 12 Jun  Pending
Casual         02 Jun           Approved

Project Overview Section

For Admin/Manager:

Project Status

Active Projects: 8
Completed Projects: 3
Employees Allocated: 126
Unallocated Employees: 22

Table:

Project Name        Client        Members     Status
HR Portal           Infosys       12          Active
Payroll System      TCS           8           Active
CRM Upgrade         Wipro         10          Completed

For Employee:

My Projects

Project Name        Role       Assigned On
WMS Portal          Developer  01 Jun 2026
HR Analytics        Tester     15 May 2026

Announcement / Notice Board

This should be visible to all users.

Example:

Announcements

1. Office closed on 15 June for maintenance.
2. Submit monthly timesheets before 28 June.
3. New HR policy updated.

Component:

announcement-widget/

Admin can have:

[Create Announcement]

Employee only sees active announcements.

Backend API:

GET /api/announcements/active
POST /api/announcements
PUT /api/announcements/{id}
DELETE /api/announcements/{id}

Recent Activity / Audit Logs

For Admin:

Recent Activity

- Admin added new employee Riya Sharma
- Manager approved leave for Aman Verma
- Employee Charvi checked in at 09:40 AM
- Project WMS Portal was updated

This can come from AuditLog table.

Backend API:

GET /api/audit-logs/recent

For Employee, show:

My Recent Activity

- You checked in at 09:40 AM
- You applied for Casual Leave
- Your leave was approved

Quick Action Buttons

Add buttons based on role.

Admin Quick Actions
+ Add Employee
+ Add Department
+ Add Project
+ Create Announcement
+ Generate Report
Manager Quick Actions
Approve Leaves
View Team Attendance
Assign Project
Generate Timesheet
Employee Quick Actions
Check In
Check Out
Apply Leave
View Attendance
View My Projects

Design:

-------------------------------------------------
| Quick Actions                                  |
| [Check In] [Apply Leave] [View Attendance]     |
-------------------------------------------------

Final Dashboard Components List

Your dashboard should contain these components:

1. Top Navbar
2. Sidebar
3. Welcome Banner
4. Summary Cards
5. Quick Actions
6. Attendance Overview Chart
7. Leave Statistics Chart
8. Today's Attendance Widget
9. Pending Approvals Widget
10. Project Overview Widget
11. Announcement Widget
12. Recent Activity Widget

Backend Dashboard APIs

Instead of calling 10 different APIs from dashboard, create one dashboard API.

Admin Dashboard API
GET /api/dashboard/admin

Response:

{
  "totalEmployees": 248,
  "presentToday": 211,
  "absentToday": 24,
  "pendingLeaves": 14,
  "activeProjects": 8,
  "departments": 6,
  "attendanceSummary": {
    "present": 211,
    "absent": 24,
    "leave": 13
  },
  "leaveSummary": {
    "pending": 14,
    "approved": 42,
    "rejected": 5
  },
  "recentAnnouncements": [],
  "recentActivities": []
}
Manager Dashboard API
GET /api/dashboard/manager

Response:

{
  "teamMembers": 18,
  "presentToday": 15,
  "onLeaveToday": 2,
  "pendingApprovals": 4,
  "activeProjects": 3,
  "teamAttendance": [],
  "pendingLeaves": [],
  "projectSummary": []
}
Employee Dashboard API
GET /api/dashboard/employee

Response:

{
  "todayStatus": "CheckedIn",
  "checkIn": "09:42 AM",
  "checkOut": null,
  "monthlyHours": 126.5,
  "leaveBalance": 8,
  "pendingLeaves": 1,
  "assignedProjects": 2,
  "announcements": []
}

Best Dashboard Page Design

Your final dashboard should look like this:

Dashboard
├── Welcome Banner
├── Summary Cards
│   ├── Total Employees
│   ├── Present Today
│   ├── Pending Leaves
│   └── Active Projects
├── Quick Actions
│   ├── Add Employee
│   ├── Mark Attendance
│   ├── Apply Leave
│   └── Generate Report
├── Analytics
│   ├── Attendance Overview Chart
│   └── Leave Statistics Chart
├── Management Widgets
│   ├── Pending Leave Approvals
│   ├── Project Overview
│   └── Announcements
└── Recent Activity