// Models/Interview.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InterviewService.Models;

public class Interview
{
    public int Id { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    [Required, MaxLength(200)]
    public string Interviewer { get; set; } = null!;

    [MaxLength(200)]
    public string? Location { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Scheduled";

    public string? Notes { get; set; }

    // Не сериализуем обратные ссылки, чтобы не ловить циклы
    [JsonIgnore]
    public ICollection<InterviewFeedback> Feedbacks { get; set; } = new List<InterviewFeedback>();
}