namespace CandidateService.Models;

public class CandidateCreateRequest
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public IFormFile? Resume { get; set; }  // файл приходит в multipart/form-data
}
