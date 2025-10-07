using AuthorizationService.Data;
using AuthorizationService.DTO;
using AuthorizationService.Messaging;
using AuthorizationService.Models;
using AuthorizationService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthorizationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly KeycloakAdminClient _keycloak;
    private readonly RabbitMqPublisher _bus;

    public UsersController(AuthDbContext db, KeycloakAdminClient keycloak, RabbitMqPublisher bus)
    {
        _db = db;
        _keycloak = keycloak;
        _bus = bus;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        // 1) Создаём пользователя в Keycloak
        var kcUserId = await _keycloak.CreateUserAsync(req.Username, req.Email, ct);
        await _keycloak.SetPasswordAsync(kcUserId, req.Password, ct);

        // 2) Создаём пользователя в БД
        var user = new User
        {
            Username = req.Username,
            Email = req.Email,
            KeycloakUserId = kcUserId,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // 3) Публикуем событие
        await _bus.PublishAsync("auth.user.created", new
        {
            userId = user.Id,
            keycloakUserId = kcUserId,
            username = user.Username,
            email = user.Email,
            createdAt = user.CreatedAt
        });

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            KeycloakUserId = user.KeycloakUserId,
            IsActive = user.IsActive,
            Roles = Enumerable.Empty<string>()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null) return NotFound();

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            KeycloakUserId = user.KeycloakUserId,
            IsActive = user.IsActive,
            Roles = user.Roles.Select(r => r.Name)
        };
    }

    [HttpGet]
    public async Task<IEnumerable<UserResponse>> List(CancellationToken ct)
    {
        var users = await _db.Users
            .Include(u => u.Roles)
            .ToListAsync(ct);

        return users.Select(user => new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            KeycloakUserId = user.KeycloakUserId,
            IsActive = user.IsActive,
            Roles = user.Roles.Select(r => r.Name)
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync("auth.user.deactivated", new
        {
            userId = user.Id,
            deactivatedAt = DateTime.UtcNow
        });

        await _keycloak.DeleteUserAsync(user.KeycloakUserId);
        return NoContent();
    }

    [HttpGet("snapshot")]
    public async Task<IActionResult> GetSnapshot([FromQuery] DateTime? since = null, CancellationToken ct = default)
    {
        IQueryable<User> query = _db.Users.Include(u => u.Roles);

        if (since.HasValue)
            query = query.Where(u => u.LastUpdatedAt > since.Value);

        var users = await query.ToListAsync(ct);

        return Ok(users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.KeycloakUserId,
            u.IsActive,
            Roles = u.Roles.Select(r => r.Name),
            u.LastUpdatedAt
        }));
    }
}