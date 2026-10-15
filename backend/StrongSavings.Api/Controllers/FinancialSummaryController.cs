
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongSavings.Api.Data;
using StrongSavings.Api.Enums;

namespace StrongSavings.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class FinancialSummaryController : ControllerBase
{
    private readonly StrongSavingsDbContext _context;

    public FinancialSummaryController(StrongSavingsDbContext context)
    {
        _context = context;
    }

    // GET: api/FinancialSummary
    // Administrators only: returns club-wide financial information.
    [HttpGet]
    public async Task<IActionResult> GetFinancialSummary()
    {
        // Load active accounts.
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Type,
                a.PersonId
            })
            .ToListAsync();

        // Calculate the balance of every account from its transactions.
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

        decimal GetBalance(int accountId)
        {
            return balanceLookup.TryGetValue(accountId, out var balance)
                ? balance
                : 0m;
        }

        // Separate personal and general account balances.
        var personalAccounts = accounts
            .Where(a =>
                a.Type == AccountType.Personal &&
                a.PersonId.HasValue)
            .ToList();

        var generalAccounts = accounts
            .Where(a => a.Type == AccountType.General)
            .ToList();

        var personalSavings = personalAccounts
            .Sum(a => GetBalance(a.Id));

        var generalSavings = generalAccounts
            .Sum(a => GetBalance(a.Id));

        // Get active members and their group names.
        var members = await _context.People
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.GroupId,
                GroupName = p.Group.Name
            })
            .ToListAsync();

        // Build savings totals for each group.
        var groupSummary = members
            .GroupBy(m => new { m.GroupId, m.GroupName })
            .Select(group =>
            {
                var memberIds = group
                    .Select(m => m.Id)
                    .ToHashSet();

                var groupAccounts = personalAccounts
                    .Where(a =>
                        a.PersonId.HasValue &&
                        memberIds.Contains(a.PersonId.Value))
                    .ToList();

                return new
                {
                    group.Key.GroupId,
                    group.Key.GroupName,
                    MemberCount = group.Count(),
                    TotalPersonalSavings = groupAccounts
                        .Sum(a => GetBalance(a.Id))
                };
            })
            .OrderBy(g => g.GroupName)
            .ToList();

        return Ok(new
        {
            Currency = "NZD",
            PersonalSavings = personalSavings,
            GeneralAccountSavings = generalSavings,
            TotalClubFunds = personalSavings + generalSavings,

            GeneralAccounts = generalAccounts
                .Select(a => new
                {
                    a.Id,
                    a.Name,
                    Balance = GetBalance(a.Id)
                })
                .ToList(),

            Groups = groupSummary
        });
    }
}
