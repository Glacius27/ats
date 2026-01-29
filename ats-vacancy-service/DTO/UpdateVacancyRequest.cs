using System.Text.Json.Serialization;

namespace ats_vacancy_service.DTO;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
public class UpdateVacancyRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Department { get; set; }
    public string? Status { get; set; }
}
