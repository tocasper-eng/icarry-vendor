using System.Security.Cryptography;
using System.Text;

namespace IcarryVendor.Web.Auth;

/// <summary>
/// vendor_account.pwd holds an unsalted SHA-1 hex digest. The table is shared with icarry-newvendor and
/// icarry-admin, so the format cannot change here without migrating those systems at the same time.
/// </summary>
public static class LegacyPasswordHasher
{
    public static string Hash(string password)
    {
        var digest = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexStringLower(digest);
    }

    public static bool Verify(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        var expected = Encoding.ASCII.GetBytes(storedHash.Trim().ToLowerInvariant());
        var actual = Encoding.ASCII.GetBytes(Hash(password));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
