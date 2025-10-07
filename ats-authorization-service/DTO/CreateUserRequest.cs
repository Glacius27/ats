using System.ComponentModel.DataAnnotations;

namespace AuthorizationService.DTO;

public class CreateUserRequest
{
    [Required, MaxLength(100)]
    public string Username { get; set; } = null!;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = null!;

    [Required, MinLength(6), MaxLength(128)]
    public string Password { get; set; } = null!;
}