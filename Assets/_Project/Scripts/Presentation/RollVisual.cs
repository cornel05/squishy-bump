using UnityEngine;

namespace SquishyBump.Presentation
{
    /// <summary>
    /// Sits on the fighter's Visual child (GDD 6.2). Rolls the Body child without slipping, ω = (ŷ × v)/r,
    /// and squashes on landings and hits. The physics root and its collider never rotate or scale.
    /// Velocity comes from the root's motion, so it works the same on host and clients.
    /// </summary>
    public sealed class RollVisual : MonoBehaviour
    {
        [SerializeField] private PresentationProfile profile;
        [Tooltip("Child mesh that rolls. Squash scales this transform instead, so it stays upright.")]
        [SerializeField] private Transform body;
        [Tooltip("Rolling radius in metres (the class radius).")]
        [SerializeField] private float radius;

        private Vector3 baseScale;
        private Vector3 basePosition;
        private Vector3 lastPosition;
        private float lastVerticalSpeed;
        private float squashElapsed = float.PositiveInfinity;

        public Transform Body => body;
        /// <summary>Visual jump detection, on every peer (for the jump sound).</summary>
        public event System.Action<RollVisual> TookOff;

        private void Awake()
        {
            baseScale = transform.localScale;
            basePosition = transform.localPosition;
            lastPosition = transform.parent != null ? transform.parent.position : transform.position;
        }

        public void Configure(PresentationProfile presentationProfile, float rollRadius)
        {
            profile = presentationProfile;
            radius = rollRadius;
        }

        /// <summary>Starts the jelly squash; called on host-confirmed hits and on landings.</summary>
        public void PlaySquash() => squashElapsed = 0f;

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            if (dt <= 0f || profile == null) return;

            var position = transform.parent != null ? transform.parent.position : transform.position;
            var velocity = (position - lastPosition) / dt;
            lastPosition = position;

            if (body != null && radius > 0f)
            {
                var omega = AngularVelocity(velocity, radius);
                var degrees = omega.magnitude * Mathf.Rad2Deg * dt;
                if (degrees > 0f) body.rotation = Quaternion.AngleAxis(degrees, omega.normalized) * body.rotation;
            }

            // Landing: falling fast, then the fall stops within one frame.
            if (lastVerticalSpeed < -profile.LandingSpeed && velocity.y - lastVerticalSpeed > profile.LandingSpeed)
                PlaySquash();
            // Take-off: resting, then rising fast within one frame (a jump).
            else if (lastVerticalSpeed > -profile.LandingSpeed && velocity.y - lastVerticalSpeed > profile.LandingSpeed)
                TookOff?.Invoke(this);
            lastVerticalSpeed = velocity.y;

            var factor = SquashFactor(squashElapsed, profile.SquashScale, profile.SquashSeconds);
            squashElapsed += dt;
            transform.localScale = Vector3.Scale(baseScale, factor);
            // Keep the bottom on the floor while the height shrinks.
            transform.localPosition = basePosition + Vector3.down * ((1f - factor.y) * radius);
        }

        /// <summary>Rolling without slipping: ω = (ŷ × v) / r (GDD 6.2).</summary>
        public static Vector3 AngularVelocity(Vector3 velocity, float radius) =>
            Vector3.Cross(Vector3.up, velocity) / radius;

        /// <summary>Scale factor: the peak squash at 0, easing back to 1 at <paramref name="seconds"/>.</summary>
        public static Vector3 SquashFactor(float elapsed, Vector2 peak, float seconds)
        {
            var t = seconds > 0f ? Mathf.Clamp01(elapsed / seconds) : 1f;
            var x = Mathf.Lerp(peak.x, 1f, t);
            return new Vector3(x, Mathf.Lerp(peak.y, 1f, t), x);
        }
    }
}
