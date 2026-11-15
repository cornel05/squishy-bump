using System;

namespace SquishyBump.Core
{
    /// <summary>Match rule picked by the host (GDD 3.6).</summary>
    public enum MatchRule { Survival, Rank }

    /// <summary>
    /// Local player options (GDD 3.1 game options). Pure preferences, never game state:
    /// Presentation and audio read them, the simulation never does. Saved by SB-51.
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        /// <summary>Multiplier on the profile's look sensitivity.</summary>
        public float MouseSensitivity = 1f;
        /// <summary>Multiplier on the profile's camera distance.</summary>
        public float CameraDistance = 1f;
        public float MasterVolume = 1f;
        public bool ScreenShake = true;
        public MatchRule Rule = MatchRule.Survival;

        public event Action<GameSettings> Changed;

        public void NotifyChanged() => Changed?.Invoke(this);
    }
}
