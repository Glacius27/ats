namespace ats_vacancy_service.DTO;

public class VacancyResponse
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Location { get; set; } = null!;
    public string? Department { get; set; }
    public string? Status { get; set; }
    public string? RecruiterId { get; set; }
    public DateTime? CreatedAt { get; set; }
}