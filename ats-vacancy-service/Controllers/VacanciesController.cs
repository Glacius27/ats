using ats_vacancy_service.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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
                Location = request.Location,
                Department = request.Department ?? string.Empty,
                RecruiterId = request.RecruiterId ?? string.Empty,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            await _repository.CreateAsync(vacancy);

            var response = new VacancyResponse
            {
                Id = vacancy.Id,
                Title = vacancy.Title,
                Description = vacancy.Description,
                Location = vacancy.Location,
                Department = vacancy.Department,
                Status = vacancy.Status,
                RecruiterId = vacancy.RecruiterId,
                CreatedAt = vacancy.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = vacancy.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateVacancyRequest request)
        {
            Console.WriteLine($"[VacancyService] Update (PUT) called for id: {id}");
            Console.WriteLine($"[VacancyService] Request: Title={request?.Title}, Description={request?.Description}, Location={request?.Location}, Department={request?.Department}, Status={request?.Status}");
            Console.WriteLine($"[VacancyService] ModelState.IsValid: {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"[VacancyService] ModelState errors:");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            if (request == null)
            {
                Console.WriteLine("[VacancyService] Request is null");
                return BadRequest("Request body is required");
            }

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                Console.WriteLine($"[VacancyService] Vacancy with id {id} not found");
                return NotFound();
            }

            Console.WriteLine($"[VacancyService] Existing vacancy: Title={existing.Title}, Status={existing.Status}");

            // Обновляем только переданные поля
            if (request.Title != null) existing.Title = request.Title;
            if (request.Description != null) existing.Description = request.Description;
            if (request.Location != null) existing.Location = request.Location;
            if (request.Department != null) existing.Department = request.Department;
            if (request.Status != null) existing.Status = request.Status;

            Console.WriteLine($"[VacancyService] Updated vacancy: Title={existing.Title}, Status={existing.Status}");

            await _repository.UpdateAsync(id, existing);
            return Ok(existing);
        }

        /// <summary>
        /// Частично обновляет вакансию (PATCH)
        /// </summary>
        /// <param name="id">ID вакансии</param>
        /// <param name="request">Данные для обновления (все поля опциональны)</param>
        /// <returns>Обновленная вакансия</returns>
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(Vacancy), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Patch(string id, [FromBody] UpdateVacancyRequest request)
        {
            Console.WriteLine($"[VacancyService] Patch called for id: {id}");
            Console.WriteLine($"[VacancyService] Request: Title={request?.Title}, Description={request?.Description}, Location={request?.Location}, Department={request?.Department}, Status={request?.Status}");

            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            // Обновляем только переданные поля
            if (request.Title != null) existing.Title = request.Title;
            if (request.Description != null) existing.Description = request.Description;
            if (request.Location != null) existing.Location = request.Location;
            if (request.Department != null) existing.Department = request.Department;
            if (request.Status != null) existing.Status = request.Status;

            await _repository.UpdateAsync(id, existing);
            return Ok(existing);
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