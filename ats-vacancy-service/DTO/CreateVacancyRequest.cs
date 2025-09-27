namespace ats_vacancy_service.DTO;

public class CreateVacancyRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Location { get; set; } = null!;
}