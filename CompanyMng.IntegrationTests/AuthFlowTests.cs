using System.Net;

namespace CompanyMng.IntegrationTests;

[Collection("Api")]
public sealed class AuthFlowTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_and_login_returns_access_and_refresh_tokens()
    {
        using var client = _factory.CreateClient();

        var (accessToken, refreshToken, status) =
            await ApiClient.RegisterAndLogin(client);

        Assert.Equal(200, status);
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
    }

    [Fact]
    public async Task Refresh_rotates_token_and_rejects_reused_token()
    {
        using var client = _factory.CreateClient();

        var (_, refreshA, _) = await ApiClient.RegisterAndLogin(client);

        var firstRefresh = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Auth/refresh",
            body: new { refreshToken = refreshA }));

        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        var rotated = await ApiClient.ReadJson<RefreshResponse>(firstRefresh);
        Assert.False(string.IsNullOrWhiteSpace(rotated.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(rotated.RefreshToken));
        Assert.NotEqual(refreshA, rotated.RefreshToken);

        var reuseA = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Auth/refresh",
            body: new { refreshToken = refreshA }));

        Assert.Equal(HttpStatusCode.Unauthorized, reuseA.StatusCode);

        var useB = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Auth/refresh",
            body: new { refreshToken = rotated.RefreshToken }));

        Assert.Equal(HttpStatusCode.OK, useB.StatusCode);
        var second = await ApiClient.ReadJson<RefreshResponse>(useB);
        Assert.False(string.IsNullOrWhiteSpace(second.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(second.RefreshToken));
    }

    [Fact]
    public async Task Second_login_revokes_previous_refresh_token()
    {
        using var client = _factory.CreateClient();
        var email = ApiClient.Unique("relogin");
        var company = $"Co-{Guid.NewGuid():N}";

        var register = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Auth/register",
            body: new
            {
                name = "Relogin",
                surname = "Admin",
                email,
                password = ApiClient.Password,
                companyName = company
            }));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login1 = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Auth/login",
            body: new { email, password = ApiClient.Password }));
        Assert.Equal(HttpStatusCode.OK, login1.StatusCode);
        var tokensA = await ApiClient.ReadJson<LoginResponse>(login1);

        var login2 = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Auth/login",
            body: new { email, password = ApiClient.Password }));
        Assert.Equal(HttpStatusCode.OK, login2.StatusCode);
        var tokensB = await ApiClient.ReadJson<LoginResponse>(login2);

        var reuseA = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Auth/refresh",
            body: new { refreshToken = tokensA.RefreshToken }));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseA.StatusCode);

        var useB = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Auth/refresh",
            body: new { refreshToken = tokensB.RefreshToken }));
        Assert.Equal(HttpStatusCode.OK, useB.StatusCode);
    }
}
