using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskBoard.Api.Models;
using Xunit;
using TaskStatus = TaskBoard.Api.Models.TaskStatus;

namespace TaskBoard.Api.Tests;

public class TasksApiTests(WebApplicationFactory<Program> factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task GetTasks_NoTasksYet_Returns200WithEmptyArray()
    {
        var project = await CreateProjectAsync();

        var response = await Client.GetAsync($"/api/projects/{project.Id}/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<TaskItem>>()).Should().BeEmpty();
    }

    [Fact]
    public async Task PostTask_ValidPayload_Returns201WithCreatedTask()
    {
        var project = await CreateProjectAsync();

        var response = await Client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
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

        var response = await Client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "No status", description = "" });
        var task = await response.Content.ReadFromJsonAsync<TaskItem>();

        task!.Status.Should().Be(TaskStatus.Todo);
    }

    [Fact]
    public async Task PostTask_RespectsExplicitStatus()
    {
        var project = await CreateProjectAsync();

        var response = await Client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "Already done", description = "", status = TaskStatus.Done });
        var task = await response.Content.ReadFromJsonAsync<TaskItem>();

        task!.Status.Should().Be(TaskStatus.Done);
    }

    [Fact]
    public async Task PostTask_SetsLocationHeader()
    {
        var project = await CreateProjectAsync();

        var response = await Client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks",
            new { title = "Location task", description = "" });

        response.Headers.Location!.ToString()
            .Should().StartWith($"/api/projects/{project.Id}/tasks/");
    }

    [Fact]
    public async Task PostTask_AppearsInSubsequentGetTasks()
    {
        var project = await CreateProjectAsync();
        var created = await CreateTaskAsync(project.Id, "Appears in list");

        var tasks = await Client.GetFromJsonAsync<List<TaskItem>>(
            $"/api/projects/{project.Id}/tasks");

        tasks.Should().ContainSingle(t => t.Id == created.Id);
    }

    [Fact]
    public async Task PatchTaskStatus_ValidUpdate_Returns200WithUpdatedTask()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Patch me");

        var response = await Client.PatchAsJsonAsync(
            $"/api/projects/{project.Id}/tasks/{task.Id}",
            new { status = TaskStatus.InProgress });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<TaskItem>())!.Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public async Task PatchTaskStatus_PersistsChange_VisibleInGetTasks()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Persist patch");

        await Client.PatchAsJsonAsync(
            $"/api/projects/{project.Id}/tasks/{task.Id}",
            new { status = TaskStatus.Done });

        var tasks = await Client.GetFromJsonAsync<List<TaskItem>>(
            $"/api/projects/{project.Id}/tasks");
        tasks!.Single(t => t.Id == task.Id).Status.Should().Be(TaskStatus.Done);
    }

    [Fact]
    public async Task PatchTaskStatus_CanTransitionFreely_NotJustForward()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Multi-step", TaskStatus.Done);

        var response = await Client.PatchAsJsonAsync(
            $"/api/projects/{project.Id}/tasks/{task.Id}",
            new { status = TaskStatus.Todo });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<TaskItem>())!.Status.Should().Be(TaskStatus.Todo);
    }

    [Fact]
    public async Task DeleteTask_ExistingTask_Returns204()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Delete me");

        var response = await Client.DeleteAsync($"/api/projects/{project.Id}/tasks/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTask_RemovedFromSubsequentGetTasks()
    {
        var project = await CreateProjectAsync();
        var task = await CreateTaskAsync(project.Id, "Also delete");

        await Client.DeleteAsync($"/api/projects/{project.Id}/tasks/{task.Id}");

        var tasks = await Client.GetFromJsonAsync<List<TaskItem>>(
            $"/api/projects/{project.Id}/tasks");
        tasks.Should().NotContain(t => t.Id == task.Id);
    }

    [Fact]
    public async Task DeleteTask_NonExistentId_Returns204()
    {
        var project = await CreateProjectAsync();

        var response = await Client.DeleteAsync(
            $"/api/projects/{project.Id}/tasks/ghost-task-id");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProject_CascadesMakesTasksGone()
    {
        var project = await CreateProjectAsync();
        await CreateTaskAsync(project.Id, "Orphan");

        await Client.DeleteAsync($"/api/projects/{project.Id}");

        var tasks = await Client.GetFromJsonAsync<List<TaskItem>>(
            $"/api/projects/{project.Id}/tasks");
        tasks.Should().BeEmpty();
    }
}
