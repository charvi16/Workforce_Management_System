using WMS.Application.DTOs.Clients;
using WMS.Domain.Enums;
using WMS.Infrastructure.Services;

namespace WMS.Tests;

public class ClientServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenClientNameAlreadyExists_Throws()
    {
        await using var context = TestSupport.CreateContext();
        var service = new ClientService(context);

        await service.CreateAsync(new ClientRequestDto { ClientName = "Acme", Status = true }, currentEmployeeId: 1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new ClientRequestDto { ClientName = "Acme", Status = true }, currentEmployeeId: 1));

        Assert.Equal("Client name already exists.", exception.Message);
    }

    [Fact]
    public async Task DeactivateAsync_SetsClientStatusFalse()
    {
        await using var context = TestSupport.CreateContext();
        var service = new ClientService(context);
        var client = await service.CreateAsync(new ClientRequestDto { ClientName = "Acme", Status = true }, currentEmployeeId: 1);

        var deactivated = await service.DeactivateAsync(client.ClientId, currentEmployeeId: 1);

        Assert.False(deactivated.Status);
    }

    [Fact]
    public async Task GetAllAsync_ForEmployee_ReturnsOnlyAccessibleClients()
    {
        await using var context = TestSupport.CreateContext();
        await TestSupport.AddEmployeeAsync(context, 1, nameof(UserRole.Employee));
        var service = new ClientService(context);
        var accessibleClient = await service.CreateAsync(new ClientRequestDto { ClientName = "Accessible", Status = true }, 1);
        await service.CreateAsync(new ClientRequestDto { ClientName = "Hidden", Status = true }, 1);

        var projectService = new ProjectService(context);
        await projectService.CreateAsync(new()
        {
            ProjectName = "Assigned",
            ClientId = accessibleClient.ClientId,
            Status = "Active",
            MemberIds = new[] { 1 }
        }, currentEmployeeId: 1);

        var page = await service.GetAllAsync(nameof(UserRole.Employee), 1, null, null);

        Assert.Single(page.Items);
        Assert.Equal("Accessible", page.Items[0].ClientName);
    }
}
