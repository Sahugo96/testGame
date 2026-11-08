using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Game.EditorTools.Content
{
    /// <summary>Before any player build: cards and JSON in sync and the content valid, or the build stops with the errors.</summary>
    public sealed class ContentBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            List<string> errors = ContentSync.Run("build");
            if (errors.Count > 0)
            {
                throw new BuildFailedException("Content is broken (see the Console): " + string.Join("; ", errors));
            }
        }
    }
}
