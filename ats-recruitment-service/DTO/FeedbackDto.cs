namespace ats_recruitment_service.DTO;

public class FeedbackDto
{
    public int ApplicationId { get; set; }
    public string AuthorId { get; set; }
    public string Comments { get; set; }
    public int Score { get; set; }
}