
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongSavings.Api.Data;
using StrongSavings.Api.Enums;
using StrongSavings.Api.Models;

namespace StrongSavings.Api.Controllers;

[ApiController]
[Route("api/Groups/{groupId:int}/People")]
[Authorize(Policy = "AdminOnly")]
public class PeopleController : ControllerBase
{
    private readonly StrongSavingsDbContext _context;

    public PeopleController(StrongSavingsDbContext context)
    {
        _context = context;
    }

    // GET: api/Groups/1/People
    // Administrators can view members in a group.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetPeople(
        int groupId)
    {
        var groupExists = await _context.Groups
            .AnyAsync(group => group.Id == groupId);

        if (!groupExists)
        {
            return NotFound("Group not found.");
        }

        var people = await _context.People
            .AsNoTracking()
            .Where(person => person.GroupId == groupId)
            .OrderBy(person => person.FirstName)
            .ThenBy(person => person.LastName)
            .Select(person => new
            {
                person.Id,
                person.FirstName,
                person.LastName,
                person.GroupId,
                person.CreatedAtUtc,
                person.IsActive
            })
            .ToListAsync();

        return Ok(people);
    }

    // POST: api/Groups/1/People
    // Creates a member and their personal savings account together.
    [HttpPost]
    public async Task<ActionResult<object>> CreatePerson(
        int groupId,
        [FromBody] CreatePersonRequest request)
    {
        var firstName = request.FirstName?.Trim();
        var lastName = request.LastName?.Trim();

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            return BadRequest(
                "First name and last name are required.");
        }

        if (firstName.Length > 100 || lastName.Length > 100)
        {
            return BadRequest(
                "First name and last name must be 100 characters or fewer.");
        }

        var groupExists = await _context.Groups
            .AnyAsync(group => group.Id == groupId);

        if (!groupExists)
        {
            return NotFound("Group not found.");
        }

        var now = DateTime.UtcNow;

        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            GroupId = groupId,
            CreatedAtUtc = now,
            IsActive = true
        };

        var account = new Account
        {
            Name = $"{firstName} {lastName} - Savings",
            Type = AccountType.Personal,
            Person = person,
            IsActive = true,
            CreatedAtUtc = now
        };

        _context.People.Add(person);
        _context.Accounts.Add(account);

        // Save the member and personal account together.
        await _context.SaveChangesAsync();

        return Created(
            $"/api/Groups/{groupId}/People",
            new
            {
                person.Id,
                person.FirstName,
                person.LastName,
                person.GroupId,
                person.CreatedAtUtc,
                person.IsActive,
                Account = new
                {
                    account.Id,
                    account.Name,
                    account.Type,
                    account.IsActive
                }
            });
    }
}

public class CreatePersonRequest
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}
