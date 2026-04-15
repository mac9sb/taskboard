using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;
using Xunit;

namespace TaskBoard.Api.Tests;

public class ProjectsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProjectsApiTests(WebApplicationFactory<Program> factory)
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

    // ── GET /api/projects ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetProjects_EmptyStore_Returns200WithEmptyArray()
    {
        var response = await _client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<Project>>()).Should().BeEmpty();
    }

    // ── POST /api/projects ────────────────────────────────────────────────────

    [Fact]
    public async Task PostProject_ValidPayload_Returns201WithCreatedProject()
    {
        var response = await _client.PostAsJsonAsync("/api/projects",
            new { name = "Alpha", description = "First project" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Project>();
        created!.Id.Should().NotBeNullOrEmpty();
        created.Name.Should().Be("Alpha");
        created.Description.Should().Be("First project");
        created.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task PostProject_SetsLocationHeader()
    {
        var response = await _client.PostAsJsonAsync("/api/projects",
            new { name = "Beta", description = "" });

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/projects/");
    }

    [Fact]
    public async Task PostProject_AppearsInSubsequentGetProjects()
    {
        var created = await CreateProjectAsync("Gamma");

        var projects = await _client.GetFromJsonAsync<List<Project>>("/api/projects");

        projects.Should().Contain(p => p.Id == created.Id && p.Name == "Gamma");
    }

    // ── DELETE /api/projects/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteProject_ExistingProject_Returns204()
    {
        var created = await CreateProjectAsync("ToDelete");

        var response = await _client.DeleteAsync($"/api/projects/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProject_RemovedFromSubsequentGetProjects()
    {
        var created = await CreateProjectAsync("AlsoDelete");

        await _client.DeleteAsync($"/api/projects/{created.Id}");

        var projects = await _client.GetFromJsonAsync<List<Project>>("/api/projects");
        projects.Should().NotContain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task DeleteProject_NonExistentId_Returns204()
    {
        var response = await _client.DeleteAsync("/api/projects/does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<Project> CreateProjectAsync(string name = "Test", string description = "")
    {
        var response = await _client.PostAsJsonAsync("/api/projects", new { name, description });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Project>())!;
    }
}
