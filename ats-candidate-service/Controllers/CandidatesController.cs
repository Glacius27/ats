using ats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CandidateService.Data;
using CandidateService.Models;

namespace CandidateService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CandidatesController : ControllerBase
    {
        private readonly CandidateContext _context;
        private readonly ResumeStorageService _storage;

        public CandidatesController(CandidateContext context, ResumeStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Candidate>>> GetCandidates()
        {
            return await _context.Candidates.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Candidate>> GetCandidate(int id)
        {
            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null) return NotFound();
            return candidate;
        }

        // [HttpPost]
        // public async Task<ActionResult<Candidate>> CreateCandidate(Candidate candidate)
        // {
        //     _context.Candidates.Add(candidate);
        //     await _context.SaveChangesAsync();
        //     return CreatedAtAction(nameof(GetCandidate), new { id = candidate.Id }, candidate);
        // }
        
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CandidateCreateRequest request)
        {
            var candidate = new Candidate
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone
            };

            if (request.Resume != null && request.Resume.Length > 0)
            {
                using var stream = request.Resume.OpenReadStream();
                await _storage.UploadResumeAsync(request.Resume.FileName, stream, request.Resume.ContentType);
                candidate.ResumeFileName = request.Resume.FileName;
            }

            _context.Candidates.Add(candidate);
            await _context.SaveChangesAsync();

            return Ok(candidate);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCandidate(int id, Candidate candidate)
        {
            if (id != candidate.Id) return BadRequest();

            _context.Entry(candidate).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCandidate(int id)
        {
            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null) return NotFound();

            _context.Candidates.Remove(candidate);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        [HttpGet("{id}/resume")]
        public async Task<IActionResult> GetResume(int id)
        {
            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null || string.IsNullOrEmpty(candidate.ResumeFileName))
                return NotFound("Резюме не найдено");

            var stream = await _storage.GetFileAsync(candidate.ResumeFileName);

            // можно улучшить — хранить ContentType вместе с файлом
            return File(stream, "application/octet-stream", candidate.ResumeFileName);
        }
    }
}