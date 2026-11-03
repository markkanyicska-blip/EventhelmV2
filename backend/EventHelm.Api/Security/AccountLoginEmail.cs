using System.Net.Mail;

namespace EventHelm.Api.Security;

public static class AccountLoginEmail
{
    public static string? Normalize(string? email)
    {
        if (email is null)
            return null;
        var value = email.Trim();
        if (value.Length is < 3 or > 254 || value.Any(c => c > 127) ||
            !MailAddress.TryCreate(value, out var address) || address.Address != value ||
            !string.IsNullOrEmpty(address.DisplayName))
            return null;
        return value.ToLowerInvariant();
    }
}
