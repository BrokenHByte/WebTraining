using System.ComponentModel.DataAnnotations;

namespace WebTraining.DTOs;

public class EventDto : IValidatableObject
{
    public int Id { get; set; }
    [Required(AllowEmptyStrings = true, ErrorMessage = "Заголовок обязателен для заполнения")]
    public required string Title { get; set; }
    public string? Description { get; set; }
    [Required]
    public DateTime StartAt { get; set; }
    [Required]
    public DateTime EndAt { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
            yield return new ValidationResult("Дата окончания должна быть после даты начала");
    }
}