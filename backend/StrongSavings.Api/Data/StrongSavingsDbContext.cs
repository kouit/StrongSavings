using Microsoft.EntityFrameworkCore;

namespace StrongSavings.Api.Data;

public class StrongSavingsDbContext : DbContext
{
    public StrongSavingsDbContext(DbContextOptions<StrongSavingsDbContext> options)
        : base(options)
    {
    }
}
