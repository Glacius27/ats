namespace ats_recruitment_service.Models;

public class Offer
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }   // FK
    public Application Application { get; set; } = null!;

    public string PositionTitle { get; set; } = null!;
    public decimal Salary { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}