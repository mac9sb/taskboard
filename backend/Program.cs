using Microsoft.Azure.Cosmos;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryStore");

if (useInMemory)
{
    builder.Services.AddSingleton<IRepository, InMemoryRepository>();
}
else
{
    builder.Services.AddSingleton<CosmosClient>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var connectionString = config["CosmosDb:ConnectionString"]
            ?? throw new InvalidOperationException("CosmosDb:ConnectionString is required");

        var options = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        if (config.GetValue<bool>("CosmosDb:DisableSslVerification"))
        {
            // vnext emulator uses plain HTTP on port 8081.
            // Disable endpoint discovery so the SDK never follows the metadata redirect
            // to 127.0.0.1 (which resolves to the pod itself inside k8s, not Cosmos DB).
            options.HttpClientFactory = () => new HttpClient(new HttpClientHandler());
            options.ConnectionMode = ConnectionMode.Gateway;
            options.LimitToEndpoint = true;
        }

        return new CosmosClient(connectionString, options);
    });

    builder.Services.AddSingleton<IRepository>(sp =>
    {
        var repo = new CosmosRepository(
            sp.GetRequiredService<CosmosClient>(),
            sp.GetRequiredService<IConfiguration>());

        // Retry initialization — Cosmos DB emulator may still be starting in k8s
        var attempts = 0;
        while (true)
        {
            try
            {
                repo.InitializeAsync().GetAwaiter().GetResult();
                break;
            }
            catch (Exception ex) when (++attempts <= 20)
            {
                Console.WriteLine($"[Cosmos] Not ready, retrying in 5s ({attempts}/20): {ex.Message}");
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        return repo;
    });
}

var app = builder.Build();

app.UseCors();

// Warm up the repository singleton
_ = app.Services.GetRequiredService<IRepository>();

// --- Projects ---
app.MapGet("/api/projects", async (IRepository db) =>
    Results.Ok(await db.GetProjectsAsync()));

app.MapPost("/api/projects", async (Project project, IRepository db) =>
{
    project.Id = Guid.NewGuid().ToString();
    project.CreatedAt = DateTime.UtcNow;
    return Results.Created($"/api/projects/{project.Id}", await db.CreateProjectAsync(project));
});

app.MapPatch("/api/projects/{id}", async (string id, ProjectUpdate update, IRepository db) =>
    Results.Ok(await db.UpdateProjectAsync(id, update.Name, update.Description)));

app.MapDelete("/api/projects/{id}", async (string id, IRepository db) =>
{
    await db.DeleteProjectAsync(id);
    return Results.NoContent();
});

// --- Tasks ---
app.MapGet("/api/projects/{projectId}/tasks", async (string projectId, IRepository db) =>
    Results.Ok(await db.GetTasksAsync(projectId)));

app.MapPost("/api/projects/{projectId}/tasks", async (string projectId, TaskItem task, IRepository db) =>
{
    task.Id = Guid.NewGuid().ToString();
    task.ProjectId = projectId;
    task.CreatedAt = DateTime.UtcNow;
    task.Status = string.IsNullOrEmpty(task.Status) ? TaskBoard.Api.Models.TaskStatus.Todo : task.Status;
    return Results.Created($"/api/projects/{projectId}/tasks/{task.Id}", await db.CreateTaskAsync(task));
});

app.MapPatch("/api/projects/{projectId}/tasks/{id}", async (string projectId, string id, TaskStatusUpdate update, IRepository db) =>
    Results.Ok(await db.UpdateTaskStatusAsync(id, projectId, update.Status)));

app.MapDelete("/api/projects/{projectId}/tasks/{id}", async (string projectId, string id, IRepository db) =>
{
    await db.DeleteTaskAsync(id, projectId);
    return Results.NoContent();
});

app.Run();

// Required for WebApplicationFactory<Program> in tests
public partial class Program { }
