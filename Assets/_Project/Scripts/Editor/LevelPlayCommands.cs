using System;
using SquishyBump.App;
using UnityEditor;
using UnityEngine;

namespace SquishyBump.Editor
{
    /// <summary>Editor Play Mode entry points to inject directly into L1–L5 via LevelLauncher.</summary>
    public static class LevelPlayCommands
    {
        [MenuItem("Squishy Bump/Play Level/L1 (Training Ring)")]
        private static void MenuL1() => PlayLevel(1);

        [MenuItem("Squishy Bump/Play Level/L2 (Moving Ground)")]
        private static void MenuL2() => PlayLevel(2);

        [MenuItem("Squishy Bump/Play Level/L3 (Cannon Court)")]
        private static void MenuL3() => PlayLevel(3);

        [MenuItem("Squishy Bump/Play Level/L4 (Frost Ring)")]
        private static void MenuL4() => PlayLevel(4);

        [MenuItem("Squishy Bump/Play Level/L5 (Chaos Core)")]
        private static void MenuL5() => PlayLevel(5);

        public static string PlayL1() => PlayLevel(1);
        public static string PlayL2() => PlayLevel(2);
        public static string PlayL3() => PlayLevel(3);
        public static string PlayL4() => PlayLevel(4);
        public static string PlayL5() => PlayLevel(5);

        public static string PlayLevel(int level)
        {
            if (level < 1 || level > 5)
                throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 5.");
            if (!Application.isPlaying)
                throw new InvalidOperationException("Enter Play Mode from Boot.unity before calling PlayLevel.");
            var launcher = UnityEngine.Object.FindFirstObjectByType<LevelLauncher>();
            if (launcher == null)
                throw new InvalidOperationException("LevelLauncher not found. Start Play Mode from Boot.unity.");
            launcher.StartSinglePlayer(level);
            return $"Loaded L{level}.";
        }
    }
}
