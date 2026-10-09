using StrongSavings.Api.Enums;

namespace StrongSavings.Api.Models;

public class Transaction
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public DateTime DateUtc { get; set; } = DateTime.UtcNow;

    public TransactionType Type { get; set; }

    public int AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public int? SubmittedByPersonId { get; set; }

    public Person? SubmittedByPerson { get; set; }

    public string? Description { get; set; }
}