using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;

namespace UserService.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly UserDbContext _db;

    public UsersController(UserDbContext db)
    {
        _db = db;
    }

    [HttpGet("{email}")]
    public async Task<IActionResult> Get(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound();

        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        if (await _db.Users.AnyAsync(u => u.Email == user.Email))
            return Conflict("User already exists.");

        _db.Users.Add(user); 
        await _db.SaveChangesAsync();
        return Created($"/users/{user.Email}", user);
    }

    [HttpPatch("{email}/space")]
    public async Task<IActionResult> UpdateUsedSpace(string email, [FromBody] UsedSpaceUpdateDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound();

        user.UsedSpace = dto.UsedSpace;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
