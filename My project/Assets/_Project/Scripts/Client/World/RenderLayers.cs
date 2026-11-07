namespace Game.Client.World
{
    /// <summary>Render layers used by client cameras. The preview layer is an unnamed user layer, so no project settings change is needed.</summary>
    public static class RenderLayers
    {
        public const int Preview = 31;

        public static int PreviewMask => 1 << Preview;

        public static int AllExceptPreview => ~(1 << Preview);
    }
}
