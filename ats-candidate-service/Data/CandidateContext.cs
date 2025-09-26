using Microsoft.EntityFrameworkCore;
using CandidateService.Models;

namespace CandidateService.Data
{
    public class CandidateContext : DbContext
    {
        public CandidateContext(DbContextOptions<CandidateContext> options)
            : base(options) { }

        public DbSet<Candidate> Candidates { get; set; }
    }
}