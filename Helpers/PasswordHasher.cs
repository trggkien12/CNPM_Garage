using System.Security.Cryptography;

namespace AutoGarageManager.Helpers
{
    public static class PasswordHasher
    {
        public static bool IsHashedPassword(string? stored)
            => !string.IsNullOrWhiteSpace(stored) && stored.StartsWith("PBKDF2$", StringComparison.Ordinal);

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) password = "123456";
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
            return $"PBKDF2$100000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string? stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            if (!IsHashedPassword(stored)) return stored == password;

            var parts = stored.Split('$');
            if (parts.Length != 4) return false;
            if (!int.TryParse(parts[1], out var iterations)) return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch
            {
                return false;
            }
        }
    }
}
