using ats_vacancy_service.DTO;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using VacancyService.Data;
using VacancyService.Models;

namespace VacancyService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VacanciesController : ControllerBase
    {
        private readonly VacancyRepository _repository;

        public VacanciesController(VacancyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Vacancy>>> Get() =>
            await _repository.GetAllAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<Vacancy>> GetById(string id)
        {
            var vacancy = await _repository.GetByIdAsync(id);
            if (vacancy == null) return NotFound();
            return vacancy;
        }

        // [HttpPost]
        // public async Task<IActionResult> Create(Vacancy vacancy)
        // {
        //     await _repository.CreateAsync(vacancy);
        //     return CreatedAtAction(nameof(GetById), new { id = vacancy.Id }, vacancy);
        // }
        
        [HttpPost]
        public async Task<ActionResult<VacancyResponse>> CreateVacancy([FromBody] CreateVacancyRequest request)
        {
            var vacancy = new Vacancy
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = request.Title,
                Description = request.Description,
                Location = request.Location
            };

            await _repository.CreateAsync(vacancy);

            var response = new VacancyResponse
            {
                Id = vacancy.Id,
                Title = vacancy.Title,
                Description = vacancy.Description,
                Location = vacancy.Location
            };

            return CreatedAtAction(nameof(GetById), new { id = vacancy.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, Vacancy vacancy)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            vacancy.Id = id;
            await _repository.UpdateAsync(id, vacancy);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}