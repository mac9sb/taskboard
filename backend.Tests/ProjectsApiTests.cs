using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskBoard.Api.Models;
using Xunit;

namespace TaskBoard.Api.Tests;

public class ProjectsApiTests(WebApplicationFactory<Program> factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task GetProjects_EmptyStore_Returns200WithEmptyArray()
    {
        var response = await Client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<Project>>()).Should().BeEmpty();
    }

    [Fact]
    public async Task PostProject_ValidPayload_Returns201WithCreatedProject()
    {
        var response = await Client.PostAsJsonAsync("/api/projects",
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
        var response = await Client.PostAsJsonAsync("/api/projects",
            new { name = "Beta", description = "" });

        response.Headers.Location!.ToString().Should().StartWith("/api/projects/");
    }

    [Fact]
    public async Task PostProject_AppearsInSubsequentGetProjects()
    {
        var created = await CreateProjectAsync("Gamma");

        var projects = await Client.GetFromJsonAsync<List<Project>>("/api/projects");

        projects.Should().Contain(p => p.Id == created.Id && p.Name == "Gamma");
    }

    [Fact]
    public async Task DeleteProject_ExistingProject_Returns204()
    {
        var created = await CreateProjectAsync("ToDelete");

        var response = await Client.DeleteAsync($"/api/projects/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProject_RemovedFromSubsequentGetProjects()
    {
        var created = await CreateProjectAsync("AlsoDelete");

        await Client.DeleteAsync($"/api/projects/{created.Id}");

        var projects = await Client.GetFromJsonAsync<List<Project>>("/api/projects");
        projects.Should().NotContain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task DeleteProject_NonExistentId_Returns204()
    {
        var response = await Client.DeleteAsync("/api/projects/does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
