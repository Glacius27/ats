namespace VacancyService.Config
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string VacanciesCollection { get; set; } = "Vacancies";
    }
}