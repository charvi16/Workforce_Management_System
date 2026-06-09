using System.Security.Cryptography;

namespace WMS.Infrastructure.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return string.Join('.', Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public static bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedKey = Convert.FromBase64String(parts[1]);
        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
