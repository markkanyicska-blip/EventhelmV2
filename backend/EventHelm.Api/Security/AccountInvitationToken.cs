using System.Security.Cryptography;
using System.Text;

namespace EventHelm.Api.Security;

public static class AccountInvitationToken
{
    public static string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token)
    {
        if (token is null || token.Length != 64 ||
            token.Any(character => character is not (>= '0' and <= '9' or >= 'A' and <= 'F')))
            throw new ArgumentException("The invitation token format is invalid.", nameof(token));

        return Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
    }
}
