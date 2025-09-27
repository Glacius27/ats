namespace ats_recruitment_service.Models;

public class Application
{
    public int Id { get; set; }
    public string CandidateId { get; set; } = null!;
    public string VacancyId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>(); 
}