
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
public class TransactionsController : ControllerBase
{
    private readonly StrongSavingsDbContext _context;

    public TransactionsController(StrongSavingsDbContext context)
    {
        _context = context;
    }

    // GET: api/Transactions
    // Administrators can view all transaction history.
    [HttpGet]
    public async Task<IActionResult> GetTransactions()
    {
        var transactions = await _context.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.DateUtc)
            .Select(t => new
            {
                t.Id,
                t.Amount,
                t.DateUtc,
                t.Type,
                t.AccountId,
                AccountName = t.Account.Name,
                t.SubmittedByPersonId,
                SubmittedBy = t.SubmittedByPerson == null
                    ? null
                    : t.SubmittedByPerson.FirstName + " " +
                      t.SubmittedByPerson.LastName,
                t.Description
            })
            .ToListAsync();

        return Ok(transactions);
    }

    // GET: api/Transactions/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        var transaction = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new
            {
                t.Id,
                t.Amount,
                t.DateUtc,
                t.Type,
                t.AccountId,
                AccountName = t.Account.Name,
                t.SubmittedByPersonId,
                SubmittedBy = t.SubmittedByPerson == null
                    ? null
                    : t.SubmittedByPerson.FirstName + " " +
                      t.SubmittedByPerson.LastName,
                t.Description
            })
            .FirstOrDefaultAsync();

        if (transaction is null)
        {
            return NotFound("Transaction not found.");
        }

        return Ok(transaction);
    }

    // POST: api/Transactions
    // Only administrators may record deposits or withdrawals.
    [HttpPost]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new
            {
                message = "Amount must be greater than zero."
            });
        }

        if (!Enum.IsDefined(typeof(TransactionType), request.Type))
        {
            return BadRequest(new
            {
                message = "Invalid transaction type."
            });
        }

        if (request.Description?.Length > 500)
        {
            return BadRequest(new
            {
                message = "Description cannot exceed 500 characters."
            });
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == request.AccountId && a.IsActive);

        if (account is null)
        {
            return BadRequest(new
            {
                message = "Account not found or inactive."
            });
        }

        if (request.SubmittedByPersonId.HasValue)
        {
            var submitter = await _context.People
                .FirstOrDefaultAsync(p =>
                    p.Id == request.SubmittedByPersonId.Value &&
                    p.IsActive);

            if (submitter is null)
            {
                return BadRequest(new
                {
                    message = "Submitter not found or inactive."
                });
            }
        }

        // Preserve the existing overdraft check.
        if (request.Type == TransactionType.Withdrawal)
        {
            var currentBalance = await _context.Transactions
                .Where(t => t.AccountId == request.AccountId)
                .Select(t => t.Type == TransactionType.Deposit
                    ? (decimal?)t.Amount
                    : (decimal?)-t.Amount)
                .SumAsync() ?? 0m;

            if (request.Amount > currentBalance)
            {
                return BadRequest(new
                {
                    message = "Insufficient funds.",
                    availableBalance = currentBalance,
                    requestedWithdrawal = request.Amount
                });
            }
        }

        var transaction = new Transaction
        {
            Amount = request.Amount,
            DateUtc = DateTime.UtcNow,
            Type = request.Type,
            AccountId = request.AccountId,
            SubmittedByPersonId = request.SubmittedByPersonId,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim()
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTransaction),
            new { id = transaction.Id },
            new
            {
                transaction.Id,
                transaction.Amount,
                transaction.DateUtc,
                transaction.Type,
                transaction.AccountId,
                transaction.SubmittedByPersonId,
                transaction.Description
            });
    }
}

public class CreateTransactionRequest
{
    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }

    public int AccountId { get; set; }

    public int? SubmittedByPersonId { get; set; }

    public string? Description { get; set; }
}
