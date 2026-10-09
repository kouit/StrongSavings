using StrongSavings.Api.Enums;

namespace StrongSavings.Api.Models;

public class Account
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public int? PersonId { get; set; }

    public Person? Person { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}