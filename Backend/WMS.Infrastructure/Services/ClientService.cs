using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Clients;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class ClientService : IClientService
{
    private readonly WmsDbContext _dbContext;

    public ClientService(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ClientDto>> GetAllAsync(string currentUserRole, int currentEmployeeId, string? search, bool? status, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
        var accessibleClientIds = GetAccessibleClientIdsQuery(currentUserRole, currentEmployee);

        var query = _dbContext.Clients
            .AsNoTracking()
            .Where(c => IsAdminRole(currentUserRole) || accessibleClientIds.Contains(c.ClientId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var likeTerm = $"%{EscapeLikePattern(term)}%";
            query = query.Where(c =>
                EF.Functions.Like(c.ClientName, likeTerm, @"\") ||
                EF.Functions.Like(c.ClientAddress ?? string.Empty, likeTerm, @"\") ||
                EF.Functions.Like(c.ClientLocation ?? string.Empty, likeTerm, @"\") ||
                EF.Functions.Like(c.ClientPhoneNumber ?? string.Empty, likeTerm, @"\"));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var projected = query.Select(c => new ClientDto
        {
            ClientId = c.ClientId,
            ClientName = c.ClientName,
            ClientAddress = c.ClientAddress,
            ClientPhoneNumber = c.ClientPhoneNumber,
            ClientLocation = c.ClientLocation,
            Status = c.Status,
            ProjectCount = c.Projects.Count
        });

        var totalCount = await projected.CountAsync(cancellationToken);
        var items = await projected
            .OrderBy(c => c.ClientName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ClientDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ClientDto> GetByIdAsync(int clientId, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken);
        var accessibleClientIds = GetAccessibleClientIdsQuery(currentUserRole, currentEmployee);

        var query = _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.ClientId == clientId && (IsAdminRole(currentUserRole) || accessibleClientIds.Contains(c.ClientId)));

        var client = await query
            .Select(c => new ClientDto
            {
                ClientId = c.ClientId,
                ClientName = c.ClientName,
                ClientAddress = c.ClientAddress,
                ClientPhoneNumber = c.ClientPhoneNumber,
                ClientLocation = c.ClientLocation,
                Status = c.Status,
                ProjectCount = c.Projects.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        return client ?? throw new InvalidOperationException("Client not found.");
    }

    public async Task<ClientDto> CreateAsync(ClientRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        await EnsureClientSchemaAsync(cancellationToken);

        var clientName = request.ClientName.Trim();
        if (string.IsNullOrWhiteSpace(clientName))
        {
            throw new InvalidOperationException("Client name is required.");
        }

        if (await _dbContext.Clients.AnyAsync(c => c.ClientName == clientName, cancellationToken))
        {
            throw new InvalidOperationException("Client name already exists.");
        }

        var client = new Client
        {
            ClientName = clientName,
            ClientAddress = string.IsNullOrWhiteSpace(request.ClientAddress) ? null : request.ClientAddress.Trim(),
            ClientPhoneNumber = NormalizePhoneNumber(request.ClientPhoneNumber),
            ClientLocation = string.IsNullOrWhiteSpace(request.ClientLocation) ? null : request.ClientLocation.Trim(),
            Status = request.Status,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = null
        };

        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(client);
    }

    public async Task<ClientDto> UpdateAsync(int clientId, ClientRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        await EnsureClientSchemaAsync(cancellationToken);

        var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken)
            ?? throw new InvalidOperationException("Client not found.");

        var clientName = request.ClientName.Trim();
        if (string.IsNullOrWhiteSpace(clientName))
        {
            throw new InvalidOperationException("Client name is required.");
        }

        if (await _dbContext.Clients.AnyAsync(c => c.ClientId != clientId && c.ClientName == clientName, cancellationToken))
        {
            throw new InvalidOperationException("Client name already exists.");
        }

        client.ClientName = clientName;
        client.ClientAddress = string.IsNullOrWhiteSpace(request.ClientAddress) ? null : request.ClientAddress.Trim();
        client.ClientPhoneNumber = NormalizePhoneNumber(request.ClientPhoneNumber);
        client.ClientLocation = string.IsNullOrWhiteSpace(request.ClientLocation) ? null : request.ClientLocation.Trim();
        client.Status = request.Status;
        client.UpdatedOn = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(client);
    }

    public async Task<ClientDto> DeactivateAsync(int clientId, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        await EnsureClientSchemaAsync(cancellationToken);

        var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken)
            ?? throw new InvalidOperationException("Client not found.");

        client.Status = false;
        client.UpdatedOn = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(client);
    }

    private IQueryable<int> GetAccessibleClientIdsQuery(string currentUserRole, Employee? currentEmployee)
    {
        if (IsAdminRole(currentUserRole))
        {
            return _dbContext.Clients.Select(c => c.ClientId);
        }

        if (currentEmployee is null)
        {
            return _dbContext.Clients.Where(c => false).Select(c => c.ClientId);
        }

        var accessibleProjectIds = _dbContext.EmployeeProjectAllocations
            .AsNoTracking()
            .Where(a => a.Status && (
                a.EmpId == currentEmployee.EmployeeId ||
                (IsManagerRole(currentUserRole) && a.Employee.DepartmentId == currentEmployee.DepartmentId && a.Employee.Role.RoleName == nameof(UserRole.Employee))))
            .Select(a => a.ProjectId)
            .Distinct();

        return _dbContext.Projects
            .AsNoTracking()
            .Where(p => accessibleProjectIds.Contains(p.ProjectId) && p.ClientId.HasValue)
            .Select(p => p.ClientId!.Value)
            .Distinct();
    }

    private Task<Employee?> GetCurrentEmployeeAsync(int currentEmployeeId, CancellationToken cancellationToken)
    {
        return _dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == currentEmployeeId, cancellationToken);
    }

    private static bool IsAdminRole(string currentUserRole) =>
        string.Equals(currentUserRole, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase);

    private static bool IsManagerRole(string currentUserRole) =>
        string.Equals(currentUserRole, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase);

    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[");

    private static string? NormalizePhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var phoneNumber = value.Trim();
        if (phoneNumber.Length > 15 || phoneNumber.Any(c => !char.IsDigit(c) && c is not '+' and not '-' and not ' ' and not '(' and not ')'))
        {
            throw new InvalidOperationException("Client phone number can contain up to 15 characters: digits, +, spaces, hyphens, and parentheses.");
        }

        return phoneNumber;
    }

    private Task EnsureClientSchemaAsync(CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsSqlServer())
        {
            return Task.CompletedTask;
        }

        return _dbContext.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID('dbo.Clients', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Clients', 'CreatedOn') IS NULL
    BEGIN
        ALTER TABLE dbo.Clients
        ADD CreatedOn datetime2 NOT NULL CONSTRAINT DF_Clients_CreatedOn DEFAULT (GETDATE());
    END

    IF COL_LENGTH('dbo.Clients', 'UpdatedOn') IS NULL
    BEGIN
        ALTER TABLE dbo.Clients
        ADD UpdatedOn datetime2 NULL;
    END

    IF COL_LENGTH('dbo.Clients', 'Status') IS NOT NULL
    BEGIN
        UPDATE dbo.Clients SET Status = 1 WHERE Status IS NULL;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
            WHERE dc.parent_object_id = OBJECT_ID('dbo.Clients')
              AND c.name = 'Status'
        )
        BEGIN
            ALTER TABLE dbo.Clients
            ADD CONSTRAINT DF_Clients_Status DEFAULT (1) FOR Status;
        END
    END

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('dbo.Clients')
          AND c.name = 'ClientPhoneNumber'
          AND t.name NOT IN ('varchar', 'nvarchar', 'char', 'nchar')
    )
    BEGIN
        ALTER TABLE dbo.Clients ALTER COLUMN ClientPhoneNumber varchar(15) NULL;
    END

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID('dbo.Clients')
          AND c.name = 'ClientLocation'
          AND c.max_length > 0
          AND c.max_length < 100
    )
    BEGIN
        ALTER TABLE dbo.Clients ALTER COLUMN ClientLocation varchar(100) NULL;
    END
END
""", cancellationToken);
    }

    private static ClientDto MapToDto(Client client)
    {
        return new ClientDto
        {
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            ClientAddress = client.ClientAddress,
            ClientPhoneNumber = client.ClientPhoneNumber,
            ClientLocation = client.ClientLocation,
            Status = client.Status,
            ProjectCount = client.Projects?.Count ?? 0
        };
    }
}
