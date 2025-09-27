using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ats_recruitment_service.Data;
using ats_recruitment_service.Models;

namespace ats_recruitment_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OffersController : ControllerBase
    {
        private readonly RecruitmentContext _context;

        public OffersController(RecruitmentContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Offer>> GetById(int id)
        {
            var offer = await _context.Offers
                .Include(o => o.Application)
                .FirstOrDefaultAsync(o => o.Id == id);

            return offer == null ? NotFound() : offer;
        }

        [HttpPost]
        public async Task<ActionResult<Offer>> Create(Offer offer)
        {
            offer.CreatedAt = DateTime.UtcNow;

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = offer.Id }, offer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Offer offer)
        {
            if (id != offer.Id) return BadRequest();

            _context.Entry(offer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null) return NotFound();

            _context.Offers.Remove(offer);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}