namespace StrongSavings.Api.Models;

public class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Person> People { get; set; } = new List<Person>();
}