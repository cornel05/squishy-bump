using UnityEngine;

namespace SquishyBump.Hazards
{
    /// <summary>
    /// GDD 12.5: amber = telegraph, red blinking at 2 Hz = about to act. Every use pairs the colour with a shape
    /// (seam ring, arrows, stripes, cone, ring with a cross) so the signal never relies on colour alone.
    /// </summary>
    public static class TelegraphColors
    {
        public static readonly Color Amber = new(1f, 0.718f, 0.012f);   // #FFB703
        public static readonly Color Red = new(0.902f, 0.224f, 0.275f); // #E63946

        /// <summary>Translucent amber for floor zones.</summary>
        public static Color AmberZone => new(Amber.r, Amber.g, Amber.b, 0.35f);
    }
}
