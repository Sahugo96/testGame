using System;
using System.Security.Cryptography;

namespace Game.Server.Persistence
{
    /// <summary>Passwords are never stored: PBKDF2 (SHA-256) with a random salt; comparison in constant time.</summary>
    public static class PasswordHasher
    {
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        public static void Hash(string password, int iterations, out string hash, out string salt)
        {
            var saltBytes = new byte[SaltBytes];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(saltBytes);
            }
            salt = Convert.ToBase64String(saltBytes);
            hash = Convert.ToBase64String(Derive(password, saltBytes, iterations));
        }

        public static bool Verify(string password, string hash, string salt, int iterations)
        {
            if (password == null || hash == null || salt == null)
            {
                return false;
            }
            byte[] expected = Convert.FromBase64String(hash);
            byte[] actual = Derive(password, Convert.FromBase64String(salt), iterations);
            int difference = expected.Length ^ actual.Length;
            for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++)
            {
                difference |= expected[i] ^ actual[i];
            }
            return difference == 0;
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashBytes);
            }
        }
    }
}
