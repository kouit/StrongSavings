
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongSavings.Api.Data;
using StrongSavings.Api.Enums;

namespace StrongSavings.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "MemberOrAdmin")]
public class SavingsSummaryController : ControllerBase
{
    private readonly StrongSavingsDbContext _context;

    public SavingsSummaryController(StrongSavingsDbContext context)
    {
        _context = context;
    }

    // GET: api/SavingsSummary
    // Administrators can view all members.
    // Members can view only their own summary.
    [HttpGet]
    public async Task<IActionResult> GetSavingsSummary()
    {
        if (!User.IsInRole("Admin"))
        {
            var personId = GetLinkedPersonId();

            if (personId is null)
            {
                return Forbid();
            }

            return await GetMemberSavingsSummary(personId.Value);
        }

        var members = await _context.People
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.GroupId,
                GroupName = p.Group.Name
            })
            .ToListAsync();

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a =>
                a.IsActive &&
                a.PersonId.HasValue &&
                a.Type == AccountType.Personal)
            .Select(a => new
            {
                a.Id,
                a.PersonId,
                a.Name
            })
            .ToListAsync();

        var accountBalances = await _context.Transactions
            .AsNoTracking()
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Balance = g.Sum(t =>
                    t.Type == TransactionType.Deposit
                        ? t.Amount
                        : -t.Amount)
            })
            .ToListAsync();

        var balanceLookup = accountBalances.ToDictionary(
            item => item.AccountId,
            item => item.Balance);

        var accountLookup = accounts.ToLookup(a => a.PersonId);

        var summary = members.Select(member =>
        {
            var memberAccounts = accountLookup[member.Id]
                .Select(account => new
                {
                    account.Id,
                    account.Name,
                    Balance = balanceLookup.TryGetValue(
                        account.Id,
                        out var balance)
                            ? balance
                            : 0m
                })
                .ToList();

            return new
            {
                MemberId = member.Id,
                MemberName = $"{member.FirstName} {member.LastName}",
                member.GroupId,
                member.GroupName,
                Accounts = memberAccounts,
                TotalSavings = memberAccounts.Sum(a => a.Balance)
            };
        });

        return Ok(summary);
    }

    // GET: api/SavingsSummary/1
    // Administrators can view any member.
    // Members can view only the member linked to their login.
    [HttpGet("{personId:int}")]
    public async Task<IActionResult> GetMemberSavingsSummary(
        int personId)
    {
        if (!User.IsInRole("Admin"))
        {
            var linkedPersonId = GetLinkedPersonId();

            if (linkedPersonId is null)
            {
                return Forbid();
            }

            if (linkedPersonId.Value != personId)
            {
                return Forbid();
            }
        }

        var member = await _context.People
            .AsNoTracking()
            .Where(p => p.Id == personId && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.GroupId,
                GroupName = p.Group.Name
            })
            .FirstOrDefaultAsync();

        if (member is null)
        {
            return NotFound("Active member not found.");
        }

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a =>
                a.IsActive &&
                a.PersonId == personId &&
                a.Type == AccountType.Personal)
            .Select(a => new
            {
                a.Id,
                a.Name,
                Balance = _context.Transactions
                    .Where(t => t.AccountId == a.Id)
                    .Select(t =>
                        t.Type == TransactionType.Deposit
                            ? (decimal?)t.Amount
                            : (decimal?)-t.Amount)
                    .Sum() ?? 0m
            })
            .ToListAsync();

        return Ok(new
        {
            MemberId = member.Id,
            MemberName = $"{member.FirstName} {member.LastName}",
            member.GroupId,
            member.GroupName,
            Accounts = accounts,
            TotalSavings = accounts.Sum(a => a.Balance)
        });
    }

    private int? GetLinkedPersonId()
    {
        var personIdClaim = User.FindFirst("personId")?.Value;

        return int.TryParse(personIdClaim, out var personId)
            ? personId
            : null;
    }
}
