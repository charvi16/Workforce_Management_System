# Workforce Management System (WMS) - High-Level Design (HLD)

## 1. Purpose
The Workforce Management System (WMS) is a full-stack enterprise application designed to centralize and automate employee-related workflows for a mid-to-large organization. It replaces fragmented spreadsheet, email, and manual HR processes with a secure, scalable, cloud-deployable system.

The system supports employee management, attendance tracking, leave management, department and project management, approval workflows, notice board announcements, dashboards, reporting, audit logging, and role-based access control.

## 2. Business Objectives
- Centralize employee records and department data.
- Track employee attendance, check-in/check-out, work mode, and monthly working hours.
- Automate leave application, cancellation, and approval workflows.
- Assign employees to departments and projects.
- Provide dashboards for HR, managers, and employees.
- Generate HR, compliance, attendance, and timesheet reports using Crystal Reports.
- Secure all APIs using JWT authentication and role-based authorization.
- Deploy backend and frontend through Azure DevOps CI/CD pipelines.

## 3. Technology Stack

| Layer | Technology |
|---|---|
| Frontend | Angular, TypeScript, JavaScript, Angular Material, RxJS, Chart.js |
| Backend | ASP.NET Core Web API, C# |
| ORM | Entity Framework Core Code-First |
| Database | Microsoft SQL Server |
| Authentication | JWT, HTTPS, Role-Based Authorization |
| Validation | DataAnnotations, Angular Reactive Forms |
| Reporting | Crystal Reports |
| Testing | xUnit, Moq, Jasmine/Karma |
| Logging | Serilog / ILogger, Angular Error Interceptor |
| DevOps | Azure Repos, Azure DevOps Pipelines, Azure App Service, Azure Static Web Apps |
| Secrets | Environment Variables / Azure Key Vault |

## 4. System Users and Roles

### Admin
- Manage employees, departments, roles, clients, projects, announcements, and reports.
- View organization-wide dashboards.
- Manage user accounts and access.
- Mark own attendance and apply for leave.
- Approve employee and manager leave requests.

### Manager
- Mark own attendance.
- View own and team attendance.
- Apply for leave.
- Approve or reject employee leave requests.
- Assign employees to projects.
- View team dashboards and project allocations.

### Employee
- View and update limited profile information.
- Mark attendance check-in/check-out.
- Apply or cancel leave.
- View announcements, attendance history, and leave status.

### Permission Matrix

| Feature | Employee | Manager | Admin |
|---|---|---|---|
| Login | Yes | Yes | Yes |
| View dashboard | Yes | Yes | Yes |
| View own profile | Yes | Yes | Yes |
| Update own basic profile | Limited | Limited | Limited |
| Search employee directory | Yes | Yes | Yes |
| Add employee | No | No | Yes |
| Edit employee details | No | Limited/No | Yes |
| Delete/deactivate employee | No | No | Yes |
| Add department | No | No | Yes |
| Edit department | No | No | Yes |
| Delete department | No | No | Yes |
| Add role | No | No | Yes |
| Assign role | No | No | Yes |
| Add project | No | Maybe/No | Yes |
| Assign employees to project | No | Yes/Limited | Yes |
| Apply leave | Yes | Yes | Yes |
| Approve employee leave | No | Yes | Yes |
| Approve manager leave | No | No | Yes |
| Mark own attendance | Yes | Yes | Yes |
| View own attendance | Yes | Yes | Yes |
| View team attendance | No | Yes | Yes |
| View all attendance | No | No | Yes |
| Create announcement | No | Maybe/No | Yes |
| View announcements | Yes | Yes | Yes |
| Generate own reports | Yes | Yes | Yes |
| Generate team reports | No | Yes | Yes |
| Generate company reports | No | No | Yes |

## 5. Architecture Overview

The WMS follows a layered clean architecture approach.

```text
Angular Frontend
    |
    | HTTPS + JWT
    v
ASP.NET Core Web API
    |
    | Application Services + DTOs + Validation
    v
Domain Layer
    |
    | Interfaces / Entities / Business Rules
    v
Infrastructure Layer
    |
    | EF Core Repositories / SQL Server / External Services
    v
SQL Server Database
```

## 6. Solution Structure

```text
/WMS-Solution
  /WMS.API
    Controllers
    Middleware
    Filters
    Program.cs
    appsettings.json
    appsettings.Development.json
    appsettings.Production.json

  /WMS.Application
    DTOs
    Services
    Validators
    Mappings
    Interfaces

  /WMS.Domain
    Entities
    Enums
    Interfaces
    Common

  /WMS.Infrastructure
    Data
    Repositories
    Migrations
    Configurations
    SeedData

  /WMS.Frontend
    /src/app
      auth
      employees
      attendance
      leaves
      departments
      projects
      dashboard
      announcements
      reports
      shared

  /WMS.Tests
    UnitTests
    ServiceTests
    ControllerTests

  /WMS.DevOps
    azure-pipelines-api.yml
    azure-pipelines-ui.yml
    release-pipeline.yml
```

## 7. Major Modules

### 7.1 Authentication and Authorization
- Login using username and password.
- Passwords stored as secure hashes.
- JWT issued after successful login.
- Role-based authorization for Admin, Manager, and Employee.
- Angular Auth Guard protects private routes.
- HTTP Interceptor attaches JWT token to API requests.

### 7.2 Employee Management
- Add, edit, view, deactivate, and search employees.
- Search by employee ID, name, department, role, email, or status.
- Validate employee age, email uniqueness, phone number, department, and role.

### 7.3 Department Management
- Create, update, view, and deactivate departments.
- Department is mapped with employees.

### 7.4 Attendance Management
- Employee, Manager, and Admin check-in/check-out for themselves.
- Calculate total working hours.
- Support WFO, WFH, and Hybrid work modes.
- Prevent duplicate check-in for the same date.
- Employees view their own attendance.
- Managers view their own and team attendance.
- Admins view all attendance.
- Generate monthly attendance and timesheet reports.

### 7.5 Leave Management
- Employee, Manager, and Admin can apply for leave.
- Employee leave is reviewed by a Manager or Admin.
- Manager leave is reviewed by an Admin.
- Admin leave is auto-approved.
- Users can view their own leave status and cancel their own pending/approved leave.
- Validates date range, overlapping leave, and approval permissions.

### 7.6 Project and Client Management
- Admin/Manager can create and manage clients.
- Admin/Manager can create and manage projects.
- Employees can be allocated to projects.
- Track allocation status and audit metadata.

### 7.7 Announcement / Notice Board
- Admin can create announcements.
- Active announcements visible to employees and managers.
- Announcements can be activated/deactivated.

### 7.8 Dashboard and Analytics
- Summary cards for total employees, active employees, leaves, attendance, and projects.
- Attendance charts using Chart.js.
- Leave statistics by status/type.
- Project allocation count.
- Real-time UI updates using RxJS BehaviorSubject.

### 7.9 Reporting
- Timesheet report.
- Monthly attendance report.
- Leave report.
- Employee list report.
- Project allocation report.
- Reports generated using Crystal Reports.

### 7.10 Audit Logging
- Tracks insert, update, and delete actions.
- Stores entity name, record ID, action, user ID, and timestamp.
- Useful for compliance and debugging.

## 8. Database Overview

Core tables:
- Employee
- Department
- Role
- Attendance
- Leave
- Announcement
- Project
- Client
- EmployeeProjectAllocation
- UserLogin
- AuditLog

Important relationships:
- One Department has many Employees.
- One Role has many Employees and UserLogin records.
- One Employee has many Attendance records.
- One Employee has many Leave records.
- One Manager approves many Leave records.
- One Client has many Projects.
- One Project has many EmployeeProjectAllocations.
- One Employee can be allocated to many Projects.

## 9. API Design Overview

Base URL:
```text
/api/v1
```

Main API groups:
- `/auth`
- `/employees`
- `/departments`
- `/roles`
- `/attendance`
- `/leaves`
- `/projects`
- `/clients`
- `/allocations`
- `/announcements`
- `/dashboard`
- `/reports`

API responses should follow a common response format:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": {},
  "errors": []
}
```

## 10. Security Design

- JWT token-based authentication.
- Role-based authorization using `[Authorize(Roles = "Admin")]` etc.
- HTTPS enforced in production.
- CORS configured only for trusted frontend origins.
- Passwords stored using secure hashing.
- Sensitive configuration stored in environment variables or Azure Key Vault.
- Input validation on backend and frontend.
- SQL injection protection through EF Core parameterized queries.
- Audit logging for critical data changes.

## 11. Performance and Scalability

Target: support up to 5000 users.

Performance practices:
- Use async/await in API calls.
- Add indexes on frequently searched columns.
- Use pagination for employee, leave, attendance, and project lists.
- Keep API response time under 200 ms for common queries.
- Use DTOs to avoid over-fetching.
- Use database indexes for Email, DepartmentId, RoleId, AttendanceDate, EmpId, ProjectId, and Status.

## 12. Logging and Exception Handling

Backend:
- Global exception middleware.
- Structured logs using ILogger or Serilog.
- Log errors, request path, user ID, and trace ID.
- Do not expose stack traces in production.

Frontend:
- Global HTTP error interceptor.
- Toast/snackbar error messages.
- Route unauthorized users to login or forbidden page.

## 13. Deployment Architecture

```text
Developer Pushes Code
        |
        v
Azure Repos
        |
        v
Azure DevOps Build Pipeline
        |
        |-- Restore .NET packages
        |-- Build API
        |-- Run backend tests
        |-- Install npm packages
        |-- Build Angular app
        |-- Publish artifacts
        v
Azure DevOps Release Pipeline
        |
        |-- Deploy API to Azure App Service
        |-- Deploy Angular to Azure Static Web Apps
        |-- Apply environment configuration
        |-- Run smoke tests
```

## 14. Git Branching Strategy

```text
main        -> production-ready code
 dev        -> integration branch
 feature/*  -> individual features
 bugfix/*   -> bug fixes
 hotfix/*   -> emergency production fixes
 db/*       -> database migration/schema changes
```

Rules:
- Developers create feature branches from `dev`.
- Pull requests required before merging to `dev`.
- Unit tests must pass before merge.
- Code review mandatory.
- `main` is updated only after stable release validation.

## 15. Non-Functional Requirements Mapping

| Requirement | Design Decision |
|---|---|
| Security | JWT, HTTPS, RBAC, Key Vault |
| Usability | Angular Material, responsive UI |
| Performance | Async APIs, indexes, pagination |
| Scalability | Layered architecture, cloud deployment |
| Maintainability | Clean architecture, repository/service pattern |
| Testing | xUnit, Jasmine/Karma, test cases per module |
| Deployment | Azure DevOps CI/CD |
| Reporting | Crystal Reports |

## 16. Assumptions
- One employee belongs to one department.
- One employee can be assigned to multiple projects.
- One project belongs to one client.
- Attendance is recorded once per employee per day.
- Employee leave is approved by Manager/Admin, Manager leave by Admin, and Admin leave is auto-approved.
- Admin has full system access.
- Employee records are soft-deactivated using Status rather than deleted.

## 17. Risks and Mitigation

| Risk | Mitigation |
|---|---|
| Duplicate attendance entries | Unique constraint on EmpId + AttendanceDate |
| Unauthorized API access | JWT + role-based authorization |
| Sensitive credential leakage | Azure Key Vault / environment variables |
| Poor performance on large datasets | Indexing, pagination, DTO-based queries |
| Manual deployment errors | Azure DevOps CI/CD |
| Inconsistent validation | DataAnnotations + Angular Reactive Forms |
