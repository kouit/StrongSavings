namespace StrongSavings.Api.Models;

public class Meeting
{
    public int Id { get; set; }

    public DateOnly MeetingDate { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}