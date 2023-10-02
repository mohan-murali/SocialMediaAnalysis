using System.Security.Cryptography;
using System.Text;

namespace SocialMediaAnalysis.Utils;

public static class PasswordUtils
{
    private const int KeySize = 64;
    private const int Iterations = 350000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

    public static string HashPassword(string password)
    {
        //generate a random salt for hashing
        var salt = RandomNumberGenerator.GetBytes(KeySize);

        //hash password given salt and iterations (default to 1000)
        //iterations provide difficulty when cracking
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        //return delimited string with salt | #iterations | hash
        return Convert.ToBase64String(salt) + "|" +
               Convert.ToBase64String(hash);
    }

    public static bool ValidatePassword(string password, string passwordHash)
    {
        //extract original values from delimited hash text
        var origHashedParts = passwordHash.Split('|');
        var origSalt = Convert.FromBase64String(origHashedParts[0]);
        var origHash = origHashedParts[1];

        var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(password, origSalt, Iterations, HashAlgorithm, KeySize);
        return CryptographicOperations.FixedTimeEquals(hashToCompare, Convert.FromBase64String(origHash));
    }
}