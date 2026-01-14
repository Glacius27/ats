namespace ats_candidate_service.DTO;

public class CandidateCreateRequest
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    
    public string Status { get; set; } = "active";
    public IFormFile? Resume { get; set; }  // файл приходит в multipart/form-data
    public string? VacancyId { get; set; }  // ID вакансии, на которую откликнулся кандидат
}
