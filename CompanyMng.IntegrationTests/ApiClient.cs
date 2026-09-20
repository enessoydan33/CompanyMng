using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CompanyMng.IntegrationTests;

internal static class ApiClient
{
    public const string Password = "Test1234!";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string Unique(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}@test.local";

    public static HttpRequestMessage Request(
        HttpMethod method,
        string url,
        string? token = null,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    public static async Task<T> ReadJson<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
        Assert.NotNull(value);
        return value!;
    }

    public static async Task<(string AccessToken, string RefreshToken, int StatusCode)> RegisterAndLogin(
        HttpClient client,
        string? email = null,
        string name = "Admin",
        string surname = "User",
        string? companyName = null)
    {
        email ??= Unique("admin");
        companyName ??= $"Co-{Guid.NewGuid():N}";

        var register = await client.SendAsync(Request(
            HttpMethod.Post,
            "/api/Auth/register",
            body: new
            {
                name,
                surname,
                email,
                password = Password,
                companyName
            }));

        Assert.Equal(200, (int)register.StatusCode);

        var login = await client.SendAsync(Request(
            HttpMethod.Post,
            "/api/Auth/login",
            body: new { email, password = Password }));

        var payload = await ReadJson<LoginResponse>(login);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));

        return (payload.AccessToken, payload.RefreshToken, (int)login.StatusCode);
    }

    public static async Task<(int Id, string Email, string AccessToken)> CreateCompanyUserAndLogin(
        HttpClient client,
        string adminToken,
        string name)
    {
        var email = Unique(name.ToLowerInvariant());

        var create = await client.SendAsync(Request(
            HttpMethod.Post,
            "/api/Auth/CreateUser",
            adminToken,
            new
            {
                name,
                surname = "Test",
                email,
                password = Password
            }));

        Assert.Equal(200, (int)create.StatusCode);
        var created = await ReadJson<CreatedUserResponse>(create);

        var login = await client.SendAsync(Request(
            HttpMethod.Post,
            "/api/Auth/login",
            body: new { email, password = Password }));

        Assert.Equal(200, (int)login.StatusCode);
        var tokens = await ReadJson<LoginResponse>(login);

        return (created.Id, email, tokens.AccessToken);
    }

    public static async Task<int> CreateProject(HttpClient client, string adminToken, string? name = null)
    {
        var response = await client.SendAsync(Request(
            HttpMethod.Post,
            "/api/Project/create",
            adminToken,
            new { name = name ?? $"Project-{Guid.NewGuid():N}", description = "d" }));

        Assert.Equal(200, (int)response.StatusCode);
        var project = await ReadJson<IdResponse>(response);
        return project.Id;
    }
}

internal sealed class LoginResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}

internal sealed class RefreshResponse
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}

internal sealed class CreatedUserResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public int CompanyId { get; set; }
}

internal sealed class IdResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public int? AssignedUserId { get; set; }
    public int Status { get; set; }
}

internal sealed class HistoryItem
{
    public int Id { get; set; }
    public int OldStatus { get; set; }
    public int NewStatus { get; set; }
    public string ChangedBy { get; set; } = null!;
}

internal sealed class CommentResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = null!;
    public string? UserName { get; set; }
}
