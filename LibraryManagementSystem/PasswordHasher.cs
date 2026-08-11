using System;
using System.Security.Cryptography;

namespace LibraryManagementSystem;

internal static class PasswordHasher
{
    private const int Iterations = 100_000;

    public static string Hash(string value)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            value,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            32);
        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string value, string expectedHash)
    {
        string[] parts = expectedHash.Split('$');
        if (parts.Length != 4
            || !string.Equals(parts[0], "PBKDF2", StringComparison.Ordinal)
            || !int.TryParse(parts[1], out int iterations))
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                value,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
