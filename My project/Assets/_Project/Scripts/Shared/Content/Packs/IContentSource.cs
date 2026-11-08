namespace Game.Shared.Content.Packs
{
    /// <summary>
    /// Where content JSON comes from: Resources in the game, the Assets folder in the editor and .NET tools. Paths are
    /// relative to the content root without extension ("packs", "base/pack", "base/items").
    /// </summary>
    public interface IContentSource
    {
        /// <summary>The file's text, or null if there is no such file.</summary>
        string ReadText(string path);
    }
}
