using Microsoft.Azure.Cosmos;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Repositories;

public class CosmosRepository : IRepository
{
    private readonly CosmosClient _client;
    private readonly string _databaseName;
    private Container _projects = null!;
    private Container _tasks = null!;

    public CosmosRepository(CosmosClient client, IConfiguration config)
    {
        _client = client;
        _databaseName = config["CosmosDb:DatabaseName"] ?? "taskboard";
    }

    public async Task InitializeAsync()
    {
        var db = (await _client.CreateDatabaseIfNotExistsAsync(_databaseName)).Database;
        _projects = (await db.CreateContainerIfNotExistsAsync("projects", "/id")).Container;
        _tasks = (await db.CreateContainerIfNotExistsAsync("tasks", "/projectId")).Container;
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        var query = _projects.GetItemQueryIterator<Project>("SELECT * FROM c ORDER BY c.createdAt DESC");
        var results = new List<Project>();
        while (query.HasMoreResults)
            results.AddRange(await query.ReadNextAsync());
        return results;
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        var response = await _projects.CreateItemAsync(project, new PartitionKey(project.Id));
        return response.Resource;
    }

    public async Task<Project> UpdateProjectAsync(string id, string name, string description)
    {
        var response = await _projects.ReadItemAsync<Project>(id, new PartitionKey(id));
        var project = response.Resource;
        project.Name = name;
        project.Description = description;
        var updated = await _projects.ReplaceItemAsync(project, id, new PartitionKey(id));
        return updated.Resource;
    }

    public async Task DeleteProjectAsync(string id)
    {
        await _projects.DeleteItemAsync<Project>(id, new PartitionKey(id));
        foreach (var task in await GetTasksAsync(id))
            await _tasks.DeleteItemAsync<TaskItem>(task.Id, new PartitionKey(id));
    }

    public async Task<List<TaskItem>> GetTasksAsync(string projectId)
    {
        var query = _tasks.GetItemQueryIterator<TaskItem>(
            new QueryDefinition("SELECT * FROM c WHERE c.projectId = @projectId ORDER BY c.createdAt ASC")
                .WithParameter("@projectId", projectId));
        var results = new List<TaskItem>();
        while (query.HasMoreResults)
            results.AddRange(await query.ReadNextAsync());
        return results;
    }

    public async Task<TaskItem> CreateTaskAsync(TaskItem task)
    {
        var response = await _tasks.CreateItemAsync(task, new PartitionKey(task.ProjectId));
        return response.Resource;
    }

    public async Task<TaskItem> UpdateTaskStatusAsync(string id, string projectId, string status)
    {
        var response = await _tasks.ReadItemAsync<TaskItem>(id, new PartitionKey(projectId));
        var task = response.Resource;
        task.Status = status;
        var updated = await _tasks.ReplaceItemAsync(task, id, new PartitionKey(projectId));
        return updated.Resource;
    }

    public async Task DeleteTaskAsync(string id, string projectId)
    {
        await _tasks.DeleteItemAsync<TaskItem>(id, new PartitionKey(projectId));
    }
}
