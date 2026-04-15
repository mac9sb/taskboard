using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;
using Xunit;

namespace TaskBoard.Api.Tests;

public class TasksApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TasksApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    var existing = services.SingleOrDefault(d => d.ServiceType == typeof(IRepository));
                    if (existing is not null) services.Remove(existing);
                    services.AddSingleton<IRepository, InMemoryRepository>();
                }))
            .CreateClient();
    }

    // ── GET /api/projects/{projectId}/tasks ───────────────────────────────────

    [Fact]
    public async Task GetTasks_NoTasksYet_Returns200WithEmptyArray()
    {
        var project = await CreateProjectAsync();

        var response = await _client.GetAsync($"/api/projects/{project.Id}/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<TaskItem>>()).Should().BeEmpty();
    }

    // ── POST /api/projects/{projectId}/tasks ──────────────────────────────────

    [Fact]
    public async Task PostTask_ValidPayload_Returns201WithCreatedTask()
    {
        var project = await CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "Write tests", description = "Cover all endpoints" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.Content.ReadFromJsonAsync<TaskItem>();
        task!.Id.Should().NotBeNullOrEmpty();
        task.Title.Should().Be("Write tests");
        task.ProjectId.Should().Be(project.Id);
        task.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task PostTask_StatusDefaultsToTodo_WhenNotProvided()
    {
        var project = await CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "No status", description = "" });
        var task = await response.Content.ReadFromJsonAsync<TaskItem>();

        task!.Status.Should().Be("todo");
    }

    [Fact]
    public async Task PostTask_RespectsExplicitStatus()
    {
        var project = await CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "Already done", description = "", status = "done" });
        var task = await response.Content.ReadFromJsonAsync<TaskItem>();

        task!.Status.Should().Be("done");
    }

    [Fact]
    public async Task PostTask_SetsLocationHeader()
    {
        var project = await CreateProjectAsync();

        var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "Location task", description = "" });

        response.Headers.Location!.ToString()
            .Should().StartWith($"/api/projects/{project.Id}/tasks/");
    }

    [Fact]
    public async Task PostTask_AppearsInSubsequentGetTasks()
    {
        var project = await CreateProjectAsync();
        var created = await CreateTaskAsync(project.Id, "Appears in list");

        var tasks = await _client.GetFromJsonAsync<List<TaskItem>>(
            $"/api/projects/{project.Id}/tasks");

        tasks.Should().ContainSingle(t => t.Id == created.Id);
    }

    // ── PATCH /api/projects/{projectId}/tasks/{id} ────────────────────────────

    [Fact]
    public async Task PatchTaskStatus_ValidUpdate_Returns200WithUpdatedTask()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Patch me");

        var response = await _client.PatchAsJsonAsync(
            $"/api/projects/{project.Id}/tasks/{task.Id}",
            new { status = "in-progress" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<TaskItem>())!.Status.Should().Be("in-progress");
    }

    [Fact]
    public async Task PatchTaskStatus_PersistsChange_VisibleInGetTasks()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Persist patch");

        await _client.PatchAsJsonAsync(
            $"/api/projects/{project.Id}/tasks/{task.Id}",
            new { status = "done" });

        var tasks = await _client.GetFromJsonAsync<List<TaskItem>>(
            $"/api/projects/{project.Id}/tasks");
        tasks!.Single(t => t.Id == task.Id).Status.Should().Be("done");
    }

    [Fact]
    public async Task PatchTaskStatus_CanTransitionFreely_NotJustForward()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Multi-step", "done");

        // Move backwards: done → todo
        var response = await _client.PatchAsJsonAsync(
            $"/api/projects/{project.Id}/tasks/{task.Id}",
            new { status = "todo" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<TaskItem>())!.Status.Should().Be("todo");
    }

    // ── DELETE /api/projects/{projectId}/tasks/{id} ───────────────────────────

    [Fact]
    public async Task DeleteTask_ExistingTask_Returns204()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Delete me");

        var response = await _client.DeleteAsync($"/api/projects/{project.Id}/tasks/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTask_RemovedFromSubsequentGetTasks()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Also delete");

        await _client.DeleteAsync($"/api/projects/{project.Id}/tasks/{task.Id}");

        var tasks = await _client.GetFromJsonAsync<List<TaskItem>>(
            $"/api/projects/{project.Id}/tasks");
        tasks.Should().NotContain(t => t.Id == task.Id);
    }

    [Fact]
    public async Task DeleteTask_NonExistentId_Returns204()
    {
        var project = await CreateProjectAsync();

        var response = await _client.DeleteAsync(
            $"/api/projects/{project.Id}/tasks/ghost-task-id");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProject_CascadesMakesTasksGone()
    {
        var project = await CreateProjectAsync();
        await CreateTaskAsync(project.Id, "Orphan");

        await _client.DeleteAsync($"/api/projects/{project.Id}");

        var tasks = await _client.GetFromJsonAsync<List<TaskItem>>(
            $"/api/projects/{project.Id}/tasks");
        tasks.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Project> CreateProjectAsync(string name = "Test Project")
    {
        var r = await _client.PostAsJsonAsync("/api/projects", new { name, description = "" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<Project>())!;
    }

    private async Task<TaskItem> CreateTaskAsync(
        string projectId, string title = "Test Task", string status = "todo")
    {
        var r = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks",
            new { title, description = "", status });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<TaskItem>())!;
    }
}
