
using Microsoft.AspNetCore.Identity;

namespace StrongSavings.Api.Auth;

public class ApplicationUser : IdentityUser<int>
{
    // Null means this login has not been linked to a club member.
    public int? PersonId { get; set; }
}
