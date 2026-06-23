using System.Text;
using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Reports;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Data;

namespace WMS.Infrastructure.Services;

public class ReportService : IReportService
{
    private const int LinesPerPage = 42;
    private readonly WmsDbContext _dbContext;

    public ReportService(WmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<byte[]> GenerateAttendanceReportAsync(AttendanceReportRequestDto request, string currentUserRole, int currentEmployeeId, CancellationToken cancellationToken = default)
    {
        var fromDate = request.FromDate.Date;
        var toDate = request.ToDate.Date;

        if (fromDate == default || toDate == default)
        {
            throw new InvalidOperationException("From date and to date are required.");
        }

        if (fromDate > toDate)
        {
            throw new InvalidOperationException("From date cannot be after to date.");
        }

        var query = _dbContext.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
            .Include(a => a.Employee)
                .ThenInclude(e => e.Role)
            .Where(a => a.AttendanceDate >= fromDate && a.AttendanceDate <= toDate);

        if (IsEmployeeRole(currentUserRole))
        {
            query = query.Where(a => a.EmpId == currentEmployeeId);
        }
        else if (IsManagerRole(currentUserRole))
        {
            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId, cancellationToken)
                ?? throw new InvalidOperationException("Current employee not found.");

            query = query.Where(a =>
                a.EmpId == currentEmployeeId ||
                (a.Employee.DepartmentId == currentEmployee.DepartmentId && a.Employee.Role.RoleName == nameof(UserRole.Employee)));
        }

        if (request.EmployeeId.HasValue && request.EmployeeId.Value > 0)
        {
            query = query.Where(a => a.EmpId == request.EmployeeId.Value);
        }

        if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
        {
            query = query.Where(a => a.Employee.DepartmentId == request.DepartmentId.Value);
        }

        var records = await query
            .OrderBy(a => a.AttendanceDate)
            .ThenBy(a => a.Employee.FirstName)
            .ThenBy(a => a.Employee.LastName)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var lines = BuildReportLines(request.ReportType, fromDate, toDate, records);
        return BuildPdf(lines);
    }

    private async Task<Employee?> GetCurrentEmployeeAsync(int currentEmployeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.EmployeeId == currentEmployeeId, cancellationToken);
    }

    private static List<string> BuildReportLines(string? reportType, DateTime fromDate, DateTime toDate, IReadOnlyCollection<Attendance> records)
    {
        var title = string.IsNullOrWhiteSpace(reportType) ? "Attendance Report" : $"{reportType.Trim()} Report";
        var lines = new List<string>
        {
            title,
            $"Date Range: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
            $"Generated On: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC",
            $"Records: {records.Count}",
            string.Empty,
            "Date        Employee                  Department        In     Out    Hours  Mode"
        };

        if (records.Count == 0)
        {
            lines.Add("No records found for the selected filters.");
            return lines;
        }

        foreach (var record in records)
        {
            var employeeName = $"{record.Employee.FirstName} {record.Employee.LastName}".Trim();
            var checkIn = record.CheckIn.ToString("HH:mm");
            var checkOut = record.CheckOut?.ToString("HH:mm") ?? "--";
            var totalHours = record.TotalHours?.ToString("0.##") ?? "--";
            lines.Add($"{record.AttendanceDate:yyyy-MM-dd}  {TrimForReport(employeeName, 24),-24}  {TrimForReport(record.Employee.Department.DepartmentName, 16),-16}  {checkIn,-5}  {checkOut,-5}  {totalHours,5}  {record.WorkMode}");
        }

        if (records.Count == 1000)
        {
            lines.Add(string.Empty);
            lines.Add("Only the first 1000 records are included. Narrow the filters for a smaller report.");
        }

        return lines;
    }

    private static byte[] BuildPdf(IReadOnlyList<string> lines)
    {
        var pages = lines
            .Chunk(LinesPerPage)
            .Select(pageLines => BuildPageContent(pageLines))
            .ToList();

        if (pages.Count == 0)
        {
            pages.Add(BuildPageContent(new[] { "No report data." }));
        }

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>"
        };

        var fontObjectId = 3 + pages.Count * 2;
        var pageObjectIds = Enumerable.Range(0, pages.Count).Select(i => 3 + i * 2).ToList();
        objects.Add($"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pages.Count} >>");

        for (var index = 0; index < pages.Count; index++)
        {
            var pageObjectId = 3 + index * 2;
            var contentObjectId = pageObjectId + 1;
            var content = pages[index];
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 {fontObjectId} 0 R >> >> /Contents {contentObjectId} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");

        var builder = new StringBuilder();
        var offsets = new List<int> { 0 };
        builder.Append("%PDF-1.4\n");

        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(index + 1).Append(" 0 obj\n").Append(objects[index]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n");
        builder.Append("0 ").Append(objects.Count + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer\n");
        builder.Append("<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        builder.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string BuildPageContent(IEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        builder.Append("BT\n/F1 10 Tf\n50 750 Td\n14 TL\n");

        foreach (var line in lines)
        {
            builder.Append('(').Append(EscapePdfText(NormalizeAscii(line))).Append(") Tj\nT*\n");
        }

        builder.Append("ET");
        return builder.ToString();
    }

    private static string TrimForReport(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string NormalizeAscii(string value)
    {
        return value.Replace("–", "-").Replace("—", "-").Replace("’", "'").Replace("“", "\"").Replace("”", "\"");
    }

    private static string EscapePdfText(string value)
    {
        return value.Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)");
    }

    private static bool IsEmployeeRole(string currentUserRole)
    {
        return string.Equals(currentUserRole, nameof(UserRole.Employee), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsManagerRole(string currentUserRole)
    {
        return string.Equals(currentUserRole, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase);
    }
}
