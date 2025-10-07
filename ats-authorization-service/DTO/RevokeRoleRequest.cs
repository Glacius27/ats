using System.ComponentModel.DataAnnotations;

namespace AuthorizationService.DTO;

public class RevokeRoleRequest
{
    [Required, MaxLength(100)]
    public string RoleName { get; set; } = null!;
}