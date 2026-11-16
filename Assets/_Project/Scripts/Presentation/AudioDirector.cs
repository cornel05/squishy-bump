using UnityEngine;

namespace SquishyBump.Presentation
{
    /// <summary>The twelve placeholder effects of GDD 12.7, in the order of <see cref="PresentationProfile"/>'s list.</summary>
    public enum Sfx
    {
        Bump, SquishBump, Dash, Jump, Cannon, IceCrack, RingDrop, ObstacleWhoosh, PusherHorn, FallOff, Respawn, Win,
    }

    /// <summary>
    /// Plays effects for gameplay events on this peer (GDD 12.7). Presentation only: App calls it from events,
    /// it never touches game state. A small pool of 3D-ish sources so overlapping sounds do not cut each other.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        private const int Voices = 12;
        // Mostly 2D so the toy sounds stay readable from the follow camera, with a little positional cue.
        private const float SpatialBlend = 0.4f;

        [SerializeField] private PresentationProfile profile;
        private AudioSource[] voices;
        private int next;

        public Sfx? LastPlayed { get; private set; }
        public float LastPitch { get; private set; }

        public void Configure(PresentationProfile presentationProfile) => profile = presentationProfile;

        private void Awake()
        {
            voices = new AudioSource[Voices];
            for (var i = 0; i < Voices; i++)
            {
                var go = new GameObject($"Voice {i}");
                go.transform.SetParent(transform, false);
                voices[i] = go.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = SpatialBlend;
            }
        }

        public void Play(Sfx id, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            LastPlayed = id;
            LastPitch = pitch;
            var clip = profile != null ? profile.Clip(id) : null;
            if (clip == null) return;
            var voice = voices[next];
            next = (next + 1) % voices.Length;
            voice.transform.position = position;
            voice.pitch = pitch;
            voice.volume = Mathf.Clamp01(volume);
            voice.clip = clip;
            voice.Play();
        }

        /// <summary>Bump (GDD 3.4, 12.7): soft bump below the strong-hit impulse, else a squish whose pitch rises with it.</summary>
        public void PlayHit(float impulse, Vector3 position)
        {
            if (impulse < profile.StrongHitImpulse)
                Play(Sfx.Bump, position, impulse / profile.StrongHitImpulse);
            else
                Play(Sfx.SquishBump, position, 1f, SquishPitch(impulse, profile));
        }

        public static float SquishPitch(float impulse, PresentationProfile p) =>
            Mathf.Lerp(p.SquishPitch.x, p.SquishPitch.y,
                Mathf.InverseLerp(p.StrongHitImpulse, p.SquishTopImpulse, impulse));
    }
}
