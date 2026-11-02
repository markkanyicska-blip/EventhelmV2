using EventHelm.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EventHelm.Api.Security;

public sealed class AccountPasswordHasher : PasswordHasher<Account>
{
    public const int IterationCount = 220_000;

    public AccountPasswordHasher()
        : base(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = IterationCount
        }))
    {
    }
}
