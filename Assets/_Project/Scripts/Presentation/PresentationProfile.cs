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

        [Header("Bump feedback (GDD 3.4)")]
        [SerializeField] private float hitStopSeconds;
        [Tooltip("At or above this impulse (N·s) a hit plays the squish instead of the soft bump.")]
        [SerializeField] private float strongHitImpulse;
        [Tooltip("Squish pitch at the hit threshold and at the top impulse.")]
        [SerializeField] private Vector2 squishPitch;
        [SerializeField] private float squishTopImpulse;
        [Tooltip("Obstacle whoosh plays when a box is this close to the local fruit (GDD 7.2: 2 m).")]
        [SerializeField] private float whooshDistance;

        [Header("SFX (GDD 12.7), in Sfx enum order")]
        [SerializeField] private AudioClip[] sfx;

        public float HitStopSeconds => hitStopSeconds;
        public float StrongHitImpulse => strongHitImpulse;
        public Vector2 SquishPitch => squishPitch;
        public float SquishTopImpulse => squishTopImpulse;
        public float WhooshDistance => whooshDistance;
        public AudioClip Clip(Sfx id) => sfx != null && (int)id < sfx.Length ? sfx[(int)id] : null;
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
