# Workforce Management System - Project Brief

## Project Name
Workforce Management System (WMS)

## Project Type
Enterprise full-stack web application

## Objective
The objective of the Workforce Management System is to design and develop a centralized platform for managing employee records, attendance, leaves, departments, projects, approvals, announcements, reports, and workforce dashboards.

The system is intended for a mid-to-large enterprise where current HR and workforce operations are handled manually through spreadsheets, emails, and disconnected processes. WMS solves this by providing a secure, scalable, responsive, and cloud-deployable application.

## Problem Statement
The existing employee management workflow is fragmented and inefficient. Attendance tracking, working hour calculation, leave approvals, employee data updates, and reporting are handled manually. This causes poor visibility, data errors, delayed approvals, and difficulty in generating HR and compliance reports.

A centralized Workforce Management System is required to automate these workflows and improve accuracy, transparency, and operational efficiency.

## Proposed Solution
The proposed WMS will provide a role-based web application with an ASP.NET Core Web API backend, Angular frontend, SQL Server database, and Azure DevOps CI/CD deployment pipeline.

The backend will be built using a clean layered architecture with Domain, Application, Infrastructure, and API projects. Entity Framework Core Code-First approach will be used for database creation and migration. DataAnnotations will be used for validation. JWT will secure API communication.

The frontend will be built using Angular, Angular Material, RxJS, Reactive Forms, and Chart.js. It will provide responsive interfaces for Admin, Manager, and Employee users.

Crystal Reports will be used to generate attendance, timesheet, leave, employee, and project allocation reports.

## Key Features

### 1. Authentication and Authorization
- Secure login using username and password.
- JWT-based API authentication.
- Role-based access for Admin, Manager, and Employee.
- Angular Auth Guard and HTTP Interceptor.

### 2. Employee Management
- Add, update, view, search, and deactivate employees.
- Search employees by name, employee ID, department, role, and status.
- Validate email, phone number, age, department, and role.

### 3. Attendance Management
- Employee check-in and check-out.
- Work mode support: WFO, WFH, Hybrid.
- Automatic total working hours calculation.
- Monthly attendance view.
- Timesheet report generation.

### 4. Leave Management
- Apply for leave.
- Cancel pending leave.
- Manager approval and rejection workflow.
- Leave history and leave status tracking.
- Leave statistics for dashboards.

### 5. Department and Role Management
- Manage departments.
- Map employees to departments and roles.
- Support Admin, Manager, and Employee roles.

### 6. Client and Project Management
- Manage client details.
- Create and update projects.
- Assign employees to projects.
- Track active/inactive project allocations.

### 7. Announcement / Notice Board
- Admin can create announcements.
- Active announcements are visible to users.
- Announcements can be updated or deactivated.

### 8. Dashboard and Analytics
- Admin dashboard for organization-wide data.
- Manager dashboard for team data.
- Employee dashboard for personal attendance and leaves.
- Summary cards and charts for attendance, leaves, and projects.

### 9. Reporting
- Attendance report.
- Monthly timesheet report.
- Leave report.
- Employee master report.
- Project allocation report.
- Reports generated using Crystal Reports.

### 10. Audit Logging
- Tracks insert, update, and delete operations.
- Stores entity name, record ID, action, user, and timestamp.
- Helps with compliance and debugging.

## Technology Stack

| Area | Technology |
|---|---|
| Frontend | Angular, TypeScript, JavaScript, Angular Material |
| UI State | RxJS BehaviorSubject |
| Forms | Angular Reactive Forms |
| Charts | Chart.js |
| Backend | ASP.NET Core Web API, C# |
| Database | SQL Server Image from docker |
| ORM | Entity Framework Core Code-First |
| Validation | DataAnnotations, Angular Validators |
| Authentication | JWT |
| Reporting | Crystal Reports |
| Testing | xUnit, Moq, Jasmine/Karma |
| DevOps | Azure DevOps, Azure Repos, Azure App Service, Azure Static Web Apps |
| Secret Management | Environment Variables / Azure Key Vault |

## System Architecture

```text
Angular Frontend
        |
        | HTTPS + JWT
        v
ASP.NET Core Web API
        |
        v
Application Layer
        |
        v
Domain Layer
        |
        v
Infrastructure Layer
        |
        v
SQL Server Database
```

## Main Modules

1. Auth Module
2. Employee Module
3. Department Module
4. Role Module
5. Attendance Module
6. Leave Module
7. Client Module
8. Project Module
9. Employee Project Allocation Module
10. Announcement Module
11. Dashboard Module
12. Report Module
13. Audit Log Module

## Database Tables

The system will use the following core tables:

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

## Folder Structure

```text
/WMS-Solution
  /WMS.API
  /WMS.Application
  /WMS.Domain
  /WMS.Infrastructure
  /WMS.Frontend
  /WMS.Tests
  /WMS.DevOps
```

## Frontend Structure

```text
/src/app
  /auth
  /employees
  /attendance
  /leaves
  /departments
  /projects
  /dashboard
  /announcements
  /reports
  /shared
```

## Non-Functional Requirements

### Security
- JWT authentication.
- HTTPS communication.
- Role-based authorization.
- CORS configuration.
- Secure credentials using environment variables or Azure Key Vault.

### Performance
- Common APIs should respond within 200 ms.
- Use async database operations.
- Add indexes on frequently searched fields.
- Use pagination for large data lists.

### Scalability
- Designed to support up to 5000 users.
- Layered backend architecture.
- Cloud deployment using Azure services.

### Usability
- Responsive Angular UI.
- Clean navigation.
- Form validations.
- Error and success notifications.

### Maintainability
- Clean folder structure.
- Repository and Service pattern.
- DTO-based API responses.
- Updated HLD and LLD documents.
- Consistent naming conventions.

### Testability
- Unit tests for each backend service.
- Controller tests for APIs.
- Frontend form and component tests.
- Test cases for each module.

## DevOps Strategy

The project will use Azure DevOps for source control, build, test, and deployment.

### Branching Strategy

```text
main        -> production-ready code
 dev        -> integration branch
 feature/*  -> feature development
 bugfix/*   -> bug fixes
 db/*       -> database changes
```

### CI Pipeline
- Restore .NET packages.
- Build backend API.
- Run backend tests.
- Install Angular dependencies.
- Build Angular frontend.
- Publish artifacts.

### CD Pipeline
- Deploy API to Azure App Service.
- Deploy Angular app to Azure Static Web Apps.
- Use environment-specific configuration.
- Fetch secrets from Azure Key Vault.
- Run smoke tests.
- Use deployment approvals.

## Expected Deliverables

- Complete ASP.NET Core Web API backend.
- Complete Angular frontend.
- SQL Server database generated using EF Core Code-First migrations.
- JWT-secured APIs.
- Responsive UI.
- Crystal Reports integration.
- Unit test cases and test evidence.
- Swagger API documentation.
- Azure DevOps CI/CD pipeline files.
- HLD document.
- LLD document.
- Project brief.
- Task tracking document.
- Final README and user manual.

## Success Criteria

The project will be considered successful when:

- All major modules are functional end-to-end.
- Admin, Manager, and Employee roles work correctly.
- Employee CRUD, attendance, leave, project allocation, and reports work properly.
- APIs are secured using JWT.
- Angular UI is responsive and user-friendly.
- Unit tests pass before merge.
- Application builds and deploys successfully using Azure DevOps.
- Documentation is complete and updated.
