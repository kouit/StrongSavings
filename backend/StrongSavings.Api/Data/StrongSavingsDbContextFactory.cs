using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StrongSavings.Api.Data;

public class StrongSavingsDbContextFactory
    : IDesignTimeDbContextFactory<StrongSavingsDbContext>
{
    public StrongSavingsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<StrongSavingsDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection was not found.");
        }

        var options = new DbContextOptionsBuilder<StrongSavingsDbContext>();

        options.UseSqlServer(connectionString);

        return new StrongSavingsDbContext(options.Options);
    }
}