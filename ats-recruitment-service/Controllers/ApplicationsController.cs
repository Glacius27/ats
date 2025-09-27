using ats_recruitment_service.Data;
using ats_recruitment_service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ats_recruitment_service.Data;
using ats_recruitment_service.Models;

namespace RecruitmentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationsController : ControllerBase
    {
       private readonly RecruitmentContext _context;

        public ApplicationsController(RecruitmentContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Application>>> GetAll()
        {
            return await _context.Applications
                .Include(a => a.Feedbacks)
                .Include(a => a.Offers)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Application>> GetById(int id)
        {
            var app = await _context.Applications
                .Include(a => a.Feedbacks)
                .Include(a => a.Offers)
                .FirstOrDefaultAsync(a => a.Id == id);

            return app == null ? NotFound() : app;
        }

        [HttpPost]
        public async Task<ActionResult<Application>> Create(Application application)
        {
            application.CreatedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = application.Id }, application);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Application application)
        {
            if (id != application.Id) return BadRequest();

            application.UpdatedAt = DateTime.UtcNow;
            _context.Entry(application).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var app = await _context.Applications.FindAsync(id);
            if (app == null) return NotFound();

            _context.Applications.Remove(app);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var app = await _context.Applications.FindAsync(id);
            if (app == null) return NotFound();

            app.Status = status;
            app.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(app);
        }
    }
}