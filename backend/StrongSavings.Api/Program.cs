
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StrongSavings.Api.Auth;
using StrongSavings.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// Database
// --------------------------------------------------

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is missing. Configure Jwt:Key using User Secrets.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "The JWT signing key must contain at least 32 bytes.");
}

const string jwtIssuer = "StrongSavings.Api";
const string jwtAudience = "StrongSavings.Web";

builder.Services.AddDbContext<StrongSavingsDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure()));

// --------------------------------------------------
// ASP.NET Core Identity
// --------------------------------------------------

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
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
    .AddEntityFrameworkStores<StrongSavingsDbContext>()
    .AddDefaultTokenProviders();

// --------------------------------------------------
// JWT authentication
// --------------------------------------------------

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultForbidScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),

                // Our JWT includes standard ClaimTypes.Role claims.
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                NameClaimType = System.Security.Claims.ClaimTypes.Name
            };
    });

// --------------------------------------------------
// Authorization policies
// --------------------------------------------------

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MemberOrAdmin", policy =>
        policy.RequireRole("Member", "Admin"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// --------------------------------------------------
// API services
// --------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// --------------------------------------------------
// Ensure roles exist
// --------------------------------------------------

// This creates the role definitions only.
// It does NOT create an administrator account or assign
// administrator privileges to a public registration.

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider.GetRequiredService<
            RoleManager<IdentityRole<int>>>();

    foreach (var roleName in new[] { "Member", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole<int>(roleName));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role '{roleName}': " +
                    string.Join(
                        "; ",
                        result.Errors.Select(error => error.Description)));
            }
        }
    }
}

// --------------------------------------------------
// HTTP pipeline
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
