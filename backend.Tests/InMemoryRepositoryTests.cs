using FluentAssertions;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;
using Xunit;
using TaskStatus = TaskBoard.Api.Models.TaskStatus;

namespace TaskBoard.Api.Tests;

public class InMemoryRepositoryTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static InMemoryRepository CreateRepo() => new();

    private static Project MakeProject(string name = "Test Project") => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        Description = "A description",
        CreatedAt = DateTime.UtcNow
    };

    private static TaskItem MakeTask(string projectId, string title = "Test Task", string status = TaskStatus.Todo) => new()
    {
        Id = Guid.NewGuid().ToString(),
        ProjectId = projectId,
        Title = title,
        Description = "A task description",
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    // ── Projects ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProjectsAsync_EmptyStore_ReturnsEmptyList()
    {
        var repo = CreateRepo();
        var result = await repo.GetProjectsAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProjectAsync_StoresAndReturnsProject()
    {
        var repo = CreateRepo();
        var project = MakeProject();

        var returned = await repo.CreateProjectAsync(project);

        returned.Should().BeEquivalentTo(project);
        var all = await repo.GetProjectsAsync();
        all.Should().ContainSingle(p => p.Id == project.Id);
    }

    [Fact]
    public async Task GetProjectsAsync_ReturnsProjectsOrderedByCreatedAtDescending()
    {
        var repo = CreateRepo();
        var older = MakeProject("Older");
        older.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        var newer = MakeProject("Newer");
        newer.CreatedAt = DateTime.UtcNow;

        await repo.CreateProjectAsync(older);
        await repo.CreateProjectAsync(newer);

        var result = await repo.GetProjectsAsync();
        result[0].Id.Should().Be(newer.Id);
        result[1].Id.Should().Be(older.Id);
    }

    [Fact]
    public async Task DeleteProjectAsync_RemovesProject()
    {
        var repo = CreateRepo();
        var project = MakeProject();
        await repo.CreateProjectAsync(project);

        await repo.DeleteProjectAsync(project.Id);

        var all = await repo.GetProjectsAsync();
        all.Should().NotContain(p => p.Id == project.Id);
    }

    [Fact]
    public async Task DeleteProjectAsync_CascadesTasksForThatProject()
    {
        var repo = CreateRepo();
        var project = MakeProject();
        await repo.CreateProjectAsync(project);
        await repo.CreateTaskAsync(MakeTask(project.Id, "Task 1"));
        await repo.CreateTaskAsync(MakeTask(project.Id, "Task 2"));

        await repo.DeleteProjectAsync(project.Id);

        var tasks = await repo.GetTasksAsync(project.Id);
        tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteProjectAsync_DoesNotRemoveTasksForOtherProjects()
    {
        var repo = CreateRepo();
        var p1 = MakeProject("P1");
        var p2 = MakeProject("P2");
        await repo.CreateProjectAsync(p1);
        await repo.CreateProjectAsync(p2);
        var task = MakeTask(p2.Id);
        await repo.CreateTaskAsync(task);

        await repo.DeleteProjectAsync(p1.Id);

        var p2Tasks = await repo.GetTasksAsync(p2.Id);
        p2Tasks.Should().ContainSingle(t => t.Id == task.Id);
    }

    [Fact]
    public async Task DeleteProjectAsync_NonExistentId_DoesNotThrow()
    {
        var repo = CreateRepo();
        var act = async () => await repo.DeleteProjectAsync("does-not-exist");
        await act.Should().NotThrowAsync();
    }

    // ── Tasks ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasksAsync_NoTasks_ReturnsEmptyList()
    {
        var repo = CreateRepo();
        var result = await repo.GetTasksAsync("any-project-id");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTaskAsync_StoresAndReturnsTask()
    {
        var repo = CreateRepo();
        var project = MakeProject();
        await repo.CreateProjectAsync(project);
        var task = MakeTask(project.Id);

        var returned = await repo.CreateTaskAsync(task);

        returned.Should().BeEquivalentTo(task);
        var all = await repo.GetTasksAsync(project.Id);
        all.Should().ContainSingle(t => t.Id == task.Id);
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsOnlyTasksForRequestedProject()
    {
        var repo = CreateRepo();
        var p1 = MakeProject("P1");
        var p2 = MakeProject("P2");
        await repo.CreateProjectAsync(p1);
        await repo.CreateProjectAsync(p2);
        var taskP1 = MakeTask(p1.Id, "P1 task");
        var taskP2 = MakeTask(p2.Id, "P2 task");
        await repo.CreateTaskAsync(taskP1);
        await repo.CreateTaskAsync(taskP2);

        var result = await repo.GetTasksAsync(p1.Id);

        result.Should().ContainSingle(t => t.Id == taskP1.Id);
        result.Should().NotContain(t => t.Id == taskP2.Id);
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsTasksOrderedByCreatedAtAscending()
    {
        var repo = CreateRepo();
        var project = MakeProject();
        await repo.CreateProjectAsync(project);
        var newer = MakeTask(project.Id, "Newer"); newer.CreatedAt = DateTime.UtcNow;
        var older = MakeTask(project.Id, "Older"); older.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        await repo.CreateTaskAsync(newer);
        await repo.CreateTaskAsync(older);

        var result = await repo.GetTasksAsync(project.Id);

        result[0].Id.Should().Be(older.Id);
        result[1].Id.Should().Be(newer.Id);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_ChangesStatusAndReturnsUpdatedTask()
    {
        var repo = CreateRepo();
        var project = MakeProject();
        await repo.CreateProjectAsync(project);
        var task = MakeTask(project.Id, status: TaskStatus.Todo);
        await repo.CreateTaskAsync(task);

        var updated = await repo.UpdateTaskStatusAsync(task.Id, project.Id, TaskStatus.Done);

        updated.Status.Should().Be(TaskStatus.Done);
        (await repo.GetTasksAsync(project.Id)).Single(t => t.Id == task.Id).Status.Should().Be(TaskStatus.Done);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_UnknownId_ThrowsKeyNotFoundException()
    {
        var repo = CreateRepo();
        var act = async () => await repo.UpdateTaskStatusAsync("ghost-id", "any-project", TaskStatus.Done);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteTaskAsync_RemovesTask()
    {
        var repo = CreateRepo();
        var project = MakeProject();
        await repo.CreateProjectAsync(project);
        var task = MakeTask(project.Id);
        await repo.CreateTaskAsync(task);

        await repo.DeleteTaskAsync(task.Id, project.Id);

        (await repo.GetTasksAsync(project.Id)).Should().NotContain(t => t.Id == task.Id);
    }

    [Fact]
    public async Task DeleteTaskAsync_NonExistentId_DoesNotThrow()
    {
        var repo = CreateRepo();
        var act = async () => await repo.DeleteTaskAsync("ghost-id", "any-project");
        await act.Should().NotThrowAsync();
    }
}
