using Microsoft.AspNetCore.Mvc;
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

        [HttpPost]
        public async Task<IActionResult> Create(Vacancy vacancy)
        {
            await _repository.CreateAsync(vacancy);
            return CreatedAtAction(nameof(GetById), new { id = vacancy.Id }, vacancy);
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