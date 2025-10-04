// Models/InterviewFeedback.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InterviewService.Models;

public class InterviewFeedback
{
    public int Id { get; set; }

    [Required]
    public int InterviewId { get; set; }

    [JsonIgnore]
    public Interview? Interview { get; set; }   // навигационное свойство не требуется в DTO

    [Required, MaxLength(100)]
    public string AuthorId { get; set; } = null!;

    [Required, MaxLength(2000)]
    public string Comments { get; set; } = null!;

    [Range(1, 5)]
    public int Score { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}