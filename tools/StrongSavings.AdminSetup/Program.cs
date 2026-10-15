using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StrongSavings.Api.Auth;
using StrongSavings.Api.Data;

var projectRoot = FindProjectRoot();
var apiProjectPath = Path.Combine(
    projectRoot, "backend", "StrongSavings.Api");

var configuration = new ConfigurationBuilder()
    .SetBasePath(apiProjectPath)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
        optional: true)
    .AddUserSecrets(typeof(ApplicationUser).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString =
    configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("Database connection string is missing.");
    return;
}

var services = new ServiceCollection();

services.AddLogging();

services.AddDbContext<StrongSavingsDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure()));

services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<StrongSavingsDbContext>();

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var userManager =
    scope.ServiceProvider.GetRequiredService<
        UserManager<ApplicationUser>>();

var db = scope.ServiceProvider.GetRequiredService<
    StrongSavingsDbContext>();

try
{
    if (!await db.Database.CanConnectAsync())
    {
        Console.WriteLine("Cannot connect to the database.");
        return;
    }

    Console.Write("Administrator email: ");
    var email = Console.ReadLine()?.Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(email))
    {
        Console.WriteLine("Email is required.");
        return;
    }

    var user = await userManager.FindByEmailAsync(email);

    if (user is null)
    {
        Console.WriteLine("No account was found for that email.");
        return;
    }

    if (!await userManager.IsInRoleAsync(user, "Admin"))
    {
        Console.WriteLine(
            "This account is not an administrator. No changes made.");
        return;
    }

    Console.Write("New password (input hidden): ");
    var newPassword = ReadPassword();
    Console.WriteLine();

    Console.Write("Confirm new password (input hidden): ");
    var confirmation = ReadPassword();
    Console.WriteLine();

    if (newPassword != confirmation)
    {
        Console.WriteLine("Passwords do not match. No changes made.");
        return;
    }

    var resetToken =
        await userManager.GeneratePasswordResetTokenAsync(user);

    var result = await userManager.ResetPasswordAsync(
        user, resetToken, newPassword);

    if (!result.Succeeded)
    {
        Console.WriteLine("Password reset failed:");

        foreach (var error in result.Errors)
        {
            Console.WriteLine(error.Description);
        }

        return;
    }

    Console.WriteLine(
        $"Password reset successfully for {user.Email}.");
    Console.WriteLine(
        "Your savings data and administrator account are unchanged.");
}
catch (Exception ex)
{
    Console.WriteLine(
        $"Reset failed ({ex.GetType().Name}). " +
        "Check your local configuration and database connection.");
}

static string FindProjectRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);

    while (directory is not null)
    {
        if (Directory.Exists(Path.Combine(
                directory.FullName, "backend", "StrongSavings.Api")) &&
            Directory.Exists(Path.Combine(
                directory.FullName, "tools", "StrongSavings.AdminSetup")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException(
        "Could not locate the StrongSavings repository root.");
}

static string ReadPassword()
{
    var password = new System.Text.StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
            break;

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
                password.Length--;

            continue;
        }

        if (!char.IsControl(key.KeyChar))
            password.Append(key.KeyChar);
    }

    return password.ToString();
}