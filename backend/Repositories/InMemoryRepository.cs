using System.Collections.Concurrent;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Repositories;

public class InMemoryRepository : IRepository
{
    private readonly ConcurrentDictionary<string, Project> _projects = new();

    // Secondary index: projectId → (taskId → task)
    // Eliminates full-dictionary scans in GetTasksAsync and DeleteProjectAsync.
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, TaskItem>> _tasksByProject = new();

    public Task<List<Project>> GetProjectsAsync() =>
        Task.FromResult(_projects.Values.OrderByDescending(p => p.CreatedAt).ToList());

    public Task<Project> CreateProjectAsync(Project project)
    {
        _projects[project.Id] = project;
        return Task.FromResult(project);
    }

    public Task<Project> UpdateProjectAsync(string id, string name, string description)
    {
        if (!_projects.TryGetValue(id, out var project))
            throw new KeyNotFoundException($"Project {id} not found");

        project.Name = name;
        project.Description = description;
        return Task.FromResult(project);
    }

    public Task DeleteProjectAsync(string id)
    {
        _projects.TryRemove(id, out _);
        _tasksByProject.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<List<TaskItem>> GetTasksAsync(string projectId)
    {
        if (!_tasksByProject.TryGetValue(projectId, out var bucket))
            return Task.FromResult(new List<TaskItem>());

        return Task.FromResult(bucket.Values.OrderBy(t => t.CreatedAt).ToList());
    }

    public Task<TaskItem> CreateTaskAsync(TaskItem task)
    {
        var bucket = _tasksByProject.GetOrAdd(task.ProjectId, _ => new ConcurrentDictionary<string, TaskItem>());
        bucket[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<TaskItem> UpdateTaskStatusAsync(string id, string projectId, string status)
    {
        if (!_tasksByProject.TryGetValue(projectId, out var bucket) || !bucket.TryGetValue(id, out var task))
            throw new KeyNotFoundException($"Task {id} not found");

        task.Status = status;
        return Task.FromResult(task);
    }

    public Task DeleteTaskAsync(string id, string projectId)
    {
        if (_tasksByProject.TryGetValue(projectId, out var bucket))
            bucket.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
