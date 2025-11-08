namespace Ats.Shared.Auth;

public class AuthUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime LastUpdatedAt { get; set; }
}