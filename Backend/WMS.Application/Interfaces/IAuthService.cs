using WMS.Application.DTOs.Auth;

namespace WMS.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
