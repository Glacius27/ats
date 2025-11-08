using Ats.Users.Services;
using Microsoft.AspNetCore.Mvc;

namespace CandidateService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserCache _cache;

    public UsersController(UserCache cache)
    {
        _cache = cache;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var users = _cache.Users.Values.ToList();
        return Ok(users);
    }
}