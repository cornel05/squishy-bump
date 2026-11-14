using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SquishyBump.Editor
{
    /// <summary>Batchmode entry points used by tools/build-windows.ps1.</summary>
    public static class BuildScript
    {
        public const string WindowsOutput = "Builds/Windows/SquishyBump.exe";

        [MenuItem("Squishy Bump/Build Windows x64")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = WindowsOutput,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[BuildScript] {s.result}: {s.totalErrors} errors, {s.totalSize / (1024 * 1024)} MB, {s.totalTime}");

            // Non-zero exit so the calling script fails fast.
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
