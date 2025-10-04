using System.ComponentModel.DataAnnotations;

namespace InterviewService.DTO;

public class InterviewCreateDto
{
    [Required] public int ApplicationId { get; set; }
    [Required] public DateTime ScheduledAt { get; set; }
    [Required, MaxLength(200)] public string Interviewer { get; set; } = null!;
    [MaxLength(200)] public string? Location { get; set; }
    [Required, MaxLength(50)] public string Status { get; set; } = "Scheduled";
    public string? Notes { get; set; }
}

public class InterviewUpdateDto // для PUT (полная замена)
{
    [Required] public int ApplicationId { get; set; }
    [Required] public DateTime ScheduledAt { get; set; }
    [Required, MaxLength(200)] public string Interviewer { get; set; } = null!;
    [MaxLength(200)] public string? Location { get; set; }
    [Required, MaxLength(50)] public string Status { get; set; } = "Scheduled";
    public string? Notes { get; set; }
}

public class InterviewPatchDto // для PATCH (частичное обновление)
{
    public int? ApplicationId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? Interviewer { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

public class InterviewDto // то, что отдаём наружу
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Interviewer { get; set; } = null!;
    public string? Location { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
}