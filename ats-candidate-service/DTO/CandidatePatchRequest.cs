namespace ats_candidate_service.DTO
{
    public class CandidatePatchRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Status { get; set; }
        public string? ResumeFileName { get; set; }
    }
}