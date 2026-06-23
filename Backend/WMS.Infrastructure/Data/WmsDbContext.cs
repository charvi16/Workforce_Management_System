using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Data;

public class WmsDbContext : DbContext
{
    public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Leave> Leaves => Set<Leave>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<EmployeeProjectAllocation> EmployeeProjectAllocations => Set<EmployeeProjectAllocation>();
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var seedCreatedOn = new DateTime(2026, 6, 9, 10, 6, 12, 862, DateTimeKind.Utc).AddTicks(8730);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Email)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Username)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.FirstName);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.LastName);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.DepartmentId);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.RoleId);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Status);

        modelBuilder.Entity<Employee>()
            .HasIndex(e => new { e.DepartmentId, e.RoleId });

        modelBuilder.Entity<Employee>()
            .HasIndex(e => new { e.DepartmentId, e.Status });

        modelBuilder.Entity<Department>()
            .HasIndex(d => d.DepartmentName);

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.RoleName);

        modelBuilder.Entity<UserLogin>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<UserLogin>()
            .HasIndex(u => u.EmployeeId)
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.EmpId, a.AttendanceDate })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.AttendanceDate);

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.EmpId);

        modelBuilder.Entity<Leave>()
            .HasIndex(l => new { l.EmpId, l.Status, l.FromDate });

        modelBuilder.Entity<Leave>()
            .HasIndex(l => l.EmpId);

        modelBuilder.Entity<Leave>()
            .HasIndex(l => l.Status);

        modelBuilder.Entity<Leave>()
            .HasIndex(l => l.FromDate);

        modelBuilder.Entity<Leave>()
            .HasIndex(l => l.ToDate);

        modelBuilder.Entity<Leave>()
            .HasIndex(l => l.AppliedOn);

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.EmpId, a.AttendanceDate, a.CheckIn, a.CheckOut });

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.Status);

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.ClientName);

        modelBuilder.Entity<Client>()
            .Property(c => c.ClientName)
            .HasColumnType("varchar(100)");

        modelBuilder.Entity<Client>()
            .Property(c => c.ClientAddress)
            .HasColumnType("varchar(max)");

        modelBuilder.Entity<Client>()
            .Property(c => c.ClientPhoneNumber)
            .HasColumnType("varchar(15)");

        modelBuilder.Entity<Client>()
            .Property(c => c.ClientLocation)
            .HasColumnType("varchar(100)");

        modelBuilder.Entity<Client>()
            .Property(c => c.Status)
            .ValueGeneratedNever();

        modelBuilder.Entity<Client>()
            .Property(c => c.CreatedOn)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.ClientId);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Status);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.StartDate);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.EndDate);

        modelBuilder.Entity<Project>()
            .Property(p => p.Status)
            .HasDefaultValue("Planned");

        modelBuilder.Entity<Project>()
            .Property(p => p.StartDate)
            .HasColumnType("date");

        modelBuilder.Entity<Project>()
            .Property(p => p.EndDate)
            .HasColumnType("date");

        modelBuilder.Entity<EmployeeProjectAllocation>()
            .HasIndex(a => new { a.EmpId, a.ProjectId, a.Status })
            .IsUnique();

        modelBuilder.Entity<EmployeeProjectAllocation>()
            .HasIndex(a => a.ProjectId);

        modelBuilder.Entity<EmployeeProjectAllocation>()
            .Property(a => a.AssignedOn)
            .HasColumnType("date");

        modelBuilder.Entity<EmployeeProjectAllocation>()
            .Property(a => a.Status)
            .HasDefaultValue(true);

        modelBuilder.Entity<Announcement>()
            .HasIndex(a => a.IsActive);

        modelBuilder.Entity<Announcement>()
            .HasIndex(a => a.CreatedOn);

        modelBuilder.Entity<Announcement>()
            .HasIndex(a => a.TargetRole);

        modelBuilder.Entity<Announcement>()
            .Property(a => a.Title)
            .HasColumnType("varchar(150)");

        modelBuilder.Entity<Announcement>()
            .Property(a => a.Message)
            .HasColumnType("varchar(max)");

        modelBuilder.Entity<Announcement>()
            .Property(a => a.TargetRole)
            .HasColumnType("varchar(20)");

        modelBuilder.Entity<Announcement>()
            .Property(a => a.CreatedOn)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Announcement>()
            .Property(a => a.IsActive)
            .ValueGeneratedNever();

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.CreatedOn);

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.UserId);

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.Username)
            .HasColumnType("varchar(100)");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.Action)
            .HasColumnType("varchar(100)");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.EntityName)
            .HasColumnType("varchar(100)");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.EntityId)
            .HasColumnType("varchar(50)");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.Details)
            .HasColumnType("varchar(max)");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.IpAddress)
            .HasColumnType("varchar(50)");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.CreatedOn)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Role)
            .WithMany(r => r.Employees)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserLogin>()
            .HasOne(u => u.Employee)
            .WithOne(e => e.UserLogin)
            .HasForeignKey<UserLogin>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Employee)
            .WithMany(e => e.Attendances)
            .HasForeignKey(a => a.EmpId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Client)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeProjectAllocation>()
            .Property(a => a.CreatedOn)
            .HasDefaultValueSql("GETDATE()");

        modelBuilder.Entity<EmployeeProjectAllocation>()
            .HasOne(a => a.Employee)
            .WithMany(e => e.ProjectAllocations)
            .HasForeignKey(a => a.EmpId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeProjectAllocation>()
            .HasOne(a => a.Project)
            .WithMany(p => p.EmployeeAllocations)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.Creator)
            .WithMany()
            .HasForeignKey(a => a.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin", Description = "System Administrator" },
            new Role { RoleId = 2, RoleName = "Manager", Description = "Team Manager" },
            new Role { RoleId = 3, RoleName = "Employee", Description = "Regular Employee" });

        modelBuilder.Entity<Department>().HasData(
            new Department { DepartmentId = 1, DepartmentName = "Human Resources", Description = "HR operations and policies", CreatedOn = seedCreatedOn },
            new Department { DepartmentId = 2, DepartmentName = "Engineering", Description = "Software delivery and technical operations", CreatedOn = seedCreatedOn },
            new Department { DepartmentId = 3, DepartmentName = "Finance", Description = "Payroll, budgeting, and accounts", CreatedOn = seedCreatedOn });
    }
}
