using System;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Hazards
{
    public enum BandStage { Solid, Warning, FinalWarning, Dropped }

    /// <summary>
    /// H01 Ring drop (GDD 7.1). Bands are ordered outermost first. The host calls <see cref="StepOnHost"/>
    /// once per playing tick: 3 s before a drop it publishes a reliable warning, at the drop tick the band's
    /// collider turns off so a fruit on it falls with it. Telegraph and fall visuals run on every peer from
    /// <see cref="BeginWarning"/>, which App wires to the <see cref="HazardEventKind.RingDropWarning"/> event.
    /// </summary>
    public sealed class RingDrop : MonoBehaviour
    {
        [SerializeField] private BalanceProfile balance;
        [SerializeField] private HazardProfile hazards;
        [Tooltip("Bands, outermost first.")]
        [SerializeField] private Collider[] bands = Array.Empty<Collider>();
        [SerializeField] private Color warningColor = new(1f, 0.65f, 0f);
        [SerializeField] private Color finalWarningColor = Color.red;

        private IGameEventSink sink;
        private bool[] warned;
        private bool[] dropped;
        private float[] warningStartedAt;
        private float[] fallSpeed;
        private LineRenderer[] seams;

        public int BandCount => bands.Length;
        public bool IsDropped(int band) => dropped[band];
        public event Action<int> BandDropped;

        private void Awake()
        {
            var n = bands.Length;
            warned = new bool[n];
            dropped = new bool[n];
            fallSpeed = new float[n];
            warningStartedAt = new float[n];
            for (var i = 0; i < n; i++) warningStartedAt[i] = float.NaN;
            seams = new LineRenderer[n];
            for (var i = 0; i < n; i++) seams[i] = CreateSeam(i);
        }

        /// <summary>Host only: where warnings are published.</summary>
        public void Bind(IGameEventSink eventSink) => sink = eventSink;

        /// <summary>Host only. <paramref name="playingTicks"/> counts ticks since GO (MatchDirector.ElapsedTicks).</summary>
        public void StepOnHost(int playingTicks, uint hostTick, float dt)
        {
            for (var i = 0; i < bands.Length; i++)
            {
                var dropTick = DropTick(i, balance, dt);
                var warningTick = dropTick - Mathf.RoundToInt(balance.RingDropWarningSeconds / dt);
                if (!warned[i] && playingTicks >= warningTick)
                {
                    warned[i] = true;
                    var effectiveTick = hostTick + (uint)Mathf.Max(0, dropTick - playingTicks);
                    if (sink != null)
                        sink.Publish(new HazardEvent(sink.NextEventId(), effectiveTick, HazardEventKind.RingDropWarning, i));
                    else
                        BeginWarning(i);
                }

                if (!dropped[i] && playingTicks >= dropTick) Drop(i);
            }
        }

        /// <summary>Every peer: starts the seam telegraph of a band.</summary>
        public void BeginWarning(int band)
        {
            if (band < 0 || band >= bands.Length || !float.IsNaN(warningStartedAt[band])) return;
            warningStartedAt[band] = Time.time;
        }

        private void Drop(int band)
        {
            dropped[band] = true;
            bands[band].enabled = false;
            BandDropped?.Invoke(band);
        }

        private void Update()
        {
            for (var i = 0; i < bands.Length; i++)
            {
                var started = warningStartedAt[i];
                var elapsed = float.IsNaN(started) ? -1f : Time.time - started;
                var stage = dropped[i] ? BandStage.Dropped : StageAt(elapsed, balance, hazards);
                // Clients have no collider to turn off: their visual falls when the warning runs out.
                if (stage == BandStage.Dropped) Fall(i);
                UpdateSeam(i, stage, elapsed);
            }
        }

        private void Fall(int band)
        {
            var t = bands[band].transform;
            if (!t.gameObject.activeSelf) return;
            fallSpeed[band] += balance.Gravity * Time.deltaTime;
            t.position += Vector3.down * (fallSpeed[band] * Time.deltaTime);
            if (t.position.y < balance.KillPlaneY) t.gameObject.SetActive(false);
        }

        private void UpdateSeam(int band, BandStage stage, float elapsed)
        {
            var seam = seams[band];
            switch (stage)
            {
                case BandStage.Warning:
                    seam.enabled = true;
                    seam.startColor = seam.endColor = warningColor;
                    break;
                case BandStage.FinalWarning:
                    seam.enabled = BlinkOn(elapsed - (balance.RingDropWarningSeconds - hazards.RingDropFinalWarningSeconds),
                        hazards.TelegraphBlinkHz);
                    seam.startColor = seam.endColor = finalWarningColor;
                    break;
                default:
                    seam.enabled = false;
                    break;
            }
        }

        private LineRenderer CreateSeam(int band)
        {
            var go = new GameObject($"Seam {band}");
            go.transform.SetParent(bands[band].transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = 0.15f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            var radius = SeamRadius(band, bands.Length, balance.RingWidthMeters);
            const int segments = 96;
            line.positionCount = segments;
            for (var s = 0; s < segments; s++)
            {
                var a = s * Mathf.PI * 2f / segments;
                line.SetPosition(s, new Vector3(Mathf.Cos(a) * radius, 0.02f, Mathf.Sin(a) * radius));
            }
            line.enabled = false;
            return line;
        }

        /// <summary>Tick since GO at which band <paramref name="band"/> (outermost = 0) drops.</summary>
        public static int DropTick(int band, BalanceProfile balance, float dt) =>
            Mathf.RoundToInt(balance.RingDropSeconds[band] / dt);

        /// <summary>Telegraph stage <paramref name="elapsed"/> seconds after the warning began (negative: none yet).</summary>
        public static BandStage StageAt(float elapsed, BalanceProfile balance, HazardProfile hazards)
        {
            if (elapsed < 0f) return BandStage.Solid;
            if (elapsed >= balance.RingDropWarningSeconds) return BandStage.Dropped;
            return elapsed >= balance.RingDropWarningSeconds - hazards.RingDropFinalWarningSeconds
                ? BandStage.FinalWarning
                : BandStage.Warning;
        }

        /// <summary>Square-wave blink: on for the first half of each period.</summary>
        public static bool BlinkOn(float seconds, float hz) => Mathf.Repeat(seconds * hz, 1f) < 0.5f;

        /// <summary>The band's inner seam; the last disc has none, so its outer edge flashes.</summary>
        public static float SeamRadius(int band, int bandCount, float width)
        {
            var inner = (bandCount - band - 1) * width;
            return inner > 0f ? inner : width;
        }
    }
}
