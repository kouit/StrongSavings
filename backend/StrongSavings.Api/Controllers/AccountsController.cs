
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongSavings.Api.Data;
using StrongSavings.Api.Enums;
using StrongSavings.Api.Models;

namespace StrongSavings.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AccountsController : ControllerBase
{
    private readonly StrongSavingsDbContext _context;

    public AccountsController(StrongSavingsDbContext context)
    {
        _context = context;
    }

    // GET: api/Accounts
    // Administrators can view all accounts.
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _context.Accounts
            .AsNoTracking()
            .OrderBy(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Type,
                a.PersonId,
                a.CreatedAtUtc,
                a.IsActive
            })
            .ToListAsync();

        return Ok(accounts);
    }

    // GET: api/Accounts/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAccount(int id)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Type,
                a.PersonId,
                a.CreatedAtUtc,
                a.IsActive
            })
            .FirstOrDefaultAsync();

        if (account is null)
        {
            return NotFound("Account not found.");
        }

        return Ok(account);
    }

    // GET: api/Accounts/1/balance
    [HttpGet("{id:int}/balance")]
    public async Task<IActionResult> GetAccountBalance(int id)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account is null)
        {
            return NotFound("Account not found.");
        }

        var balance = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == id)
            .Select(t => t.Type == TransactionType.Deposit
                ? (decimal?)t.Amount
                : (decimal?)-t.Amount)
            .SumAsync() ?? 0m;

        return Ok(new
        {
            account.Id,
            account.Name,
            account.Type,
            Balance = balance
        });
    }

    // POST: api/Accounts/personal/1
    // Administrators create personal accounts for existing members.
    [HttpPost("personal/{personId:int}")]
    public async Task<IActionResult> CreatePersonalAccount(int personId)
    {
        var person = await _context.People
            .FirstOrDefaultAsync(p => p.Id == personId && p.IsActive);

        if (person is null)
        {
            return NotFound("Active member not found.");
        }

        var existingAccount = await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.PersonId == personId &&
                a.Type == AccountType.Personal);

        if (existingAccount is not null)
        {
            return Conflict(new
            {
                Message = "This member already has a personal account.",
                existingAccount.Id,
                existingAccount.Name
            });
        }

        var account = new Account
        {
            Name = $"{person.FirstName} {person.LastName} - Savings",
            Type = AccountType.Personal,
            PersonId = person.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAccount),
            new { id = account.Id },
            new
            {
                account.Id,
                account.Name,
                account.Type,
                account.PersonId,
                account.CreatedAtUtc,
                account.IsActive
            });
    }

    // POST: api/Accounts/general
    // Administrators can create the general Lalo Apele account.
    [HttpPost("general")]
    public async Task<IActionResult> CreateGeneralAccount()
    {
        const string generalAccountName = "Lalo Apele";

        var existingAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Type == AccountType.General);

        if (existingAccount is not null)
        {
            return Conflict("The general account already exists.");
        }

        var account = new Account
        {
            Name = generalAccountName,
            Type = AccountType.General,
            PersonId = null,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAccount),
            new { id = account.Id },
            new
            {
                account.Id,
                account.Name,
                account.Type,
                account.PersonId,
                account.CreatedAtUtc,
                account.IsActive
            });
    }
}
