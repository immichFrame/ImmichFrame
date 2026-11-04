using System.Security.Cryptography;
using System.Text;

namespace ImmichFrame.WebApi.Helpers
{
    public static class SecretComparison
    {
        /// <summary>
        /// Compares in constant time. Hashing first gives both sides the same length, so the
        /// comparison reveals neither the secret's content nor its length.
        /// </summary>
        public static bool Matches(string expected, string given)
            => CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
                SHA256.HashData(Encoding.UTF8.GetBytes(given)));
    }
}
