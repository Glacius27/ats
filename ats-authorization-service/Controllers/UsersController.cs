
using Ats.Integration;
using AuthorizationService.Data;
using AuthorizationService.DTO;
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
    private readonly IAtsBus _bus;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AuthDbContext db, KeycloakAdminClient keycloak, IAtsBus bus, ILogger<UsersController> logger)
    {
        _db = db;
        _keycloak = keycloak;
        _bus = bus;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        //  Создаём пользователя в Keycloak
        var kcUserId = await _keycloak.CreateUserAsync(req.Username, req.Email, ct);
        await _keycloak.SetPasswordAsync(kcUserId, req.Password, ct);

        // Создаём пользователя в БД
        var user = new User
        {
            Username = req.Username,
            Email = req.Email,
            KeycloakUserId = kcUserId,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        
        if (!string.IsNullOrWhiteSpace(req.Role))
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == req.Role, ct);
            if (role != null)
            {
                _db.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
                await _db.SaveChangesAsync(ct);
            }
        }
        var roles = await _db.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync(ct);
        
        _logger.LogInformation("🐇 Publishing user list update to RabbitMQ...");
        await _bus.PublishAsync("user.created", new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            isActive = user.IsActive,
            roles = roles
        }, ct);
        _logger.LogInformation("new user message published to RabbitMQ");

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            KeycloakUserId = user.KeycloakUserId,
            IsActive = user.IsActive,
            Roles = roles
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null) return NotFound();

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            KeycloakUserId = user.KeycloakUserId,
            IsActive = user.IsActive,
            Roles = user.Roles.Select(r => r.Role.Name)
        };
    }

    [HttpGet]
    public async Task<IEnumerable<UserResponse>> List(CancellationToken ct)
    {
        var users = await _db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync(ct);

        return users.Select(user => new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            KeycloakUserId = user.KeycloakUserId,
            IsActive = user.IsActive,
            //Roles = user.Roles.Select(r => r.Role.Name)
            Roles = user.Roles
                .Where(ur => ur.Role != null)  
                .Select(ur => ur.Role!.Name)  
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync("user.deactivated", new { id = user.Id }, ct);

        await _keycloak.DeleteUserAsync(user.KeycloakUserId);
        return NoContent();
    }

    [HttpGet("snapshot")]
    public async Task<IActionResult> GetSnapshot([FromQuery] DateTime? since = null, CancellationToken ct = default)
    {
        IQueryable<User> query = _db.Users.Include(u => u.Roles).ThenInclude(ur => ur.Role);

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
            Roles = u.Roles.Select(r => r.Role.Name),
            u.LastUpdatedAt
        }));
    }
    
    [HttpPost("publish-list")]
    public async Task<IActionResult> PublishUsersList(CancellationToken ct)
    {
        var users = await _db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .Select(u => new UserDto(
                u.Id,
                u.Username,
                u.Email,
                u.Roles.Select(ur => ur.Role.Name).ToList()
            ))
            .ToListAsync(ct);

        var evt = new UsersListEvent { Users = users };

       await _bus.PublishAsync("users.list", new { users }, ct);

        return Ok(new { published = users.Count });
    }
    public record UserDto(Guid Id, string Username, string Email, List<string> Roles);

    public class UsersListEvent
    {
        public List<UserDto> Users { get; set; } = new();
    }
}