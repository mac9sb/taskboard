using TaskBoard.Api.Models;

namespace TaskBoard.Api.Repositories;

public interface IRepository
{
    Task<List<Project>> GetProjectsAsync();
    Task<Project> CreateProjectAsync(Project project);
    Task DeleteProjectAsync(string id);

    Task<List<TaskItem>> GetTasksAsync(string projectId);
    Task<TaskItem> CreateTaskAsync(TaskItem task);
    Task<TaskItem> UpdateTaskStatusAsync(string id, string projectId, string status);
    Task DeleteTaskAsync(string id, string projectId);
}
