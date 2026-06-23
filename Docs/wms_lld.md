# Workforce Management System (WMS) - Low-Level Design (LLD)

## 1. Purpose
This Low-Level Design document describes the internal implementation details of the Workforce Management System. It includes entity design, database structure, DTOs, services, repositories, API endpoints, validations, exception handling, test cases, and frontend component structure.

## 2. Backend Project Design

### 2.1 Backend Layer Responsibilities

#### WMS.Domain
Contains pure business entities, enums, interfaces, and shared domain rules.

#### WMS.Application
Contains DTOs, service interfaces, business services, AutoMapper profiles, and validation logic.

#### WMS.Infrastructure
Contains EF Core DbContext, repository implementations, migrations, database configurations, and seed data.

##### WMS.API
Contains controllers, middleware, authentication configuration, Swagger setup, dependency injection, and API request handling.

## 3. Domain Entities

### 3.1 Employee Entity

```csharp
public class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(80)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, RegularExpression("^[MFO]$")]
    public char Gender { get; set; }

    [Required]
    public DateTime DOB { get; set; }

    [Required]
    public DateTime DOJ { get; set; }

    public int DepartmentId { get; set; }
    public int RoleId { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }

    public Department Department { get; set; }
    public Role Role { get; set; }
    public ICollection<Attendance> Attendances { get; set; }
    public ICollection<Leave> Leaves { get; set; }
    public ICollection<EmployeeProjectAllocation> ProjectAllocations { get; set; }
}
```

### 3.2 Department Entity

```csharp
public class Department
{
    [Key]
    public int DepartmentId { get; set; }

    [Required, MaxLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public ICollection<Employee> Employees { get; set; }
}
```

### 3.3 Role Entity

```csharp
public class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Description { get; set; }

    public ICollection<Employee> Employees { get; set; }
    public ICollection<UserLogin> UserLogins { get; set; }
}
```

### 3.4 Attendance Entity

```csharp
public class Attendance
{
    [Key]
    public int AttendanceId { get; set; }

    public int EmpId { get; set; }

    [Required]
    public DateTime CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public double? TotalHours { get; set; }

    [MaxLength(20)]
    public string? WorkMode { get; set; }

    [Required]
    public DateTime AttendanceDate { get; set; }

    public Employee Employee { get; set; }
}
```

### 3.5 Leave Entity

```csharp
public class Leave
{
    [Key]
    public int LeaveId { get; set; }

    public int EmpId { get; set; }

    [Required, MaxLength(30)]
    public string LeaveType { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Reason { get; set; }

    [Required]
    public DateTime FromDate { get; set; }

    [Required]
    public DateTime ToDate { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public DateTime AppliedOn { get; set; } = DateTime.UtcNow;

    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedOn { get; set; }

    public Employee Employee { get; set; }
    public Employee? Approver { get; set; }
}
```

### 3.6 Client Entity

```csharp
public class Client
{
    [Key]
    public int ClientId { get; set; }

    [Required, MaxLength(100)]
    public string ClientName { get; set; } = string.Empty;

    public string? ClientAddress { get; set; }

    [Column(TypeName = "numeric(10,0)")]
    public decimal? ClientPhoneNumber { get; set; }

    [MaxLength(20)]
    public string? ClientLocation { get; set; }

    public bool Status { get; set; } = true;

    public ICollection<Project> Projects { get; set; }
}
```

### 3.7 Project Entity

```csharp
public class Project
{
    [Key]
    public int ProjectId { get; set; }

    [Required, MaxLength(100)]
    public string ProjectName { get; set; } = string.Empty;

    public int? ClientId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    public Client? Client { get; set; }
    public ICollection<EmployeeProjectAllocation> EmployeeAllocations { get; set; }
}
```

### 3.8 EmployeeProjectAllocation Entity

```csharp
public class EmployeeProjectAllocation
{
    [Key]
    public int AllocationId { get; set; }

    public int EmpId { get; set; }
    public int ProjectId { get; set; }

    [Required]
    public DateTime AssignedOn { get; set; }

    [Required]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    public bool Status { get; set; } = true;

    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Employee Employee { get; set; }
    public Project Project { get; set; }
}
```

### 3.9 Announcement Entity

```csharp
public class Announcement
{
    [Key]
    public int AnnouncementId { get; set; }

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Employee Creator { get; set; }
}
```

### 3.10 UserLogin Entity

```csharp
public class UserLogin
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public DateTime? LastLogin { get; set; }

    public Role Role { get; set; }
}
```

### 3.11 AuditLog Entity

```csharp
public class AuditLog
{
    [Key]
    public int AuditId { get; set; }

    public string EntityName { get; set; } = string.Empty;
    public int RecordId { get; set; }

    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;

    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
```

## 4. EF Core DbContext Design

```csharp
public class WmsDbContext : DbContext
{
    public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options) { }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<Leave> Leaves { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<EmployeeProjectAllocation> EmployeeProjectAllocations { get; set; }
    public DbSet<UserLogin> UserLogins { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email)
            .IsUnique();

        modelBuilder.Entity<UserLogin>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.EmpId, a.AttendanceDate })
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Role)
            .WithMany(r => r.Employees)
            .HasForeignKey(e => e.RoleId);

        modelBuilder.Entity<Leave>()
            .HasOne(l => l.Employee)
            .WithMany(e => e.Leaves)
            .HasForeignKey(l => l.EmpId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Leave>()
            .HasOne(l => l.Approver)
            .WithMany()
            .HasForeignKey(l => l.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin", Description = "System Administrator" },
            new Role { RoleId = 2, RoleName = "Manager", Description = "Team Manager" },
            new Role { RoleId = 3, RoleName = "Employee", Description = "Regular Employee" }
        );
    }
}
```

## 5. Database Tables and Constraints

### 5.1 Employee
| Column | Type | Constraint |
|---|---|---|
| EmployeeId | INT | PK, Identity |
| FirstName | VARCHAR(50) | NOT NULL |
| LastName | VARCHAR(50) | NOT NULL |
| Email | VARCHAR(80) | UNIQUE, NOT NULL |
| PhoneNumber | VARCHAR(15) | NOT NULL |
| Gender | CHAR(1) | CHECK M/F/O |
| DOB | DATE | NOT NULL, age >= 18 business validation |
| DOJ | DATE | NOT NULL |
| DepartmentId | INT | FK |
| RoleId | INT | FK |
| Status | VARCHAR(20) | DEFAULT Active |
| CreatedOn | DATETIME | DEFAULT GETDATE() |
| UpdatedOn | DATETIME | NULL |

### 5.2 Recommended Indexes

```sql
CREATE INDEX IX_Employee_DepartmentId ON Employees(DepartmentId);
CREATE INDEX IX_Employee_RoleId ON Employees(RoleId);
CREATE INDEX IX_Employee_Status ON Employees(Status);
CREATE INDEX IX_Attendance_EmpId_Date ON Attendances(EmpId, AttendanceDate);
CREATE INDEX IX_Leave_EmpId_Status ON Leaves(EmpId, Status);
CREATE INDEX IX_Project_Status ON Projects(Status);
CREATE INDEX IX_Allocation_EmpId_ProjectId ON EmployeeProjectAllocations(EmpId, ProjectId);
```

## 6. DTO Design

### 6.1 Employee DTOs

```csharp
public class CreateEmployeeDto
{
    [Required, MaxLength(50)]
    public string FirstName { get; set; }

    [Required, MaxLength(50)]
    public string LastName { get; set; }

    [Required, EmailAddress, MaxLength(80)]
    public string Email { get; set; }

    [Required, MaxLength(15)]
    public string PhoneNumber { get; set; }

    [Required]
    public char Gender { get; set; }

    [Required]
    public DateTime DOB { get; set; }

    [Required]
    public DateTime DOJ { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public int RoleId { get; set; }
}

public class EmployeeResponseDto
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string DepartmentName { get; set; }
    public string RoleName { get; set; }
    public string Status { get; set; }
}
```

### 6.2 Attendance DTOs

```csharp
public class CheckInDto
{
    [Required]
    public int EmpId { get; set; }

    [Required, MaxLength(20)]
    public string WorkMode { get; set; }
}

public class CheckOutDto
{
    [Required]
    public int EmpId { get; set; }
}
```

### 6.3 Leave DTOs

```csharp
public class ApplyLeaveDto
{
    [Required, MaxLength(30)]
    public string LeaveType { get; set; }

    [MaxLength(255)]
    public string? Reason { get; set; }

    [Required]
    public DateTime FromDate { get; set; }

    [Required]
    public DateTime ToDate { get; set; }
}

public class LeaveApprovalDto
{
    [Required]
    public int LeaveId { get; set; }

    [Required]
    public string Status { get; set; }
}
```

## 7. Repository Design

### 7.1 Generic Repository Interface

```csharp
public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync();
}
```

### 7.2 Specific Repository Examples

```csharp
public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByEmailAsync(string email);
    Task<IEnumerable<Employee>> SearchAsync(string? name, int? departmentId, int? roleId);
}

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<Attendance?> GetTodayAttendanceAsync(int empId, DateTime date);
    Task<IEnumerable<Attendance>> GetMonthlyAttendanceAsync(int empId, int month, int year);
}
```

## 8. Service Layer Design

### 8.1 Employee Service

Responsibilities:
- Validate age >= 18.
- Validate unique email.
- Validate department and role existence.
- Create/update employee.
- Return DTOs, not entities.

```csharp
public interface IEmployeeService
{
    Task<EmployeeResponseDto> CreateEmployeeAsync(CreateEmployeeDto dto);
    Task<EmployeeResponseDto?> GetEmployeeByIdAsync(int id);
    Task<IEnumerable<EmployeeResponseDto>> SearchEmployeesAsync(string? name, int? departmentId, int? roleId);
    Task<EmployeeResponseDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto dto);
    Task<bool> DeactivateEmployeeAsync(int id);
}
```

### 8.2 Attendance Service

Business rules:
- Employee cannot check in twice on the same day.
- Check-out is allowed only after check-in.
- TotalHours = CheckOut - CheckIn.
- WorkMode must be WFO, WFH, or Hybrid.

```csharp
public interface IAttendanceService
{
    Task<AttendanceResponseDto> CheckInAsync(CheckInDto dto);
    Task<AttendanceResponseDto> CheckOutAsync(CheckOutDto dto);
    Task<IEnumerable<AttendanceResponseDto>> GetMonthlyAttendanceAsync(int empId, int month, int year);
}
```

### 8.3 Leave Service

Business rules:
- FromDate cannot be after ToDate.
- Past leave dates are not allowed unless admin override is implemented.
- Overlapping pending/approved leaves are not allowed.
- Employee, Manager, and Admin can apply leave for themselves.
- Employee leave is approved or rejected by Manager/Admin.
- Manager leave is approved or rejected by Admin.
- Admin leave is auto-approved with ApprovedBy set to the admin employee record.
- Users cannot approve or reject their own leave.
- Own pending/approved leave can be cancelled.

```csharp
public interface ILeaveService
{
    Task<LeaveResponseDto> ApplyLeaveAsync(int empId, ApplyLeaveDto dto);
    Task<bool> CancelLeaveAsync(int leaveId, int empId);
    Task<LeaveResponseDto> ApproveOrRejectLeaveAsync(int leaveId, int managerId, string status);
    Task<IEnumerable<LeaveResponseDto>> GetLeavesByEmployeeAsync(int empId);
    Task<IEnumerable<LeaveResponseDto>> GetPendingLeavesAsync();
}
```

## 9. API Endpoint Design

### 9.1 Auth APIs

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/v1/auth/login` | Public | Login and generate JWT |
| POST | `/api/v1/auth/change-password` | Authenticated | Change password |

### 9.2 Employee APIs

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/v1/employees` | Admin/Manager | List/search employees |
| GET | `/api/v1/employees/{id}` | Admin/Manager/Self | Get employee details |
| POST | `/api/v1/employees` | Admin | Create employee and login account |
| PUT | `/api/v1/employees/{id}` | Admin | Update employee |
| DELETE | `/api/v1/employees/{id}` | Admin | Soft deactivate employee |

### 9.3 Attendance APIs

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/v1/attendance/check-in` | Employee/Manager/Admin | Check in for self |
| POST | `/api/v1/attendance/check-out` | Employee/Manager/Admin | Check out for self |
| GET | `/api/v1/attendance/monthly` | Employee/Manager/Admin | Monthly attendance; employee sees self, manager sees self/team, admin sees all |
| GET | `/api/v1/attendance/employees` | Employee/Manager/Admin | Employees available for attendance view scope |

### 9.4 Leave APIs

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/v1/leaves/apply` | Employee/Manager/Admin | Apply leave for self |
| PUT | `/api/v1/leaves/{id}/cancel` | Employee/Manager/Admin | Cancel own leave |
| PUT | `/api/v1/leaves/{id}/review` | Manager/Admin | Approve or reject according to approval matrix |
| GET | `/api/v1/leaves` | Employee/Manager/Admin | Leave status list scoped by role |
| GET | `/api/v1/leaves/statistics` | Employee/Manager/Admin | Leave statistics scoped by role |
| GET | `/api/v1/leaves/employees` | Employee/Manager/Admin | Employees available for leave view scope |

### 9.5 Department APIs

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/v1/departments` | Authenticated | List departments |
| POST | `/api/v1/departments` | Admin | Create department |
| PUT | `/api/v1/departments/{id}` | Admin | Update department |
| DELETE | `/api/v1/departments/{id}` | Admin | Delete/deactivate department |

### 9.6 Project and Allocation APIs

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/v1/projects` | Authenticated | List projects |
| POST | `/api/v1/projects` | Admin/Manager | Create project |
| PUT | `/api/v1/projects/{id}` | Admin/Manager | Update project |
| POST | `/api/v1/allocations` | Admin/Manager | Assign employee to project |
| PUT | `/api/v1/allocations/{id}/deactivate` | Admin/Manager | Deactivate allocation |

### 9.7 Dashboard APIs

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/v1/dashboard/admin` | Admin | Admin dashboard |
| GET | `/api/v1/dashboard/manager` | Manager | Manager dashboard |
| GET | `/api/v1/dashboard/employee` | Employee | Employee dashboard |

### 9.8 Reports APIs

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/v1/reports/attendance` | Admin/Manager | Attendance report |
| GET | `/api/v1/reports/timesheet` | Admin/Manager | Timesheet report |
| GET | `/api/v1/reports/leaves` | Admin/Manager | Leave report |
| GET | `/api/v1/reports/projects` | Admin/Manager | Project allocation report |

## 10. Validation Rules

### Employee Validation
- FirstName and LastName are required.
- Email is required, valid, and unique.
- PhoneNumber is required and max 15 characters.
- Gender must be M, F, or O.
- DOB must make employee at least 18 years old.
- DOJ cannot be before DOB + 18 years.
- DepartmentId and RoleId must exist.

### Attendance Validation
- EmpId must exist.
- WorkMode must be WFO, WFH, or Hybrid.
- Check-in is allowed only once per date.
- Check-out cannot happen without check-in.
- Check-out cannot be before check-in.

### Leave Validation
- LeaveType is required.
- FromDate and ToDate are required.
- FromDate <= ToDate.
- Overlapping leaves are not allowed.
- Pending leave can be cancelled.
- Approved/rejected leave cannot be cancelled by employee.

### Project Validation
- ProjectName is required.
- EndDate cannot be earlier than StartDate.
- ClientId must exist if provided.

## 11. Exception Handling Design

### Custom Exceptions

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

public class UnauthorizedAccessAppException : Exception
{
    public UnauthorizedAccessAppException(string message) : base(message) { }
}
```

### Global Exception Middleware

```csharp
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "An unexpected error occurred.",
                errors = new[] { ex.Message }
            });
        }
    }
}
```

## 12. Frontend LLD

### 12.1 Angular Folder Structure

```text
/src/app
  /auth
    login
    auth.service.ts
    auth.guard.ts
    auth.interceptor.ts

  /employees
    employee-list
    employee-form
    employee-detail
    employee.service.ts
    employee.model.ts

  /attendance
    check-in-out
    monthly-attendance
    attendance.service.ts
    attendance.model.ts

  /leaves
    apply-leave
    my-leaves
    leave-approvals
    leave.service.ts
    leave.model.ts

  /departments
    department-list
    department-form
    department.service.ts

  /projects
    project-list
    project-form
    allocation-form
    project.service.ts

  /dashboard
    admin-dashboard
    manager-dashboard
    employee-dashboard
    dashboard.service.ts

  /reports
    attendance-report
    timesheet-report
    leave-report

  /shared
    components
    services
    models
    interceptors
    validators
```

### 12.2 Angular Services

```typescript
@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private baseUrl = environment.apiUrl + '/employees';

  constructor(private http: HttpClient) {}

  getEmployees(params: any): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.baseUrl, { params });
  }

  getEmployeeById(id: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.baseUrl}/${id}`);
  }

  createEmployee(payload: CreateEmployee): Observable<Employee> {
    return this.http.post<Employee>(this.baseUrl, payload);
  }

  updateEmployee(id: number, payload: UpdateEmployee): Observable<Employee> {
    return this.http.put<Employee>(`${this.baseUrl}/${id}`, payload);
  }
}
```

### 12.3 RxJS State Example

```typescript
@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  setUser(user: User | null): void {
    this.currentUserSubject.next(user);
  }
}
```

### 12.4 Angular Validation Example

```typescript
this.employeeForm = this.fb.group({
  firstName: ['', [Validators.required, Validators.maxLength(50)]],
  lastName: ['', [Validators.required, Validators.maxLength(50)]],
  email: ['', [Validators.required, Validators.email]],
  phoneNumber: ['', [Validators.required, Validators.maxLength(15)]],
  gender: ['', Validators.required],
  dob: ['', Validators.required],
  doj: ['', Validators.required],
  departmentId: ['', Validators.required],
  roleId: ['', Validators.required]
});
```

## 13. Authentication Flow

```text
User enters username/password
        |
        v
Angular AuthService sends POST /auth/login
        |
        v
API validates username and password hash
        |
        v
API generates JWT with UserId and Role
        |
        v
Angular stores token securely
        |
        v
HTTP Interceptor attaches token to API calls
        |
        v
API authorizes request using JWT and role
```

## 14. Attendance Flow

```text
Employee clicks Check In
        |
        v
Frontend sends EmpId + WorkMode
        |
        v
API checks if attendance already exists for today
        |
        v
Create Attendance record with CheckIn timestamp
        |
        v
Return success response

Employee clicks Check Out
        |
        v
API finds today's attendance record
        |
        v
Set CheckOut timestamp
        |
        v
Calculate TotalHours
        |
        v
Return updated attendance
```

## 15. Leave Approval Flow

```text
Employee/Manager/Admin applies leave for self
        |
        v
API validates dates and overlapping leaves
        |
        v
Employee/Manager leave saved with Pending status; Admin leave saved as Approved
        |
        v
Manager sees Employee requests; Admin sees Employee and Manager requests
        |
        v
Manager/Admin approves or rejects according to applicant role
        |
        v
Leave status updated with ApprovedBy and ApprovedOn
```

## 16. Test Case Design

### 16.1 Employee Module Test Cases

| Test Case | Input | Expected Result |
|---|---|---|
| Create valid employee | Valid employee data | Employee created |
| Duplicate email | Existing email | Validation error |
| Invalid email | Wrong email format | Validation error |
| Under 18 DOB | DOB less than 18 years | Validation error |
| Search by department | DepartmentId | Employees returned |
| Deactivate employee | Valid EmployeeId | Status becomes Inactive |

### 16.2 Attendance Module Test Cases

| Test Case | Input | Expected Result |
|---|---|---|
| Valid check-in | EmpId, WorkMode | Attendance created |
| Duplicate check-in | Same EmpId/date | Error |
| Check-out without check-in | EmpId | Error |
| Valid check-out | Existing check-in | CheckOut and TotalHours updated |
| Monthly attendance | EmpId, month, year | Attendance list returned |

### 16.3 Leave Module Test Cases

| Test Case | Input | Expected Result |
|---|---|---|
| Apply valid leave | Valid date range | Leave Pending |
| FromDate after ToDate | Invalid range | Error |
| Overlapping leave | Existing leave range | Error |
| Cancel pending leave | Pending leave ID | Leave cancelled |
| Approve leave | Manager approves | Status Approved |
| Reject leave | Manager rejects | Status Rejected |

### 16.4 Project Module Test Cases

| Test Case | Input | Expected Result |
|---|---|---|
| Create project | Valid data | Project created |
| Invalid date range | EndDate before StartDate | Error |
| Assign employee | EmpId + ProjectId | Allocation created |
| Duplicate active allocation | Same EmpId + ProjectId | Error |
| Deactivate allocation | AllocationId | Status false |

### 16.5 Auth Module Test Cases

| Test Case | Input | Expected Result |
|---|---|---|
| Valid login | Correct credentials | JWT returned |
| Invalid login | Wrong password | Unauthorized |
| Access protected API without token | No JWT | 401 Unauthorized |
| Access admin API as employee | Employee token | 403 Forbidden |

## 17. Crystal Reports Design

Reports to implement:
- Attendance Report
- Monthly Timesheet Report
- Leave Report
- Employee Master Report
- Project Allocation Report

Report filters:
- Date range
- Department
- Employee
- Project
- Leave status

Output formats:
- PDF
- Excel
- Print preview

## 18. CI/CD Pipeline Design

### API Build Pipeline

```yaml
trigger:
  branches:
    include:
      - main
      - dev

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '8.x'

- script: dotnet restore
  displayName: Restore packages

- script: dotnet build --configuration Release --no-restore
  displayName: Build API

- script: dotnet test --configuration Release --no-build
  displayName: Run unit tests

- script: dotnet publish WMS.API/WMS.API.csproj --configuration Release --output $(Build.ArtifactStagingDirectory)
  displayName: Publish API

- task: PublishBuildArtifacts@1
  inputs:
    PathtoPublish: '$(Build.ArtifactStagingDirectory)'
    ArtifactName: 'wms-api'
```

### Angular Build Pipeline

```yaml
trigger:
  branches:
    include:
      - main
      - dev

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: NodeTool@0
  inputs:
    versionSpec: '20.x'

- script: npm install
  workingDirectory: WMS.Frontend
  displayName: Install dependencies

- script: npm run build -- --configuration production
  workingDirectory: WMS.Frontend
  displayName: Build Angular app

- task: PublishBuildArtifacts@1
  inputs:
    PathtoPublish: 'WMS.Frontend/dist'
    ArtifactName: 'wms-frontend'
```

## 19. Environment Configuration

### Backend
Use:
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`
- Azure Key Vault for production secrets.

Important keys:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Issuer": "",
    "Audience": "",
    "Secret": ""
  },
  "AllowedOrigins": [""]
}
```

### Frontend
Use:
- `environment.ts`
- `environment.development.ts`
- `environment.production.ts`

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://wms-api.azurewebsites.net/api/v1'
};
```

## 20. Coding Guidelines

- Use PascalCase for C# classes and methods.
- Use camelCase for local variables and TypeScript variables.
- Use meaningful names for services, repositories, DTOs, and components.
- Keep controllers thin.
- Put business logic in services.
- Use async/await for database operations.
- Do not expose entities directly from APIs.
- Use DTOs for request/response models.
- Use centralized exception handling.
- Use logging for critical operations.
- Write unit tests before merging PRs.
