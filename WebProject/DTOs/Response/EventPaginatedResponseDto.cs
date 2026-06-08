namespace WebProject.DTOs;

public class EventPaginatedResponseDto
{
    public int TotalCountEvents { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public required List<EventResponseDto> Events { get; set; }
}