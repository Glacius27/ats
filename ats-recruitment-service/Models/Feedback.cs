namespace ats_recruitment_service.Models;

public class Feedback
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигация
    public Application Application { get; set; } = null!;
}
