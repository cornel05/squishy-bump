using UnityEngine;

namespace SquishyBump.Presentation
{
    /// <summary>Visual-only tuning: squash (GDD 6.2), camera (GDD 3.4, 10.1) and screen shake. Never read by the simulation.</summary>
    [CreateAssetMenu(menuName = "Squishy Bump/Presentation Profile")]
    public sealed class PresentationProfile : ScriptableObject
    {
        [Header("Squash")]
        [Tooltip("Peak scale: x = horizontal, y = vertical.")]
        [SerializeField] private Vector2 squashScale;
        [SerializeField] private float squashSeconds;
        [Tooltip("Downward speed, m/s, above which stopping counts as a landing.")]
        [SerializeField] private float landingSpeed;

        [Header("Camera")]
        [SerializeField] private float cameraDistance;
        [SerializeField] private float pitchMinDegrees;
        [SerializeField] private float pitchMaxDegrees;
        [SerializeField] private float pitchDefaultDegrees;
        [Tooltip("Degrees of orbit per unit of look input.")]
        [SerializeField] private float lookSensitivity;

        [Header("Screen shake")]
        [Tooltip("Shake force per N·s of received impulse.")]
        [SerializeField] private float shakePerImpulse;

        public Vector2 SquashScale => squashScale;
        public float SquashSeconds => squashSeconds;
        public float LandingSpeed => landingSpeed;
        public float CameraDistance => cameraDistance;
        public float PitchMinDegrees => pitchMinDegrees;
        public float PitchMaxDegrees => pitchMaxDegrees;
        public float PitchDefaultDegrees => pitchDefaultDegrees;
        public float LookSensitivity => lookSensitivity;
        public float ShakePerImpulse => shakePerImpulse;
    }
}
