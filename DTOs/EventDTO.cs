using System.ComponentModel.DataAnnotations;

namespace WebTraining.DTOs;

public class EventDto : IValidatableObject
{
    [Required]
    public int Id { get; set; }
    
    [Required(AllowEmptyStrings = true, ErrorMessage = "Заголовок обязателен для заполнения")]
    [StringLength(200, MinimumLength = 1,
        ErrorMessage = "Заголовок события должен быть от 1 до 200 символов")]
    public required string Title { get; set; }
    
    public string? Description { get; set; }
    
    [Required(AllowEmptyStrings = true, ErrorMessage = "Дата начала обязательна к заполнению")]
    public DateTime StartAt { get; set; }
    
    [Required(AllowEmptyStrings = true, ErrorMessage = "Дата окончания обязательна к заполнению")]
    public DateTime EndAt { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
            yield return new ValidationResult("Дата окончания должна быть позже даты начала");
    }
}