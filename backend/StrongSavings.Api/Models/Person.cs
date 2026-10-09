namespace StrongSavings.Api.Models;

public class Person
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int GroupId { get; set; }

    public Group Group { get; set; } = null!;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}