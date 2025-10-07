using AuthorizationService.Data;
using AuthorizationService.DTO;
using AuthorizationService.Messaging;
using AuthorizationService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthorizationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly RabbitMqPublisher _bus;

    public RolesController(AuthDbContext db, RabbitMqPublisher bus)
    {
        _db = db;
        _bus = bus;
    }

    [HttpGet]
    public async Task<IEnumerable<string>> All(CancellationToken ct)
    {
        return await _db.Roles
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .ToListAsync(ct);
    }

    [HttpPost("{userId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid userId, [FromBody] AssignRoleRequest req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return NotFound("User not found");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == req.RoleName, ct);
        if (role == null) return NotFound("Role not found");

        if (user.Roles.Any(r => r.Id == role.Id))
            return Conflict("Role already assigned");

        user.Roles.Add(role);
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync("auth.role.assigned", new
        {
            userId,
            role = role.Name,
            assignedAt = DateTime.UtcNow
        });

        return NoContent();
    }

    [HttpPost("{userId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid userId, [FromBody] RevokeRoleRequest req, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return NotFound("User not found");

        var role = user.Roles.FirstOrDefault(r => r.Name == req.RoleName);
        if (role == null) return NotFound("Role not assigned");

        user.Roles.Remove(role);
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync("auth.role.revoked", new
        {
            userId,
            role = role.Name,
            revokedAt = DateTime.UtcNow
        });

        return NoContent();
    }

    [HttpGet("{userId:guid}")]
    public async Task<IEnumerable<string>> GetUserRoles(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null) return Array.Empty<string>();

        return user.Roles.Select(r => r.Name);
    }
}