using System;
using System.Collections.Generic;
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
    public sealed class RingDrop : MonoBehaviour, IHazard, IBotHazardHints
    {
        public const float InitialWaterY = -0.25f;
        public const float SubmergedWaterY = 0.35f;
        public const float WaterRiseDropDepth = 1.2f;

        [SerializeField] private BalanceProfile balance;
        [SerializeField] private HazardProfile hazards;
        [Tooltip("Bands, outermost first.")]
        [SerializeField] private Collider[] bands = Array.Empty<Collider>();
        [Header("L1 Rising Water (optional)")]
        [SerializeField] private Transform waterSurface;
        [SerializeField] private Transform floatsRoot;

        private IGameEventSink sink;
        private bool[] warned;
        private bool[] dropped;
        private float[] warningStartedAt;
        private float[] fallSpeed;
        private float[] initialBandY;
        private LineRenderer[] seams;
        private float currentWaterY = InitialWaterY;

        public int BandCount => bands.Length;
        public bool IsDropped(int band) => dropped[band];
        public Transform WaterSurface => waterSurface;
        public event Action<int> BandDropped;

        public void SetWaterSurface(Transform water, Transform floats = null)
        {
            waterSurface = water;
            floatsRoot = floats;
        }

        private void Awake()
        {
            var n = bands.Length;
            warned = new bool[n];
            dropped = new bool[n];
            fallSpeed = new float[n];
            initialBandY = new float[n];
            warningStartedAt = new float[n];
            for (var i = 0; i < n; i++)
            {
                warningStartedAt[i] = float.NaN;
                initialBandY[i] = bands[i] != null ? bands[i].transform.position.y : 0f;
            }
            seams = new LineRenderer[n];
            for (var i = 0; i < n; i++) seams[i] = CreateSeam(i);
            if (waterSurface == null)
            {
                var waterGo = GameObject.Find("Water");
                if (waterGo != null) waterSurface = waterGo.transform;
            }
            if (floatsRoot == null)
            {
                var floatsGo = GameObject.Find("CreativeFloats");
                if (floatsGo != null) floatsRoot = floatsGo.transform;
            }
            currentWaterY = InitialWaterY;
            ApplyWaterHeight(currentWaterY);
        }

        /// <summary>Host only: where warnings are published.</summary>
        public void Bind(IGameEventSink eventSink) => sink = eventSink;

        public void Bind(IGameEventSink eventSink, IFighterPhysics fighters) => sink = eventSink;

        public void CollectBotWarnings(List<BotHazardWarning> warnings, int playingTicks, float dt)
        {
            for (var i = 0; i < BandCount; i++)
            {
                var remaining = (DropTick(i, balance, dt) - playingTicks) * dt;
                if (!warned[i] || dropped[i] || remaining < 0) continue;
                warnings.Add(new BotHazardWarning(BotAwareness.RingDrop, BotHazardShape.Annulus,
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2((BandCount - i - 1) * balance.RingWidthMeters,
                        (BandCount - i) * balance.RingWidthMeters), 0, remaining));
            }
        }

        // Clients follow the RingDropWarning event instead (BeginWarning), so there is nothing to pose here.
        public void ShowAt(float playingSeconds) { }

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

        /// <summary>
        /// Every peer: starts the seam telegraph of a band. A client that hears the event late passes how far
        /// into the warning the host already is, so both show the same stage at the same host tick.
        /// </summary>
        public void BeginWarning(int band, float alreadyElapsedSeconds = 0f)
        {
            if (band < 0 || band >= bands.Length || !float.IsNaN(warningStartedAt[band])) return;
            // Negative: the client renders behind the host, so the telegraph starts a little later.
            warningStartedAt[band] = Time.time - alreadyElapsedSeconds;
        }

        /// <summary>
        /// Seconds since a telegraph began at host time <paramref name="hostNow"/>, for an event that acts at
        /// <paramref name="effectiveTick"/> after a lead of <paramref name="leadSeconds"/>.
        /// </summary>
        public static float TelegraphElapsed(uint effectiveTick, float leadSeconds, float hostNow, float dt) =>
            hostNow - (effectiveTick * dt - leadSeconds);

        private void Drop(int band)
        {
            dropped[band] = true;
            bands[band].enabled = false;
            BandDropped?.Invoke(band);
        }

        private void Update()
        {
            var totalDropProgress = 0f;
            for (var i = 0; i < bands.Length; i++)
            {
                var started = warningStartedAt[i];
                var elapsed = float.IsNaN(started) ? -1f : Time.time - started;
                var stage = dropped[i] ? BandStage.Dropped : StageAt(elapsed, balance, hazards);
                // Clients have no collider to turn off: their visual falls when the warning runs out.
                if (stage == BandStage.Dropped)
                {
                    Fall(i);
                    var t = bands[i].transform;
                    totalDropProgress += !t.gameObject.activeSelf
                        ? 1f
                        : Mathf.Clamp01((initialBandY[i] - t.position.y) / WaterRiseDropDepth);
                }
                UpdateSeam(i, stage, elapsed);
            }
            UpdateWater(totalDropProgress);
        }

        private void UpdateWater(float totalDropProgress)
        {
            if (waterSurface == null || bands.Length == 0) return;
            var low = Mathf.Clamp(Mathf.FloorToInt(totalDropProgress), 0, bands.Length);
            var frac = totalDropProgress - low;
            var targetY = Mathf.Lerp(TargetWaterY(low, bands.Length), TargetWaterY(low + 1, bands.Length), frac);
            if (targetY <= currentWaterY) return;
            currentWaterY = targetY;
            ApplyWaterHeight(currentWaterY);
        }

        private void ApplyWaterHeight(float y)
        {
            if (waterSurface != null)
            {
                var pos = waterSurface.position;
                pos.y = y;
                waterSurface.position = pos;
            }
            if (floatsRoot != null)
            {
                var pos = floatsRoot.position;
                pos.y = y - InitialWaterY;
                floatsRoot.position = pos;
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
                    seam.startColor = seam.endColor = TelegraphColors.Amber;
                    break;
                case BandStage.FinalWarning:
                    seam.enabled = BlinkOn(elapsed - (balance.RingDropWarningSeconds - hazards.RingDropFinalWarningSeconds),
                        hazards.TelegraphBlinkHz);
                    seam.startColor = seam.endColor = TelegraphColors.Red;
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

        /// <summary>Target water Y after <paramref name="droppedBands"/> of <paramref name="bandCount"/> bands have fallen.</summary>
        public static float TargetWaterY(int droppedBands, int bandCount)
        {
            if (bandCount <= 0 || droppedBands <= 0) return InitialWaterY;
            if (droppedBands >= bandCount) return SubmergedWaterY;
            var step = (0.05f - InitialWaterY) / (bandCount - 1);
            return InitialWaterY + droppedBands * step;
        }
    }
}
