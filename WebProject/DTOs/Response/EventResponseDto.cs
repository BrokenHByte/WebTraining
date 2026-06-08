using System.ComponentModel.DataAnnotations;

namespace WebProject.DTOs;

// Копия Event, в валидации ответа смысла пока мало
public class EventResponseDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}