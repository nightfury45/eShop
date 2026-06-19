using System.Net;
using System.Net.Http.Json;
using eShop.Admin.API.Apis;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShop.Admin.FunctionalTests;

public sealed class AdminApiTests : IClassFixture<AdminApiFixture>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    public AdminApiTests(AdminApiFixture fixture)
    {
        _webApplicationFactory = fixture;
    }

    [Fact]
    public async Task PingReturnsOk()
    {
        var httpClient = _webApplicationFactory.CreateDefaultClient();

        var response = await httpClient.GetAsync("/api/admin/ping", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PingResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
    }

    [Fact]
    public async Task MeReturnsUnauthorizedWithoutAdminPrincipal()
    {
        var httpClient = _webApplicationFactory.CreateDefaultClient();

        var response = await httpClient.GetAsync("/api/admin/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MeReturnsAdministratorForAuthorizedRequest()
    {
        var httpClient = _webApplicationFactory.CreateDefaultClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/me");
        request.Headers.Add(AdminAutoAuthorizeMiddleware.AdminHeader, "true");

        var response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AdminUser>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Contains("Administrator", body.Roles);
    }
}
