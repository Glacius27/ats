namespace Ats.Shared.Auth;

public class AuthUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public string RolesJson { get; set; } = "[]";
    public DateTime LastUpdatedAt { get; set; }
}