using System.Text.Json.Serialization;

namespace TaskBoard.Api.Models;

public class TaskItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "todo";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public record TaskStatusUpdate([property: JsonPropertyName("status")] string Status);
