namespace WebProject.Models;

public record Event
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
}