using System.Security.Claims;
using eShop.Admin.API.Apis;
using Microsoft.Extensions.Configuration;

namespace eShop.Admin.UnitTests;

[TestClass]
public class AdminApiTests
{
    [TestMethod]
    public void Ping_returns_ok_status()
    {
        var result = AdminApi.Ping();

        Assert.IsNotNull(result.Value);
        Assert.AreEqual("ok", result.Value.Status);
    }

    [TestMethod]
    public void Ping_returns_a_utc_timestamp()
    {
        var result = AdminApi.Ping();

        Assert.IsNotNull(result.Value);
        Assert.AreEqual(DateTimeKind.Utc, result.Value.Utc.Kind);
    }

    [TestMethod]
    public void GetClientConfig_exposes_adminspa_authority_from_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Identity:Url"] = "https://identity.example",
            })
            .Build();

        var result = AdminApi.GetClientConfig(configuration);

        Assert.IsNotNull(result.Value);
        Assert.AreEqual("https://identity.example", result.Value.Authority);
        Assert.AreEqual("adminspa", result.Value.ClientId);
        Assert.Contains("admin", result.Value.Scope);
    }

    [TestMethod]
    public void GetMe_projects_subject_name_and_roles_from_claims()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "user-123"),
                new Claim("name", "Priya Admin"),
                new Claim("role", "Administrator"),
                new Claim("role", "Auditor"),
            },
            authenticationType: "test"));

        var result = AdminApi.GetMe(principal);

        Assert.IsNotNull(result.Value);
        Assert.AreEqual("user-123", result.Value.Subject);
        Assert.AreEqual("Priya Admin", result.Value.Name);
        CollectionAssert.AreEquivalent(new[] { "Administrator", "Auditor" }, result.Value.Roles);
    }
}
