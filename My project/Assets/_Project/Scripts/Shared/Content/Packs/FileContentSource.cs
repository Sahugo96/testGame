using System.IO;
using System.Text;

namespace Game.Shared.Content.Packs
{
    /// <summary>Content from a folder on disk (tests, LoadTest, the editor): root/&lt;path&gt;.json.</summary>
    public sealed class FileContentSource : IContentSource
    {
        public const string Extension = ".json";

        public FileContentSource(string root) => Root = root;

        public string Root { get; }

        public string ReadText(string path)
        {
            string file = FullPath(path);
            return File.Exists(file) ? File.ReadAllText(file, Encoding.UTF8) : null;
        }

        public string FullPath(string path) => Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar) + Extension);
    }
}
