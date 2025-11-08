using Ats.Messaging.Abstractions;
using AuthorizationService.Data;
using AuthorizationService.DTO;
using AuthorizationService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthorizationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly IEventPublisher _publisher;

    public RolesController(AuthDbContext db, IEventPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
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

        if (user.Roles.Any(r => r.RoleId == role.Id))
            return Conflict("Role already assigned");
        
        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        };

        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new
        {
            userId,
            role = role.Name,
            assignedAt = DateTime.UtcNow
        }, routingKey: "role.assigned");

        return Ok(new { message = $"Role '{role.Name}' assigned to {user.Username}" });
    }

    [HttpPost("{userId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid userId, [FromBody] RevokeRoleRequest req, CancellationToken ct)
    {
        var userRole = await _db.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Role.Name == req.Role, ct);

        if (userRole == null)
            return NotFound($"User does not have role '{req.Role}'.");

        _db.UserRoles.Remove(userRole);
        await _db.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new
        {
            userId = userId,
            role = req.Role,
            revokedAt = DateTime.UtcNow
        }, routingKey: "role.revoked");

        return Ok(new { message = $"Role '{req.Role}' revoked." });
    }

    [HttpGet("{userId:guid}")]
    public async Task<IEnumerable<string>> GetUserRoles(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null) return Array.Empty<string>();

        return user.Roles.Select(ur => ur.Role.Name);
    }
}