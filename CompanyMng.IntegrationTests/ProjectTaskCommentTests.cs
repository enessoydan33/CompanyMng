using System.Net;
using CompanyMng.Enum;
using TaskStatusEnum = CompanyMng.Enum.TaskStatus;

namespace CompanyMng.IntegrationTests;

[Collection("Api")]
public sealed class ProjectTaskCommentTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ProjectTaskCommentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_create_project_normal_user_cannot()
    {
        using var client = _factory.CreateClient();
        var (adminToken, _, _) = await ApiClient.RegisterAndLogin(client, name: "ProjAdmin");
        var (_, _, userToken) = await ApiClient.CreateCompanyUserAndLogin(client, adminToken, "Member");

        var adminCreate = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Project/create",
            adminToken,
            new { name = $"P-{Guid.NewGuid():N}", description = "ok" }));
        Assert.Equal(HttpStatusCode.OK, adminCreate.StatusCode);
        var project = await ApiClient.ReadJson<IdResponse>(adminCreate);
        Assert.True(project.Id > 0);

        var userCreate = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            "/api/Project/create",
            userToken,
            new { name = $"Denied-{Guid.NewGuid():N}", description = "no" }));
        Assert.Equal(HttpStatusCode.Forbidden, userCreate.StatusCode);

        var userTask = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/Task/{project.Id}",
            userToken,
            new { title = "Should fail", description = "x", priority = (int)TaskPriority.Medium }));
        Assert.Equal(HttpStatusCode.Forbidden, userTask.StatusCode);
    }

    [Fact]
    public async Task Admin_can_add_project_member_but_duplicate_is_rejected()
    {
        using var client = _factory.CreateClient();
        var (adminToken, _, _) = await ApiClient.RegisterAndLogin(client, name: "MemAdmin");
        var (userId, _, _) = await ApiClient.CreateCompanyUserAndLogin(client, adminToken, "ToAdd");
        var projectId = await ApiClient.CreateProject(client, adminToken);

        var add = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/ProjectMember/{projectId}",
            adminToken,
            new { userId }));
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        var duplicate = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/ProjectMember/{projectId}",
            adminToken,
            new { userId }));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task Task_can_be_assigned_only_to_project_members()
    {
        using var client = _factory.CreateClient();
        var (adminToken, _, _) = await ApiClient.RegisterAndLogin(client, name: "AssignAdmin");
        var (memberId, _, _) = await ApiClient.CreateCompanyUserAndLogin(client, adminToken, "Assignee");
        var (outsiderId, _, _) = await ApiClient.CreateCompanyUserAndLogin(client, adminToken, "Outsider");
        var projectId = await ApiClient.CreateProject(client, adminToken);

        var addMember = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/ProjectMember/{projectId}",
            adminToken,
            new { userId = memberId }));
        Assert.Equal(HttpStatusCode.OK, addMember.StatusCode);

        var assigned = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/Task/{projectId}",
            adminToken,
            new
            {
                title = "Member task",
                description = "ok",
                assignedUserId = memberId,
                priority = (int)TaskPriority.Medium
            }));
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        var task = await ApiClient.ReadJson<IdResponse>(assigned);
        Assert.Equal(memberId, task.AssignedUserId);

        var notMember = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/Task/{projectId}",
            adminToken,
            new
            {
                title = "Invalid assign",
                description = "no",
                assignedUserId = outsiderId,
                priority = (int)TaskPriority.Medium
            }));
        Assert.Equal(HttpStatusCode.BadRequest, notMember.StatusCode);
    }

    [Fact]
    public async Task Task_status_is_allowed_for_assignee_and_admin_forbidden_for_other_user()
    {
        using var client = _factory.CreateClient();
        var (adminToken, _, _) = await ApiClient.RegisterAndLogin(client, name: "StatusAdmin");
        var (userAId, _, userAToken) = await ApiClient.CreateCompanyUserAndLogin(client, adminToken, "Alice");
        var (_, _, userBToken) = await ApiClient.CreateCompanyUserAndLogin(client, adminToken, "Bob");
        var projectId = await ApiClient.CreateProject(client, adminToken);

        await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/ProjectMember/{projectId}",
            adminToken,
            new { userId = userAId }));

        var createTask = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/Task/{projectId}",
            adminToken,
            new
            {
                title = "Status task",
                assignedUserId = userAId,
                priority = (int)TaskPriority.Medium
            }));
        Assert.Equal(HttpStatusCode.OK, createTask.StatusCode);
        var task = await ApiClient.ReadJson<IdResponse>(createTask);

        var alicePatch = await client.SendAsync(ApiClient.Request(
            HttpMethod.Patch,
            $"/api/Task/{task.Id}/status",
            userAToken,
            new { status = (int)TaskStatusEnum.InStatus }));
        Assert.Equal(HttpStatusCode.OK, alicePatch.StatusCode);

        var bobPatch = await client.SendAsync(ApiClient.Request(
            HttpMethod.Patch,
            $"/api/Task/{task.Id}/status",
            userBToken,
            new { status = (int)TaskStatusEnum.Completed }));
        Assert.Equal(HttpStatusCode.Forbidden, bobPatch.StatusCode);

        var adminPatch = await client.SendAsync(ApiClient.Request(
            HttpMethod.Patch,
            $"/api/Task/{task.Id}/status",
            adminToken,
            new { status = (int)TaskStatusEnum.Completed }));
        Assert.Equal(HttpStatusCode.OK, adminPatch.StatusCode);

        var historyResponse = await client.SendAsync(ApiClient.Request(
            HttpMethod.Get,
            $"/api/Task/{task.Id}/history",
            adminToken));
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await ApiClient.ReadJson<List<HistoryItem>>(historyResponse);

        Assert.Contains(history, h =>
            h.OldStatus == (int)TaskStatusEnum.Todo &&
            h.NewStatus == (int)TaskStatusEnum.InStatus &&
            h.ChangedBy.Contains("Alice"));
        Assert.Contains(history, h =>
            h.OldStatus == (int)TaskStatusEnum.InStatus &&
            h.NewStatus == (int)TaskStatusEnum.Completed &&
            h.ChangedBy.Contains("StatusAdmin"));
    }

    [Fact]
    public async Task Comments_can_be_created_listed_and_deleted_by_owner_or_admin()
    {
        using var client = _factory.CreateClient();
        var (adminToken, _, _) = await ApiClient.RegisterAndLogin(client, name: "CommentAdmin");
        var (userAId, _, userAToken) = await ApiClient.CreateCompanyUserAndLogin(client, adminToken, "Writer");
        var (_, _, userBToken) = await ApiClient.CreateCompanyUserAndLogin(client, adminToken, "Other");
        var projectId = await ApiClient.CreateProject(client, adminToken);

        await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/ProjectMember/{projectId}",
            adminToken,
            new { userId = userAId }));

        var createTask = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/Task/{projectId}",
            adminToken,
            new { title = "Commented task", priority = (int)TaskPriority.Low }));
        var task = await ApiClient.ReadJson<IdResponse>(createTask);

        var createComment = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/Comment/{task.Id}",
            userAToken,
            new { content = "hello from writer" }));
        Assert.Equal(HttpStatusCode.OK, createComment.StatusCode);
        var comment = await ApiClient.ReadJson<CommentResponse>(createComment);

        var list = await client.SendAsync(ApiClient.Request(
            HttpMethod.Get,
            $"/api/Comment/{task.Id}",
            userAToken));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var comments = await ApiClient.ReadJson<List<CommentResponse>>(list);
        Assert.Contains(comments, c => c.Id == comment.Id && c.Content == "hello from writer");

        var otherDelete = await client.SendAsync(ApiClient.Request(
            HttpMethod.Delete,
            $"/api/Comment/{comment.Id}",
            userBToken));
        Assert.Equal(HttpStatusCode.Forbidden, otherDelete.StatusCode);

        var ownerDelete = await client.SendAsync(ApiClient.Request(
            HttpMethod.Delete,
            $"/api/Comment/{comment.Id}",
            userAToken));
        Assert.Equal(HttpStatusCode.OK, ownerDelete.StatusCode);

        var adminComment = await client.SendAsync(ApiClient.Request(
            HttpMethod.Post,
            $"/api/Comment/{task.Id}",
            userAToken,
            new { content = "admin will delete this" }));
        var toDelete = await ApiClient.ReadJson<CommentResponse>(adminComment);

        var adminDelete = await client.SendAsync(ApiClient.Request(
            HttpMethod.Delete,
            $"/api/Comment/{toDelete.Id}",
            adminToken));
        Assert.Equal(HttpStatusCode.OK, adminDelete.StatusCode);
    }
}
