using System.Net;

namespace CompanyMng.IntegrationTests;

[Collection("Api")]
public sealed class TenantIsolationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public TenantIsolationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Company_B_admin_cannot_read_or_mutate_company_A_resources()
    {
        using var client = _factory.CreateClient();

        var secretName = $"SecretProject-{Guid.NewGuid():N}";
        var (adminA, _, _) = await ApiClient.RegisterAndLogin(
            client,
            name: "AdminA",
            companyName: $"CompanyA-{Guid.NewGuid():N}");
        var projectAId = await ApiClient.CreateProject(client, adminA, secretName);

        var createTask = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/Task/{projectAId}",
            adminA,
            new { title = "Secret task", description = "tenant-a-only", priority = 2 }));
        Assert.Equal(HttpStatusCode.OK, createTask.StatusCode);
        var taskA = await ApiClient.ReadJson<IdResponse>(createTask);

        var (adminB, _, _) = await ApiClient.RegisterAndLogin(
            client,
            name: "AdminB",
            companyName: $"CompanyB-{Guid.NewGuid():N}");

        var getProject = await client.SendAsync(ApiClient.Request(
            HttpMethod.Get,
            $"/api/Project/{projectAId}",
            adminB));
        Assert.Equal(HttpStatusCode.NotFound, getProject.StatusCode);
        var projectBody = await getProject.Content.ReadAsStringAsync();
        Assert.DoesNotContain(secretName, projectBody);

        var getTask = await client.SendAsync(ApiClient.Request(
            HttpMethod.Get,
            $"/api/Task/{taskA.Id}",
            adminB));
        Assert.Equal(HttpStatusCode.NotFound, getTask.StatusCode);
        var taskBody = await getTask.Content.ReadAsStringAsync();
        Assert.DoesNotContain("tenant-a-only", taskBody);
        Assert.DoesNotContain("Secret task", taskBody);

        var updateProject = await client.SendAsync(ApiClient.Request(
            HttpMethod.Put,
            $"/api/Project/{projectAId}",
            adminB,
            new { name = "Hijacked", description = "no" }));
        Assert.Equal(HttpStatusCode.NotFound, updateProject.StatusCode);

        var deleteProject = await client.SendAsync(ApiClient.Request(
            HttpMethod.Delete,
            $"/api/Project/{projectAId}",
            adminB));
        Assert.Equal(HttpStatusCode.NotFound, deleteProject.StatusCode);

        var stillThere = await client.SendAsync(ApiClient.Request(
            HttpMethod.Get,
            $"/api/Project/{projectAId}",
            adminA));
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
        var owned = await ApiClient.ReadJson<IdResponse>(stillThere);
        Assert.Equal(secretName, owned.Name);
    }
}
