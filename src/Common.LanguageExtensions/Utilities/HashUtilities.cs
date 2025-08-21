using System.Security.Cryptography;
using System.Text;

namespace Common.LanguageExtensions.Utilities
{
    public static class HashUtilities
    {
        public static string ComputeSHA256Hash(string value)
        {
            using (var hasher = SHA256.Create())
            {
                byte[] hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
