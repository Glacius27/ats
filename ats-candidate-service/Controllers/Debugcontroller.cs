using Ats.CandidateService.Users;
using Microsoft.AspNetCore.Mvc;

namespace CandidateService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class Debugcontroller : ControllerBase
{
    private readonly UserCache _cache;

    public Debugcontroller(UserCache cache)
    {
        _cache = cache;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var users = _cache.All;
        return Ok(users);
    }
}