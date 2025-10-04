// Controllers/InterviewsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InterviewService.Data;
using InterviewService.Models;
using InterviewService.DTO;

namespace InterviewService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InterviewsController : ControllerBase
{
    private readonly InterviewContext _db;

    public InterviewsController(InterviewContext db) => _db = db;

    // GET /api/interviews
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InterviewDto>>> GetAll()
    {
        var items = await _db.Interviews.AsNoTracking().ToListAsync();
        return items.Select(ToDto).ToList();
    }

    // GET /api/interviews/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<InterviewDto>> GetById(int id)
    {
        var entity = await _db.Interviews.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return entity is null ? NotFound() : ToDto(entity);
    }

    // POST /api/interviews
    [HttpPost]
    public async Task<ActionResult<InterviewDto>> Create([FromBody] InterviewCreateDto dto)
    {
        var entity = new Interview
        {
            ApplicationId = dto.ApplicationId,
            ScheduledAt   = dto.ScheduledAt,
            Interviewer   = dto.Interviewer,
            Location      = dto.Location,
            Status        = dto.Status,
            Notes         = dto.Notes
        };

        _db.Interviews.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    // PUT /api/interviews/{id}  (полная замена)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] InterviewUpdateDto dto)
    {
        var entity = await _db.Interviews.FindAsync(id);
        if (entity is null) return NotFound();

        entity.ApplicationId = dto.ApplicationId;
        entity.ScheduledAt   = dto.ScheduledAt;
        entity.Interviewer   = dto.Interviewer;
        entity.Location      = dto.Location;
        entity.Status        = dto.Status;
        entity.Notes         = dto.Notes;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PATCH /api/interviews/{id} (частичное)
    [HttpPatch("{id:int}")]
    public async Task<ActionResult<InterviewDto>> Patch(int id, [FromBody] InterviewPatchDto dto)
    {
        var entity = await _db.Interviews.FindAsync(id);
        if (entity is null) return NotFound();

        if (dto.ApplicationId.HasValue) entity.ApplicationId = dto.ApplicationId.Value;
        if (dto.ScheduledAt.HasValue)  entity.ScheduledAt   = dto.ScheduledAt.Value;
        if (dto.Interviewer is not null) entity.Interviewer = dto.Interviewer;
        if (dto.Location   is not null) entity.Location     = dto.Location;
        if (dto.Status     is not null) entity.Status       = dto.Status;
        if (dto.Notes      is not null) entity.Notes        = dto.Notes;

        await _db.SaveChangesAsync();
        return Ok(ToDto(entity));
    }

    // DELETE /api/interviews/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Interviews.FindAsync(id);
        if (entity is null) return NotFound();

        _db.Interviews.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ----- FEEDBACKS (вложенные) -----

    // GET /api/interviews/{id}/feedbacks
    [HttpGet("{id:int}/feedbacks")]
    public async Task<ActionResult<IEnumerable<FeedbackDto>>> GetFeedbacks(int id)
    {
        var exists = await _db.Interviews.AnyAsync(i => i.Id == id);
        if (!exists) return NotFound();

        var items = await _db.Feedbacks
            .AsNoTracking()
            .Where(f => f.InterviewId == id)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    // GET /api/interviews/{id}/feedbacks/{feedbackId}
    [HttpGet("{id:int}/feedbacks/{feedbackId:int}")]
    public async Task<ActionResult<FeedbackDto>> GetFeedback(int id, int feedbackId)
    {
        var item = await _db.Feedbacks.AsNoTracking()
            .FirstOrDefaultAsync(f => f.InterviewId == id && f.Id == feedbackId);

        return item is null ? NotFound() : ToDto(item);
    }

    // POST /api/interviews/{id}/feedbacks
    [HttpPost("{id:int}/feedbacks")]
    public async Task<ActionResult<FeedbackDto>> CreateFeedback(int id, [FromBody] FeedbackCreateDto dto)
    {
        var exists = await _db.Interviews.AnyAsync(i => i.Id == id);
        if (!exists) return NotFound();

        var entity = new InterviewFeedback
        {
            InterviewId = id,
            AuthorId    = dto.AuthorId,
            Comments    = dto.Comments,
            Score       = dto.Score,
            CreatedAt   = DateTime.UtcNow
        };

        _db.Feedbacks.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFeedback), new { id, feedbackId = entity.Id }, ToDto(entity));
    }

    // DELETE /api/interviews/{id}/feedbacks/{feedbackId}
    [HttpDelete("{id:int}/feedbacks/{feedbackId:int}")]
    public async Task<IActionResult> DeleteFeedback(int id, int feedbackId)
    {
        var entity = await _db.Feedbacks
            .FirstOrDefaultAsync(f => f.InterviewId == id && f.Id == feedbackId);

        if (entity is null) return NotFound();

        _db.Feedbacks.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- mapping helpers ----
    private static InterviewDto ToDto(Interview e) => new()
    {
        Id = e.Id,
        ApplicationId = e.ApplicationId,
        ScheduledAt = e.ScheduledAt,
        Interviewer = e.Interviewer,
        Location = e.Location,
        Status = e.Status,
        Notes = e.Notes
    };

    private static FeedbackDto ToDto(InterviewFeedback f) => new()
    {
        Id = f.Id,
        InterviewId = f.InterviewId,
        AuthorId = f.AuthorId,
        Comments = f.Comments,
        Score = f.Score,
        CreatedAt = f.CreatedAt
    };
}