
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongSavings.Api.Data;
using StrongSavings.Api.Models;

namespace StrongSavings.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class GroupsController : ControllerBase
{
    private readonly StrongSavingsDbContext _context;

    public GroupsController(StrongSavingsDbContext context)
    {
        _context = context;
    }

    // GET: api/Groups
    // Administrators can view all club groups.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Group>>> GetGroups()
    {
        return await _context.Groups
            .AsNoTracking()
            .OrderBy(group => group.Name)
            .ToListAsync();
    }

    // POST: api/Groups
    // Administrators can create new groups.
    [HttpPost]
    public async Task<ActionResult<Group>> CreateGroup(
        [FromBody] CreateGroupRequest request)
    {
        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Group name is required.");
        }

        if (name.Length > 100)
        {
            return BadRequest(
                "Group name cannot exceed 100 characters.");
        }

        var group = new Group
        {
            Name = name,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetGroups),
            new { id = group.Id },
            group);
    }
}

public class CreateGroupRequest
{
    public string? Name { get; set; }
}
