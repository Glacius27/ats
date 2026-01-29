namespace ats_recruitment_service.DTO;

public class CreateApplicationRequest
{
    public string CandidateId { get; set; } = null!;
    public string VacancyId { get; set; } = null!;
    public string? Status { get; set; }
}
