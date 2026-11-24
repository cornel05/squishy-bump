using System;
using System.Collections.Generic;
using SquishyBump.Hazards;
using UnityEngine;

namespace SquishyBump.App
{
    public enum LevelArtMode { None, MovingGround, IceMeshTiles, IceBricks }

    /// <summary>
    /// Binds pre-placed Creative arena art to runtime-generated hazard visuals (L2 MovingGround, L4/L5 IceTiles).
    /// Runs on every peer without AssetDatabase so standalone builds mirror Editor scenes.
    /// </summary>
    public sealed class LevelArtBinder : MonoBehaviour
    {
        [Serializable]
        public struct IcePaletteEntry
        {
            public Vector2 Xz;
            public Color Color;
        }

        [Serializable]
        public struct IceBrickEntry
        {
            public Renderer Renderer;
            public Color Color;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private LevelArtMode mode;

        [Header("Moving Ground (L2)")]
        [SerializeField] private Transform turntableArt;
        [SerializeField] private Transform ringArt;
        [SerializeField] private float turntableLift = 0.002f;

        [Header("Ice Tiles (L4 / L5)")]
        [SerializeField] private Mesh bakedTileMesh;
        [SerializeField] private IcePaletteEntry[] palette;
        [SerializeField] private IceBrickEntry[] bricks;

        private Material runtimeMaterial;
        private readonly List<(Renderer Carrier, Renderer Art, Color BaseColor)> skins = new();
        private MaterialPropertyBlock carrierBlock;
        private MaterialPropertyBlock artBlock;

        public bool IsBound { get; private set; }
        public int BoundTileCount => skins.Count;

        public void ConfigureMovingGround(Transform turntableVisual, Transform ringVisual, float lift = 0.002f)
        {
            mode = LevelArtMode.MovingGround;
            turntableArt = turntableVisual;
            ringArt = ringVisual;
            turntableLift = lift;
        }

        public void ConfigureIceMeshTiles(Mesh tileMesh, IcePaletteEntry[] tilePalette)
        {
            mode = LevelArtMode.IceMeshTiles;
            bakedTileMesh = tileMesh;
            palette = tilePalette;
        }

        public void ConfigureIceBricks(IceBrickEntry[] boardBricks)
        {
            mode = LevelArtMode.IceBricks;
            bricks = boardBricks;
        }

        private void Start()
        {
            switch (mode)
            {
                case LevelArtMode.MovingGround:
                    BindMovingGround();
                    break;
                case LevelArtMode.IceMeshTiles:
                    BindIceMeshTiles();
                    break;
                case LevelArtMode.IceBricks:
                    BindIceBricks();
                    break;
            }
        }

        private void LateUpdate()
        {
            if (mode == LevelArtMode.IceMeshTiles || mode == LevelArtMode.IceBricks)
                SyncTileSkins();
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }

        private void BindMovingGround()
        {
            var moving = FindInScene<MovingGround>();
            if (moving == null || turntableArt == null || ringArt == null) return;

            var turntableTransform = moving.transform.Find("Turntable");
            var ringTransform = moving.transform.Find("Obstacle Ring");
            if (turntableTransform == null || ringTransform == null) return;

            var turntable = turntableTransform.GetComponent<Renderer>();
            var ring = ringTransform.GetComponent<Renderer>();
            if (turntable == null || ring == null) return;

            var centerRenderer = turntableArt.GetComponent<Renderer>();
            if (centerRenderer != null && centerRenderer.bounds.size.x > 0f)
            {
                var target = turntable.bounds;
                var current = centerRenderer.bounds;
                turntableArt.position += new Vector3(target.center.x - current.center.x,
                    target.max.y - current.max.y + turntableLift, target.center.z - current.center.z);
            }

            turntableArt.SetParent(turntable.transform, true);
            ringArt.SetParent(ring.transform, true);
            turntable.enabled = false;
            ring.enabled = false;
            IsBound = true;
        }

        private void BindIceMeshTiles()
        {
            var ice = FindInScene<IceTiles>();
            if (ice == null || ice.TileCount == 0 || bakedTileMesh == null || palette == null || palette.Length == 0)
                return;

            var carriers = CollectIceCarriers(ice);
            if (carriers.Length == 0) return;

            runtimeMaterial = CreateIceMaterial(carriers[0].sharedMaterial, "L4 Creative Ice Runtime");
            for (var i = 0; i < carriers.Length; i++)
            {
                var carrier = carriers[i];
                if (carrier == null) continue;
                carrier.forceRenderingOff = true;

                var tile = new GameObject("CreativeIceTile");
                tile.transform.SetParent(carrier.transform, false);
                tile.AddComponent<MeshFilter>().sharedMesh = bakedTileMesh;
                var art = tile.AddComponent<MeshRenderer>();
                art.sharedMaterial = runtimeMaterial;

                var center = carrier.bounds.center;
                var bestColor = palette[0].Color;
                var bestDist = float.MaxValue;
                for (var p = 0; p < palette.Length; p++)
                {
                    var dx = palette[p].Xz.x - center.x;
                    var dz = palette[p].Xz.y - center.z;
                    var dist = dx * dx + dz * dz;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestColor = palette[p].Color;
                    }
                }
                skins.Add((carrier, art, bestColor));
            }

            SyncTileSkins();
            IsBound = skins.Count > 0;
        }

        private void BindIceBricks()
        {
            var ice = FindInScene<IceTiles>();
            if (ice == null || ice.TileCount == 0 || bricks == null || bricks.Length == 0) return;

            var carriers = CollectIceCarriers(ice);
            if (carriers.Length == 0) return;

            runtimeMaterial = CreateIceMaterial(carriers[0].sharedMaterial, "L5 Creative Lego Runtime");
            for (var i = 0; i < carriers.Length; i++)
                if (carriers[i] != null) carriers[i].forceRenderingOff = true;

            for (var i = 0; i < bricks.Length; i++)
            {
                var brick = bricks[i].Renderer;
                if (brick == null) continue;

                var brickCenter = brick.bounds.center;
                Renderer nearest = null;
                var bestDist = float.MaxValue;
                for (var c = 0; c < carriers.Length; c++)
                {
                    var carrier = carriers[c];
                    if (carrier == null) continue;
                    var cc = carrier.bounds.center;
                    var dx = cc.x - brickCenter.x;
                    var dz = cc.z - brickCenter.z;
                    var dist = dx * dx + dz * dz;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        nearest = carrier;
                    }
                }
                if (nearest == null) continue;

                brick.sharedMaterial = runtimeMaterial;
                brick.transform.position += Vector3.up * (nearest.bounds.max.y - brick.bounds.max.y);
                brick.transform.SetParent(nearest.transform, true);
                skins.Add((nearest, brick, bricks[i].Color));
            }

            SyncTileSkins();
            IsBound = skins.Count > 0;
        }

        private void SyncTileSkins()
        {
            if (skins.Count == 0) return;
            carrierBlock ??= new MaterialPropertyBlock();
            artBlock ??= new MaterialPropertyBlock();

            for (var i = 0; i < skins.Count; i++)
            {
                var (carrier, art, baseColor) = skins[i];
                if (carrier == null || art == null) continue;
                art.enabled = carrier.enabled;

                Color applied;
                if (carrier.HasPropertyBlock())
                {
                    carrier.GetPropertyBlock(carrierBlock);
                    var state = carrierBlock.GetColor(BaseColorId);
                    bool redFlash = state.r > state.b;
                    applied = redFlash ? state : baseColor;
                    applied.a = state.a > 0f || redFlash || state != default ? state.a : 1f;
                }
                else
                {
                    applied = baseColor;
                    applied.a = 1f;
                }
                artBlock.SetColor(BaseColorId, applied);
                art.SetPropertyBlock(artBlock);
            }
        }

        private static Renderer[] CollectIceCarriers(IceTiles ice)
        {
            var carriers = new Renderer[ice.TileCount];
            for (var i = 0; i < ice.TileCount; i++)
            {
                var visual = ice.TileCollider(i).transform.Find("Visual");
                if (visual != null) carriers[i] = visual.GetComponent<Renderer>();
            }
            return carriers;
        }

        private static Material CreateIceMaterial(Material template, string name) =>
            template != null
                ? new Material(template) { name = name }
                : new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };

        private T FindInScene<T>() where T : Component
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
