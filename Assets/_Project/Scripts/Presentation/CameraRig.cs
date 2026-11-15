using Unity.Cinemachine;
using UnityEngine;

namespace SquishyBump.Presentation
{
    /// <summary>
    /// Local player's follow camera (GDD 3.4, 10.1): Cinemachine 3 orbit at a fixed distance, pitch clamped,
    /// never rolling with the fighter, plus impulse screen shake that the player can turn off.
    /// Builds its Cinemachine pieces in Awake so scenes only need this component and a Camera.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private PresentationProfile profile;
        [Tooltip("Camera driven by the rig; Camera.main when empty.")]
        [SerializeField] private Camera outputCamera;
        [SerializeField] private bool shakeEnabled = true;

        private CinemachineCamera virtualCamera;
        private CinemachineOrbitalFollow orbit;
        private CinemachineImpulseSource impulse;

        /// <summary>Camera heading in degrees: the yaw sent in <c>InputCommand</c> for camera-relative movement.</summary>
        public float Yaw => orbit.HorizontalAxis.Value;
        public float Pitch => orbit.VerticalAxis.Value;
        public float Distance => orbit.Radius;
        public bool ShakeEnabled { get => shakeEnabled; set => shakeEnabled = value; }
        /// <summary>Player option: multiplier on the profile's look sensitivity.</summary>
        public float SensitivityScale { get; set; } = 1f;

        /// <summary>For rigs built at runtime: call on an inactive object, before Awake.</summary>
        public void Configure(PresentationProfile presentationProfile, Camera camera)
        {
            profile = presentationProfile;
            outputCamera = camera;
        }

        /// <summary>Player option: multiplier on the profile's camera distance.</summary>
        public void SetDistanceScale(float scale) => orbit.Radius = profile.CameraDistance * scale;

        private void Awake()
        {
            if (outputCamera == null) outputCamera = Camera.main;
            if (outputCamera != null && !outputCamera.TryGetComponent<CinemachineBrain>(out _))
                outputCamera.gameObject.AddComponent<CinemachineBrain>();

            var go = new GameObject("FollowCamera");
            go.transform.SetParent(transform, false);
            virtualCamera = go.AddComponent<CinemachineCamera>();
            orbit = go.AddComponent<CinemachineOrbitalFollow>();
            go.AddComponent<CinemachineRotationComposer>();
            go.AddComponent<CinemachineImpulseListener>();
            impulse = go.AddComponent<CinemachineImpulseSource>();

            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = profile.CameraDistance;
            orbit.VerticalAxis.Range = new Vector2(profile.PitchMinDegrees, profile.PitchMaxDegrees);
            orbit.VerticalAxis.Center = profile.PitchDefaultDegrees;
            orbit.VerticalAxis.Value = profile.PitchDefaultDegrees;
            orbit.VerticalAxis.Wrap = false;
        }

        public void Follow(Transform target)
        {
            virtualCamera.Follow = target;
            virtualCamera.LookAt = target;
        }

        /// <summary>Applies one frame of look input (mouse delta). Pitch stays inside the clamp.</summary>
        public void Orbit(Vector2 look)
        {
            var degrees = look * (profile.LookSensitivity * SensitivityScale);
            orbit.HorizontalAxis.Value = Mathf.Repeat(orbit.HorizontalAxis.Value + degrees.x + 180f, 360f) - 180f;
            orbit.VerticalAxis.Value = ClampPitch(orbit.VerticalAxis.Value - degrees.y, profile);
        }

        /// <summary>Shakes in proportion to a received impulse (N·s). Returns false when shake is off.</summary>
        public bool Shake(float receivedImpulse)
        {
            if (!shakeEnabled || receivedImpulse <= 0f) return false;
            impulse.GenerateImpulseWithForce(receivedImpulse * profile.ShakePerImpulse);
            return true;
        }

        public static float ClampPitch(float degrees, PresentationProfile profile) =>
            Mathf.Clamp(degrees, profile.PitchMinDegrees, profile.PitchMaxDegrees);
    }
}
