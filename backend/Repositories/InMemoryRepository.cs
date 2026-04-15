using System.Collections.Concurrent;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Repositories;

public class InMemoryRepository : IRepository
{
    private readonly ConcurrentDictionary<string, Project> _projects = new();
    private readonly ConcurrentDictionary<string, TaskItem> _tasks = new();

    public Task<List<Project>> GetProjectsAsync() =>
        Task.FromResult(_projects.Values.OrderByDescending(p => p.CreatedAt).ToList());

    public Task<Project> CreateProjectAsync(Project project)
    {
        _projects[project.Id] = project;
        return Task.FromResult(project);
    }

    public Task DeleteProjectAsync(string id)
    {
        _projects.TryRemove(id, out _);
        foreach (var task in _tasks.Values.Where(t => t.ProjectId == id).ToList())
            _tasks.TryRemove(task.Id, out _);
        return Task.CompletedTask;
    }

    public Task<List<TaskItem>> GetTasksAsync(string projectId) =>
        Task.FromResult(_tasks.Values
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.CreatedAt)
            .ToList());

    public Task<TaskItem> CreateTaskAsync(TaskItem task)
    {
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<TaskItem> UpdateTaskStatusAsync(string id, string projectId, string status)
    {
        if (!_tasks.TryGetValue(id, out var task))
            throw new KeyNotFoundException($"Task {id} not found");
        task.Status = status;
        return Task.FromResult(task);
    }

    public Task DeleteTaskAsync(string id, string projectId)
    {
        _tasks.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
