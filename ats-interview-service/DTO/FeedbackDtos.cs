using System.ComponentModel.DataAnnotations;

namespace InterviewService.DTO;

public class FeedbackCreateDto
{
    [Required] public string AuthorId { get; set; } = null!;
    [Required] public string Comments { get; set; } = null!;
    [Range(1,5)] public int Score { get; set; }
}

public class FeedbackDto
{
    public int Id { get; set; }
    public int InterviewId { get; set; }
    public string AuthorId { get; set; } = null!;
    public string Comments { get; set; } = null!;
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
}