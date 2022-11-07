using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Security.Cryptography;

namespace StatsBro.Domain.Helpers
{
    public class Chaos
    {
        private const int iterations = 51913;
        private static readonly HashAlgorithmName _algorithm = HashAlgorithmName.SHA512;

        public static string Hash(string salt, params string[] s)
        {
            var hashBytes = KeyDerivation.Pbkdf2(
                password: string.Join(".", s),
                salt: Convert.FromBase64String(salt),
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: iterations,
                numBytesRequested: 512/8);
            
            return Convert.ToBase64String(hashBytes);
        }

        public static string GenerateSalt(int size = 32)
        {
            RNGCryptoServiceProvider rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            byte[] randomBytes = new byte[size];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
