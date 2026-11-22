using System;
using System.Collections.Generic;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.Hazards
{
    /// <summary>Sàn H06. Chỉ host chọn ô và bật collider; client trình bày sự kiện tin cậy.</summary>
    public sealed class IceTiles : MonoBehaviour, IHazard, IBotHazardHints
    {
        [SerializeField] private IceProfile profile;
        [SerializeField] private BalanceProfile balance;
        [SerializeField] private HazardProfile hazards;
        [SerializeField] private Material tileMaterial;
        [SerializeField] private bool chaos;
        private IceField field;
        private IFighterPhysics fighters;
        private IGameEventSink sink;
        private Mesh mesh;
        private PhysicsMaterial friction;
        private Material material, lineMaterial;
        private MeshCollider[] floors;
        private Transform[] visuals;
        private Renderer[] renderers;
        private LineRenderer[] cracks;
        private IceStage[] visibleStages;
        private float[] started;
        // Giữ chuyển trạng thái qua các chu kỳ: client có thể render trước event mới nhất.
        // Lịch sử chỉ sống trong một match (tối đa 150 s), không tích lũy qua session.
        private List<(IceStage Stage, float At)>[] clientTransitions;
        private MaterialPropertyBlock tint;
        private int playingTick;
        public IceField Field => field;
        public int TileCount => floors?.Length ?? 0;
        public Collider TileCollider(int index) => floors[index];
        public Vector3 TileCenter(int index)
        {
            var h = field.Cells[index];
            return transform.TransformPoint(new Vector3(profile.FlatToFlat * (h.Q + h.R * 0.5f), 0,
                profile.FlatToFlat * Mathf.Sqrt(3f) * 0.5f * h.R));
        }

        public void Configure(IceProfile ice, BalanceProfile physics, HazardProfile telegraphs, Material surface, bool isChaos = false)
        { profile = ice; balance = physics; hazards = telegraphs; tileMaterial = surface; chaos = isChaos; }

        private void Awake()
        {
            tint = new MaterialPropertyBlock();
            if (profile == null || balance == null || hazards == null)
                throw new InvalidOperationException("IceTiles cần IceProfile, BalanceProfile và HazardProfile.");
            field = new IceField(IceRules.FromProfile(profile, Time.fixedDeltaTime, chaos), 0);
            field.Changed += OnChanged;
            mesh = HexPrism(profile.FlatToFlat, profile.Thickness);
            friction = new PhysicsMaterial("H06 μ ice") { dynamicFriction = balance.IceFriction,
                staticFriction = balance.IceFriction, frictionCombine = PhysicsMaterialCombine.Minimum };
            material = tileMaterial != null ? new Material(tileMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            // URP/Unlit is stripped from the player (Shader.Find → null aborted Awake: no floor); Sprites/Default ships, like RingDrop.
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            floors = new MeshCollider[field.Count]; visuals = new Transform[field.Count];
            renderers = new Renderer[field.Count]; cracks = new LineRenderer[field.Count];
            visibleStages = new IceStage[field.Count]; started = new float[field.Count];
            clientTransitions = new List<(IceStage Stage, float At)>[field.Count];
            for (var i = 0; i < field.Count; i++)
            {
                clientTransitions[i] = new List<(IceStage Stage, float At)>();
                var tile = new GameObject($"Ice {i} ({field.Cells[i].Q},{field.Cells[i].R})");
                tile.transform.SetParent(transform, false); tile.transform.position = TileCenter(i);
                floors[i] = tile.AddComponent<MeshCollider>(); floors[i].sharedMesh = mesh; floors[i].sharedMaterial = friction;
                var visual = new GameObject("Visual"); visual.transform.SetParent(tile.transform, false);
                visual.AddComponent<MeshFilter>().sharedMesh = mesh;
                renderers[i] = visual.AddComponent<MeshRenderer>(); renderers[i].sharedMaterial = material; visuals[i] = visual.transform;
                var line = new GameObject("White crack warning"); line.transform.SetParent(visual.transform, false);
                cracks[i] = line.AddComponent<LineRenderer>(); cracks[i].sharedMaterial = lineMaterial;
                cracks[i].useWorldSpace = false; cracks[i].widthMultiplier = 0.035f;
                cracks[i].startColor = cracks[i].endColor = Color.white;
                cracks[i].positionCount = 7;
                var radius = profile.FlatToFlat / Mathf.Sqrt(3f) * 0.85f;
                // Ba nhánh trắng, khác hình với sàn nguyên và không phụ thuộc màu đỏ.
                cracks[i].SetPositions(new[] { Vector3.up * 0.015f,
                    new Vector3(radius, 0.015f, 0), Vector3.up * 0.015f,
                    new Vector3(-radius * 0.5f, 0.015f, radius * 0.8660254f), Vector3.up * 0.015f,
                    new Vector3(-radius * 0.5f, 0.015f, -radius * 0.8660254f), Vector3.up * 0.015f });
                cracks[i].enabled = false;
            }
            RenderAt(0);
        }

        public void SetSeed(uint seed)
        {
            if (playingTick != 0) throw new InvalidOperationException("Chỉ đặt seed trước GO.");
            field = new IceField(IceRules.FromProfile(profile, Time.fixedDeltaTime, chaos), seed);
            field.Changed += OnChanged;
        }
        public void Bind(IGameEventSink eventSink, IFighterPhysics physics)
        { sink = eventSink; fighters = physics; }
        public void CollectBotWarnings(List<BotHazardWarning> warnings, int playingTicks, float dt)
        {
            for (var i = 0; i < field.Count; i++)
            {
                if (field.Stage(i) != IceStage.Cracking) continue;
                var center = TileCenter(i);
                var remaining = profile.CrackSeconds - (playingTicks - field.ChangedAt(i)) * dt;
                warnings.Add(new BotHazardWarning(BotAwareness.IceTiles, BotHazardShape.Hex,
                    new Vector2(center.x, center.z), new Vector2(profile.FlatToFlat, 0),
                    transform.eulerAngles.y, Mathf.Max(0, remaining)));
            }
        }
        private uint currentHostTick;
        public void StepOnHost(int ticks, uint hostTick, float dt)
        {
            if (fighters == null) return;
            playingTick = ticks; currentHostTick = hostTick;
            field.Step(ticks, StandingCount, Overlaps);
            RenderAt(ticks * dt);
        }
        private void OnChanged(int index, IceStage stage, int tick)
        {
            visibleStages[index] = stage; started[index] = tick * Time.fixedDeltaTime;
            floors[index].enabled = stage == IceStage.Solid || stage == IceStage.Cracking;
            if (sink == null) return;
            if (stage == IceStage.Cracking)
                sink.Publish(new HazardEvent(sink.NextEventId(), currentHostTick +
                    (uint)Mathf.RoundToInt(profile.CrackSeconds / Time.fixedDeltaTime), HazardEventKind.IceTileWarning, index));
            else if (stage == IceStage.Fading || stage == IceStage.Solid)
                sink.Publish(new HazardEvent(sink.NextEventId(), currentHostTick,
                    stage == IceStage.Fading ? HazardEventKind.IceTileFading : HazardEventKind.IceTileSolid, index));
        }
        private int StandingCount(int index)
        {
            var count = 0;
            foreach (var id in fighters.FighterIds)
            {
                var p = fighters.GetPosition(id);
                // Raycast xác nhận chính collider đang đỡ quả, không tính quả đang nhảy qua ô.
                if (UnityEngine.Physics.Raycast(p, Vector3.down, out var hit, profile.FlatToFlat,
                    ~(1 << LayerMask.NameToLayer("Fighter")), QueryTriggerInteraction.Ignore) && hit.collider == floors[index]) count++;
            }
            return count;
        }
        private bool Overlaps(int index)
        {
            var center = TileCenter(index);
            // Broadphase đủ bao hex prism; kiểm tra bằng collider thật của từng quả.
            var extent = new Vector3(profile.FlatToFlat / Mathf.Sqrt(3f), profile.Thickness * 0.5f,
                profile.FlatToFlat / Mathf.Sqrt(3f));
            return UnityEngine.Physics.OverlapBox(center + Vector3.down * profile.Thickness * 0.5f, extent,
                transform.rotation, 1 << LayerMask.NameToLayer("Fighter"), QueryTriggerInteraction.Ignore).Length > 0;
        }

        /// <summary>App đổi host tick sang thời gian từ GO. Không tự chọn ô hay tự cho ô đứng được trên client.</summary>
        public void ApplyEvent(HazardEvent e, float effectiveSeconds)
        {
            if (fighters != null || e.Index < 0 || e.Index >= TileCount) return;
            var i = e.Index;
            if (e.Kind == HazardEventKind.IceTileWarning)
            {
                clientTransitions[i].Add((IceStage.Cracking, effectiveSeconds - profile.CrackSeconds));
                clientTransitions[i].Add((IceStage.Fallen, effectiveSeconds));
            }
            else if (e.Kind == HazardEventKind.IceTileFading) clientTransitions[i].Add((IceStage.Fading, effectiveSeconds));
            else if (e.Kind == HazardEventKind.IceTileSolid) clientTransitions[i].Add((IceStage.Solid, effectiveSeconds));
        }
        public void ShowAt(float seconds)
        {
            if (fighters != null) return;
            for (var i = 0; i < TileCount; i++)
            {
                var stage = IceStage.Solid; var at = 0f;
                foreach (var transition in clientTransitions[i])
                    if (seconds >= transition.At) { stage = transition.Stage; at = transition.At; }
                visibleStages[i] = stage; started[i] = at;
                // Client không mô phỏng vật lý, nhưng collider vẫn phản ánh đúng sàn để query trình bày.
                floors[i].enabled = stage == IceStage.Solid || stage == IceStage.Cracking;
            }
            RenderAt(seconds);
        }
        private void RenderAt(float seconds)
        {
            for (var i = 0; i < TileCount; i++)
            {
                var stage = visibleStages[i]; var age = Mathf.Max(0, seconds - started[i]);
                var fallingY = -0.5f * balance.Gravity * age * age;
                renderers[i].enabled = stage != IceStage.Fallen || fallingY > balance.KillPlaneY;
                cracks[i].enabled = stage == IceStage.Cracking;
                cracks[i].transform.localScale = Vector3.one *
                    (stage == IceStage.Cracking ? Mathf.Lerp(0.1f, 1f, Mathf.Clamp01(age / profile.CrackSeconds)) : 1f);
                var shake = stage == IceStage.Cracking ? Mathf.Sin(age * 50f) * profile.ShakeMeters : 0;
                visuals[i].localPosition = new Vector3(shake, stage == IceStage.Fallen ? fallingY : 0, 0);
                visuals[i].localScale = Vector3.one * (stage == IceStage.Fading ? Mathf.Lerp(0.8f, 0.98f,
                    Mathf.Clamp01(age / profile.FadeSeconds)) : 1f);
                var color = stage == IceStage.Cracking && (int)(age * hazards.TelegraphBlinkHz * 2) % 2 == 0
                    ? TelegraphColors.Red : stage == IceStage.Fading ? Color.Lerp(new Color(0.12f, 0.2f, 0.3f),
                        new Color(0.45f, 0.7f, 0.8f), Mathf.Clamp01(age / profile.FadeSeconds)) : new Color(0.65f, 0.88f, 0.96f);
                if (stage == IceStage.Fading) color.a = Mathf.Lerp(0.1f, 0.7f, Mathf.Clamp01(age / profile.FadeSeconds));
                tint.SetColor("_BaseColor", color); renderers[i].SetPropertyBlock(tint);
            }
        }
        private static Mesh HexPrism(float width, float depth)
        {
            var vertices = new Vector3[14]; var triangles = new int[72]; var cursor = 0;
            vertices[0] = Vector3.zero; vertices[7] = Vector3.down * depth;
            for (var i = 0; i < 6; i++)
            {
                var angle = (30 + 60 * i) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * width / Mathf.Sqrt(3f), 0, Mathf.Sin(angle) * width / Mathf.Sqrt(3f));
                vertices[i + 8] = vertices[i + 1] + Vector3.down * depth;
            }
            for (var i = 0; i < 6; i++)
            {
                int a = i + 1, b = (i + 1) % 6 + 1;
                foreach (var v in new[] { 0, b, a, 7, a + 7, b + 7, a, b, a + 7, b, b + 7, a + 7 }) triangles[cursor++] = v;
            }
            var result = new Mesh { name = "H06 procedural hex prism", vertices = vertices, triangles = triangles };
            result.RecalculateNormals(); result.RecalculateBounds(); return result;
        }
        private void OnDestroy()
        { Destroy(mesh); Destroy(friction); Destroy(material); Destroy(lineMaterial); }
    }
}
