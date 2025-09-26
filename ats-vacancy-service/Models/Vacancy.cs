namespace VacancyService.Models

{
    public class Vacancy
    {
        public string Id { get; set; } 
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Department { get; set; } = null!;
        public string Status { get; set; } = "Open"; // Open / Closed
        public string RecruiterId { get; set; } = null!; // Ссылка на рекрутера
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}