using System.Security.Cryptography;
using System.Text;

namespace Game.Shared.Content.Packs
{
    /// <summary>
    /// Hash of the loaded content (packs, files, texts in load order) so a client with other data is refused at login
    /// (question 82). Line endings are normalized: git may check files out with CRLF on Windows.
    /// </summary>
    public sealed class ContentHash
    {
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        public void Add(string path, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(path + "\n" + (text ?? string.Empty).Replace("\r\n", "\n") + "\n");
            _hash.AppendData(bytes);
        }

        public string Finish()
        {
            byte[] digest = _hash.GetHashAndReset();
            var sb = new StringBuilder(digest.Length * 2);
            foreach (byte b in digest)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
