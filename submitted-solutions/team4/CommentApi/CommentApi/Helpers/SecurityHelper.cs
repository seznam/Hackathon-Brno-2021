using System.Security.Cryptography;
using System.Text;

namespace CommentApi.Helpers
{
    public static class Sha256Helper
    {
        public static string GenerateHash(string data)
        {
            var bytes = Encoding.Unicode.GetBytes(data);
            using var hashEngine = SHA256.Create();
            var hashedBytes = hashEngine.ComputeHash(bytes, 0, bytes.Length);
            var sb = new StringBuilder();
            foreach (var b in hashedBytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }
    }
} 