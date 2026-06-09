# Workforce Management System - tasks.md

## Phase 1: Project Setup

- [ ] Create Git repository `WMS-Solution`. --Done
- [ ] Create branches: `main`, `dev`, `feature/*`, `bugfix/*`, `db/*`.
- [ ] Create ASP.NET Core solution.
- [ ] Add backend projects:
  - [ ] `WMS.API`
  - [ ] `WMS.Application`
  - [ ] `WMS.Domain`
  - [ ] `WMS.Infrastructure`
  - [ ] `WMS.Tests`
- [ ] Create Angular project `WMS.Frontend` with routing enabled.
- [ ] Create `WMS.DevOps` folder for Azure Pipeline YAML files.
- [ ] Configure `.gitignore` for .NET, Angular, Visual Studio Code, and environment files.
- [ ] Add README with setup instructions.

## Phase 2: Backend Base Setup

- [ ] Install required NuGet packages:
  - [ ] Entity Framework Core
  - [ ] EF Core SQL Server Provider
  - [ ] EF Core Tools
  - [ ] AutoMapper
  - [ ] JWT Bearer Authentication
  - [ ] xUnit
  - [ ] Moq
  - [ ] Serilog or logging package
- [ ] Configure project references:
  - [ ] API references Application and Infrastructure.
  - [ ] Application references Domain.
  - [ ] Infrastructure references Domain and Application.
  - [ ] Tests reference Application, Domain, Infrastructure, and API.
- [ ] Configure `appsettings.Development.json`.
- [ ] Configure `appsettings.Production.json`.
- [ ] Add dependency injection extensions.
- [ ] Configure Swagger.
- [ ] Configure CORS.
- [ ] Configure global exception middleware.
- [ ] Configure logging.

## Phase 3: Domain Layer

- [ ] Create `Employee` entity.
- [ ] Create `Department` entity.
- [ ] Create `Role` entity.
- [ ] Create `Attendance` entity.
- [ ] Create `Leave` entity.
- [ ] Create `Announcement` entity.
- [ ] Create `Project` entity.
- [ ] Create `Client` entity.
- [ ] Create `EmployeeProjectAllocation` entity.
- [ ] Create `UserLogin` entity.
- [ ] Create `AuditLog` entity.
- [ ] Add DataAnnotations to entities.
- [ ] Add enums for:
  - [ ] Gender
  - [ ] EmployeeStatus
  - [ ] LeaveStatus
  - [ ] LeaveType
  - [ ] WorkMode
  - [ ] ProjectStatus
  - [ ] UserRole

## Phase 4: Infrastructure and Database

- [ ] Create `WmsDbContext`.
- [ ] Add DbSet properties for all entities.
- [ ] Configure entity relationships using Fluent API.
- [ ] Configure unique index on Employee Email.
- [ ] Configure unique index on UserLogin Username.
- [ ] Configure unique index on Attendance EmpId + AttendanceDate.
- [ ] Configure foreign keys.
- [ ] Configure delete behavior.
- [ ] Seed default roles: Admin, Manager, Employee.
- [ ] Seed sample departments.
- [ ] Create initial EF Core migration.
- [ ] Apply migration to SQL Server.
- [ ] Verify database tables.
- [ ] Add recommended indexes.

## Phase 5: Repository Layer

- [ ] Create generic repository interface.
- [ ] Create generic repository implementation.
- [ ] Create employee repository.
- [ ] Create attendance repository.
- [ ] Create leave repository.
- [ ] Create department repository.
- [ ] Create role repository.
- [ ] Create project repository.
- [ ] Create client repository.
- [ ] Create allocation repository.
- [ ] Create announcement repository.
- [ ] Create audit log repository.
- [ ] Register repositories in dependency injection.

## Phase 6: Application Layer

- [ ] Create common API response model.
- [ ] Create pagination models.
- [ ] Create AutoMapper profiles.
- [ ] Create DTOs for Employee module.
- [ ] Create DTOs for Department module.
- [ ] Create DTOs for Attendance module.
- [ ] Create DTOs for Leave module.
- [ ] Create DTOs for Project module.
- [ ] Create DTOs for Client module.
- [ ] Create DTOs for Announcement module.
- [ ] Create DTOs for Auth module.
- [ ] Create service interfaces.
- [ ] Create service implementations.
- [ ] Add business validations.

## Phase 7: Authentication and Authorization

- [ ] Create user registration service.
- [ ] Create login service.
- [ ] Implement password hashing.
- [ ] Configure JWT settings.
- [ ] Generate JWT token with UserId and Role claims.
- [ ] Add `[Authorize]` to protected controllers.
- [ ] Add role-based authorization for Admin, Manager, Employee.
- [ ] Create change password API.
- [ ] Create seed admin user.
- [ ] Test login using Swagger/Postman.

## Phase 8: Employee Management Module

- [ ] Create employee controller.
- [ ] Add API to create employee.
- [ ] Add API to update employee.
- [ ] Add API to get employee by ID.
- [ ] Add API to search employees by name, ID, department, role, and status.
- [ ] Add API to deactivate employee.
- [ ] Validate age >= 18.
- [ ] Validate unique email.
- [ ] Validate valid department and role.
- [ ] Add audit logging for create/update/deactivate.
- [ ] Write unit tests.

## Phase 9: Department and Role Module

- [ ] Create department controller.
- [ ] Add department CRUD APIs.
- [ ] Add role list API.
- [ ] Prevent deleting departments with active employees.
- [ ] Add audit logging.
- [ ] Write unit tests.

## Phase 10: Attendance Module

- [ ] Create attendance controller.
- [ ] Add check-in API.
- [ ] Add check-out API.
- [ ] Add monthly attendance API.
- [ ] Add team attendance API for managers.
- [ ] Prevent duplicate check-in for same employee/date.
- [ ] Calculate total hours on checkout.
- [ ] Validate work mode.
- [ ] Add attendance report data endpoint.
- [ ] Write unit tests.

## Phase 11: Leave Module

- [ ] Create leave controller.
- [ ] Add apply leave API.
- [ ] Add cancel leave API.
- [ ] Add approve leave API.
- [ ] Add reject leave API.
- [ ] Add my leaves API.
- [ ] Add pending approvals API.
- [ ] Validate leave date range.
- [ ] Prevent overlapping leave.
- [ ] Restrict approval to Manager/Admin.
- [ ] Add audit logging.
- [ ] Write unit tests.

## Phase 12: Client and Project Module

- [ ] Create client controller.
- [ ] Add client CRUD APIs.
- [ ] Create project controller.
- [ ] Add project CRUD APIs.
- [ ] Validate project date range.
- [ ] Create allocation controller.
- [ ] Add employee-to-project allocation API.
- [ ] Add allocation update/deactivate API.
- [ ] Prevent duplicate active allocation.
- [ ] Add audit logging.
- [ ] Write unit tests.

## Phase 13: Announcement Module

- [ ] Create announcement controller.
- [ ] Add create announcement API.
- [ ] Add update announcement API.
- [ ] Add activate/deactivate announcement API.
- [ ] Add active announcements API.
- [ ] Restrict create/update/delete to Admin.
- [ ] Add audit logging.
- [ ] Write unit tests.

## Phase 14: Dashboard Module

- [ ] Create dashboard controller.
- [ ] Add admin dashboard API.
- [ ] Add manager dashboard API.
- [ ] Add employee dashboard API.
- [ ] Add employee count summary.
- [ ] Add attendance summary.
- [ ] Add leave statistics.
- [ ] Add project count summary.
- [ ] Add chart data endpoints.
- [ ] Write unit tests.

## Phase 15: Crystal Reports

- [ ] Install Crystal Reports runtime/tools.
- [ ] Create attendance report template.
- [ ] Create monthly timesheet report template.
- [ ] Create leave report template.
- [ ] Create employee master report template.
- [ ] Create project allocation report template.
- [ ] Add report export to PDF.
- [ ] Add report export to Excel.
- [ ] Add report filter options.
- [ ] Test report generation.

## Phase 16: Angular Base Setup

- [ ] Install Angular Material.
- [ ] Install Chart.js.
- [ ] Configure environment files.
- [ ] Create shared layout.
- [ ] Create navbar/sidebar.
- [ ] Create login page.
- [ ] Create dashboard routes.
- [ ] Create reusable table component.
- [ ] Create reusable confirmation dialog.
- [ ] Create reusable toast/snackbar service.
- [ ] Create loading spinner.

## Phase 17: Angular Auth Module

- [ ] Create auth service.
- [ ] Create login component.
- [ ] Store JWT token.
- [ ] Decode role from JWT.
- [ ] Create auth guard.
- [ ] Create role guard.
- [ ] Create HTTP interceptor.
- [ ] Add logout functionality.
- [ ] Add unauthorized page.

## Phase 18: Angular Employee Module

- [ ] Create employee list page.
- [ ] Create employee search filters.
- [ ] Create employee form page.
- [ ] Add create employee form validation.
- [ ] Add update employee form validation.
- [ ] Create employee detail page.
- [ ] Add deactivate employee button.
- [ ] Connect module with backend APIs.

## Phase 19: Angular Attendance Module

- [ ] Create check-in/check-out page.
- [ ] Add work mode selection.
- [ ] Show today's attendance status.
- [ ] Create monthly attendance page.
- [ ] Create attendance table.
- [ ] Add manager team attendance view.
- [ ] Connect module with backend APIs.

## Phase 20: Angular Leave Module

- [ ] Create apply leave page.
- [ ] Add leave form validation.
- [ ] Create my leaves page.
- [ ] Create leave approval page for managers.
- [ ] Add approve/reject buttons.
- [ ] Add cancel leave button.
- [ ] Connect module with backend APIs.

## Phase 21: Angular Department, Project, Client Modules

- [ ] Create department list and form pages.
- [ ] Create client list and form pages.
- [ ] Create project list and form pages.
- [ ] Create employee allocation page.
- [ ] Add validation for project dates.
- [ ] Connect modules with backend APIs.

## Phase 22: Angular Dashboard and Reports

- [ ] Create admin dashboard.
- [ ] Create manager dashboard.
- [ ] Create employee dashboard.
- [ ] Add summary cards.
- [ ] Add attendance chart.
- [ ] Add leave statistics chart.
- [ ] Add project count chart.
- [ ] Create reports page.
- [ ] Add report filters.
- [ ] Add download report buttons.

## Phase 23: Testing

- [ ] Write backend unit tests using xUnit.
- [ ] Write service tests using Moq.
- [ ] Write controller tests.
- [ ] Write Angular unit tests using Jasmine/Karma.
- [ ] Test API endpoints using Swagger/Postman.
- [ ] Test role-based authorization.
- [ ] Test frontend form validations.
- [ ] Test report generation.
- [ ] Prepare test evidence screenshots.

## Phase 24: DevOps and Deployment

- [ ] Create Azure Repo.
- [ ] Create Azure App Service for API.
- [ ] Create Azure Static Web App for Angular frontend.
- [ ] Create SQL Server database in Azure or configure hosted SQL Server.
- [ ] Create Azure Key Vault.
- [ ] Add database connection string to Key Vault.
- [ ] Add JWT secret to Key Vault.
- [ ] Create API build pipeline.
- [ ] Create Angular build pipeline.
- [ ] Add test automation stage.
- [ ] Add artifact publishing stage.
- [ ] Create release pipeline.
- [ ] Add deployment approvals.
- [ ] Run smoke tests after deployment.

## Phase 25: Documentation and Final Submission

- [ ] Complete HLD document.
- [ ] Complete LLD document.
- [ ] Complete database schema document.
- [ ] Complete API documentation using Swagger screenshots.
- [ ] Complete test case document.
- [ ] Add test evidence screenshots.
- [ ] Add deployment screenshots.
- [ ] Add user manual.
- [ ] Add final README.
- [ ] Verify end-to-end functionality.
- [ ] Prepare final project demo.
