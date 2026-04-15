using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;
using Xunit;
using TaskStatus = TaskBoard.Api.Models.TaskStatus;

namespace TaskBoard.Api.Tests;

public abstract class ApiTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly HttpClient Client;

    protected ApiTestBase(WebApplicationFactory<Program> factory)
    {
        Client = factory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    var existing = services.SingleOrDefault(d => d.ServiceType == typeof(IRepository));
                    if (existing is not null) services.Remove(existing);
                    services.AddSingleton<IRepository, InMemoryRepository>();
                }))
            .CreateClient();
    }

    protected async Task<Project> CreateProjectAsync(string name = "Test Project", string description = "")
    {
        var response = await Client.PostAsJsonAsync("/api/projects", new { name, description });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Project>())!;
    }

    protected async Task<TaskItem> CreateTaskAsync(string projectId, string title = "Test Task", string status = TaskStatus.Todo)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks",
            new { title, description = "", status });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskItem>())!;
    }
}
