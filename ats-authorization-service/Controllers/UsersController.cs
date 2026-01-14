
using Ats.Integration;
using Ats.Integration.Contracts;
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
      
        var kcUserId = await _keycloak.CreateUserAsync(req.Username, req.Email, ct);
        await _keycloak.SetPasswordAsync(kcUserId, req.Password, ct);

       
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
        
        await _bus.PublishAsync("user.created", new AuthUser
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            Roles = roles ?? new List<string>()
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

    [HttpGet("by-keycloak-id/{keycloakUserId}")]
    public async Task<ActionResult<UserResponse>> GetByKeycloakUserId(string keycloakUserId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, ct);

        // If user not found in DB, try to sync from Keycloak
        if (user == null)
        {
            try
            {
                _logger.LogInformation("User not found in DB, attempting to sync from Keycloak: {KeycloakUserId}", keycloakUserId);
                var kcUser = await _keycloak.GetUserAsync(keycloakUserId, ct);
                if (kcUser == null)
                {
                    _logger.LogWarning("User not found in Keycloak: {KeycloakUserId}", keycloakUserId);
                    return NotFound();
                }
                _logger.LogInformation("Found user in Keycloak: {Username} ({KeycloakUserId})", kcUser.Username, keycloakUserId);

                // Create user in DB from Keycloak data
                user = new User
                {
                    Username = kcUser.Username ?? kcUser.Id,
                    Email = kcUser.Email ?? string.Empty,
                    KeycloakUserId = kcUser.Id,
                    IsActive = kcUser.Enabled
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync(ct);

                // Sync roles from Keycloak
                var kcRoles = await _keycloak.GetUserRealmRolesAsync(keycloakUserId, ct);
                _logger.LogInformation("Keycloak roles for user {KeycloakUserId}: {Roles}", keycloakUserId, string.Join(", ", kcRoles));
                foreach (var roleName in kcRoles)
                {
                    // Try case-insensitive match
                    var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower(), ct);
                    if (role != null)
                    {
                        _logger.LogInformation("Found role {RoleName} for user {Username}", role.Name, user.Username);
                        var userRole = new UserRole
                        {
                            UserId = user.Id,
                            RoleId = role.Id
                        };
                        _db.UserRoles.Add(userRole);
                    }
                    else
                    {
                        _logger.LogWarning("Role {RoleName} not found in database for user {Username}", roleName, user.Username);
                    }
                }
                await _db.SaveChangesAsync(ct);

                // Reload user with roles
                user = await _db.Users
                    .Include(u => u.Roles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == user.Id, ct);

                _logger.LogInformation("User synced from Keycloak: {Username} ({KeycloakUserId})", user?.Username, keycloakUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync user from Keycloak: {KeycloakUserId}", keycloakUserId);
                return NotFound();
            }
        }

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
            Roles = user.Roles
                .Where(ur => ur.Role != null)  
                .Select(ur => ur.Role!.Name)  
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);

        var roles = await _db.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role!.Name)
            .ToListAsync(ct);

        var authUser = new AuthUser
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            Roles = roles ?? new List<string>()
        };

        await _bus.PublishAsync("user.deactivated", authUser, ct);
        _logger.LogInformation("User deactivated event published: {User}", user.Username);
        
        await _keycloak.DeleteUserAsync(user.KeycloakUserId, ct);
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

    [HttpPost("sync-from-keycloak")]
    public async Task<IActionResult> SyncUsersFromKeycloak(CancellationToken ct)
    {
        try
        {
            var kcUsers = await _keycloak.GetAllUsersAsync(ct);
            var syncedCount = 0;
            var createdCount = 0;

            foreach (var kcUser in kcUsers)
            {
                // Skip service accounts
                if (kcUser.Username?.StartsWith("service-account-") == true)
                    continue;

                var existingUser = await _db.Users
                    .FirstOrDefaultAsync(u => u.KeycloakUserId == kcUser.Id, ct);

                if (existingUser == null)
                {
                    // Create new user
                    var user = new User
                    {
                        Username = kcUser.Username ?? kcUser.Id,
                        Email = kcUser.Email ?? string.Empty,
                        KeycloakUserId = kcUser.Id,
                        IsActive = kcUser.Enabled
                    };

                    _db.Users.Add(user);
                    await _db.SaveChangesAsync(ct);

                    // Sync roles from Keycloak
                    var kcRoles = await _keycloak.GetUserRealmRolesAsync(kcUser.Id, ct);
                    _logger.LogInformation("Keycloak roles for user {KeycloakUserId}: {Roles}", kcUser.Id, string.Join(", ", kcRoles));
                    foreach (var roleName in kcRoles)
                    {
                        // Try case-insensitive match
                        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower(), ct);
                        if (role != null)
                        {
                            _logger.LogInformation("Found role {RoleName} for user {Username}", role.Name, user.Username);
                            var userRole = new UserRole
                            {
                                UserId = user.Id,
                                RoleId = role.Id
                            };
                            _db.UserRoles.Add(userRole);
                        }
                        else
                        {
                            _logger.LogWarning("Role {RoleName} not found in database for user {Username}", roleName, user.Username);
                        }
                    }
                    await _db.SaveChangesAsync(ct);
                    createdCount++;
                }
                else
                {
                    // Update existing user
                    existingUser.Username = kcUser.Username ?? existingUser.Username;
                    existingUser.Email = kcUser.Email ?? existingUser.Email;
                    existingUser.IsActive = kcUser.Enabled;

                    // Sync roles
                    var kcRoles = await _keycloak.GetUserRealmRolesAsync(kcUser.Id, ct);
                    var existingRoleNames = await _db.UserRoles
                        .Include(ur => ur.Role)
                        .Where(ur => ur.UserId == existingUser.Id)
                        .Select(ur => ur.Role!.Name)
                        .ToListAsync(ct);

                    // Add missing roles (case-insensitive comparison)
                    foreach (var roleName in kcRoles.Where(r => !existingRoleNames.Any(existing => existing.ToLower() == r.ToLower())))
                    {
                        // Try case-insensitive match
                        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower(), ct);
                        if (role != null)
                        {
                            var userRole = new UserRole
                            {
                                UserId = existingUser.Id,
                                RoleId = role.Id
                            };
                            _db.UserRoles.Add(userRole);
                        }
                    }

                    // Remove roles that are no longer in Keycloak (case-insensitive comparison)
                    var rolesToRemove = existingRoleNames.Where(r => !kcRoles.Any(kc => kc.ToLower() == r.ToLower())).ToList();
                    foreach (var roleName in rolesToRemove)
                    {
                        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower(), ct);
                        if (role != null)
                        {
                            var userRole = await _db.UserRoles
                                .FirstOrDefaultAsync(ur => ur.UserId == existingUser.Id && ur.RoleId == role.Id, ct);
                            if (userRole != null)
                            {
                                _db.UserRoles.Remove(userRole);
                            }
                        }
                    }

                    await _db.SaveChangesAsync(ct);
                }
                syncedCount++;
            }

            _logger.LogInformation("Synced {SyncedCount} users from Keycloak, created {CreatedCount} new users", syncedCount, createdCount);

            return Ok(new { 
                synced = syncedCount, 
                created = createdCount,
                message = $"Синхронизировано {syncedCount} пользователей, создано {createdCount} новых"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync users from Keycloak");
            return StatusCode(500, new { error = "Ошибка синхронизации пользователей из Keycloak", details = ex.Message });
        }
    }
}