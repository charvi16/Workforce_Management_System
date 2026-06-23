using WMS.Application.Common;
using WMS.Application.DTOs.Announcements;

namespace WMS.Application.Interfaces;

public interface IAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetAllAsync(string currentUserRole, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> GetByIdAsync(int announcementId, string currentUserRole, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> CreateAsync(AnnouncementRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> UpdateAsync(int announcementId, AnnouncementRequestDto request, int currentEmployeeId, CancellationToken cancellationToken = default);
    Task<AnnouncementDto> DeactivateAsync(int announcementId, int currentEmployeeId, CancellationToken cancellationToken = default);
}
