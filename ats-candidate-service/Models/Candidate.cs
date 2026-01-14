using System.ComponentModel.DataAnnotations;

namespace CandidateService.Models
{
    public class Candidate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
        
        public string Status { get; set; } = "active";

        // Имя файла в MinIO
        public string? ResumeFileName { get; set; }
        
        // ID вакансии, на которую откликнулся кандидат
        public string? VacancyId { get; set; }
    }
}