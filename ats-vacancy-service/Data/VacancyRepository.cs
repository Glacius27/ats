using MongoDB.Driver;
using VacancyService.Models;
using VacancyService.Config;
using Microsoft.Extensions.Options;

namespace VacancyService.Data
{
    public class VacancyRepository
    {
        private readonly IMongoCollection<Vacancy> _vacancies;

        public VacancyRepository(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _vacancies = database.GetCollection<Vacancy>(settings.Value.VacanciesCollection);
        }

        public async Task<List<Vacancy>> GetAllAsync() =>
            await _vacancies.Find(_ => true).ToListAsync();

        public async Task<Vacancy?> GetByIdAsync(string id) =>
            await _vacancies.Find(v => v.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Vacancy vacancy) =>
            await _vacancies.InsertOneAsync(vacancy);

        public async Task UpdateAsync(string id, Vacancy vacancy) =>
            await _vacancies.ReplaceOneAsync(v => v.Id == id, vacancy);

        public async Task DeleteAsync(string id) =>
            await _vacancies.DeleteOneAsync(v => v.Id == id);
    }
}