using Ats.Integration;
using AuthorizationService.Data;
using AuthorizationService.Models;
using AuthorizationService.DTO;           // если тут лежат AssignRoleRequest / RevokeRoleRequest
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthorizationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly IAtsBus _bus;

    public RolesController(AuthDbContext db, IAtsBus bus)
    {
        _db = db;
        _bus = bus;
    }

    // GET: api/roles/{userId}
    [HttpGet("{userId:guid}")]
    public async Task<IEnumerable<string>> GetUserRoles(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return Array.Empty<string>();
        return user.Roles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name);
    }

    // POST: api/roles/{userId}/assign
    [HttpPost("{userId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid userId, [FromBody] AssignRoleRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest("Role is required.");

        var user = await _db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return NotFound($"User {userId} not found.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == req.Role, ct);
        if (role is null) return NotFound($"Role '{req.Role}' not found.");

        var hasRole = user.Roles.Any(ur => ur.RoleId == role.Id);
        if (!hasRole)
        {
            user.Roles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
            await _db.SaveChangesAsync(ct);
        }

        // публикация полного снимка user.updated
        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role!.Name)
            .ToListAsync(ct);

        await _bus.PublishAsync("user.updated", new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            isActive = user.IsActive,
            roles
        }, ct);

        return Ok(new { message = $"Role '{req.Role}' assigned.", roles });
    }

    // POST: api/roles/{userId}/revoke
    [HttpPost("{userId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid userId, [FromBody] RevokeRoleRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest("Role is required.");

        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return NotFound($"User {userId} not found.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == req.Role, ct);
        if (role is null) return NotFound($"Role '{req.Role}' not found.");

        var link = await _db.UserRoles.FirstOrDefaultAsync(
            ur => ur.UserId == user.Id && ur.RoleId == role.Id, ct);

        if (link is null)
            return NotFound($"User doesn't have role '{req.Role}'.");

        _db.UserRoles.Remove(link);
        await _db.SaveChangesAsync(ct);

        // публикация полного снимка user.updated
        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role!.Name)
            .ToListAsync(ct);

        await _bus.PublishAsync("user.updated", new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            isActive = user.IsActive,
            roles
        }, ct);

        return Ok(new { message = $"Role '{req.Role}' revoked.", roles });
    }
}