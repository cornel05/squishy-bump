using UnityEngine;

namespace SquishyBump.Core
{
    /// <summary>Floor area an active hazard currently threatens (burning patch, falling tile, cone…), as a circle.</summary>
    public readonly struct HazardZone
    {
        public HazardZone(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public Vector3 Center { get; }
        public float Radius { get; }
    }
}
