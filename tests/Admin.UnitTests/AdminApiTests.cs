using eShop.Admin.API.Apis;

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
}
