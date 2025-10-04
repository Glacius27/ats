using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ats_recruitment_service.Data;
using ats_recruitment_service.DTO;
using ats_recruitment_service.Models;

namespace ats_recruitment_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbacksController : ControllerBase
    {
        private readonly RecruitmentContext _context;

        public FeedbacksController(RecruitmentContext context)
        {
            _context = context;
        }

        [HttpGet("application/{applicationId}")]
        public async Task<ActionResult<IEnumerable<Feedback>>> GetByApplication(int applicationId)
        {
            return await _context.Feedbacks
                .Where(f => f.ApplicationId == applicationId)
                .ToListAsync();
        }

        // [HttpPost]
        // public async Task<ActionResult<Feedback>> Create(Feedback feedback)
        // {
        //     feedback.CreatedAt = DateTime.UtcNow;
        //
        //     _context.Feedbacks.Add(feedback);
        //     await _context.SaveChangesAsync();
        //
        //     return CreatedAtAction(nameof(GetByApplication), new { applicationId = feedback.ApplicationId }, feedback);
        // }
        
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FeedbackDto dto)
        {
            var feedback = new Feedback
            {
                ApplicationId = dto.ApplicationId,
                AuthorId = dto.AuthorId,
                Comments = dto.Comments,
                Score = dto.Score
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            //return CreatedAtAction(nameof(GetByApplication), new { id = feedback.Id }, feedback);
            return Ok(feedback);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var fb = await _context.Feedbacks.FindAsync(id);
            if (fb == null) return NotFound();

            _context.Feedbacks.Remove(fb);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}