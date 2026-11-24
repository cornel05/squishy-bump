using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SquishyBump.App;
using SquishyBump.Core;
using SquishyBump.Hazards;
using SquishyBump.Match;
using SquishyBump.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SquishyBump.Editor
{
    /// <summary>
    /// Builds L1–L5 scenes with promoted Creative 3D map art (SB-78). Re-running overwrites L1..L5.unity.
    /// Note: Settled per-level fighter visual scales from _scratch/fruit-scale-preview/README.md are
    /// L1: 0.95x, L2: 0.80x, L3: 0.60x, L4: 0.90x, L5: 0.85x (visual-only; kept at 1.0x in SB-78).
    /// Batchmode: -executeMethod SquishyBump.Editor.LevelArtSceneBuilder.BuildAll
    /// </summary>
    public static class LevelArtSceneBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ArtRoot = ProjectRoot + "/Art/Levels";
        private const string IceTileMeshPath = ArtRoot + "/L4/IceTile.asset";
        private const float L1IslandY = -6.636805f;
        private const float L1IslandScale = 2.6653836f;

        private static readonly HashSet<string> L4StaticTileNames = CreateL4StaticTileNames();

        [MenuItem("Squishy Bump/Build All Levels With Art (L1-L5)")]
        public static void BuildAll()
        {
            try
            {
                BuildL1();
                BuildL2();
                BuildL3();
                BuildL4();
                BuildL5();
                AssetDatabase.SaveAssets();
                Debug.Log("[LevelArtSceneBuilder] Built L1-L5 with Creative art.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [MenuItem("Squishy Bump/Build Level Art/L1 (Training Ring)")]
        public static void BuildL1()
        {
            var level = LoadLevel(1);
            var balance = LoadBalance();
            var floorPrefab = LoadAsset<GameObject>($"{ProjectRoot}/Prefabs/Hazards/TrainingRingFloor.prefab");
            var bandMat = LoadAsset<Material>($"{ArtRoot}/L1/Band.mat");
            var waterMat = LoadAsset<Material>($"{ArtRoot}/L1/Water.mat");
            var mapAsset = LoadAsset<GameObject>($"{ArtRoot}/L1/Map1.glb");
            ConfigureL1SandMaterial(bandMat, $"{ArtRoot}/L1/sand01_Normal.jpg");
            var woodMat = LoadOrCreateWoodMaterial($"{ArtRoot}/L1/WoodPlank.mat", $"{ArtRoot}/L1/WoodPlank.png");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Camera.main.transform.SetPositionAndRotation(new Vector3(0f, 9f, -12f), Quaternion.Euler(35f, 0f, 0f));

            var floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab);
            var bandTransforms = new Transform[4];
            var bandOuterRadii = new float[4];
            for (var i = 0; i < 4; i++)
            {
                var bandTransform = floor.transform.Find($"Band {i}")
                    ?? throw new InvalidOperationException($"Missing Band {i} on TrainingRingFloor.");
                bandTransforms[i] = bandTransform;
                var bandCollider = bandTransform.GetComponent<MeshCollider>();
                bandOuterRadii[i] = bandCollider != null ? bandCollider.bounds.extents.x : 0f;
                var bandRenderer = bandTransform.GetComponent<MeshRenderer>();
                if (bandRenderer != null) bandRenderer.enabled = false;

                var vongAsset = LoadAsset<GameObject>($"{ArtRoot}/L1/Vong{i + 1}.glb");
                var fixedMesh = LoadAsset<Mesh>($"{ArtRoot}/L1/Fixed_Vong{i + 1}.asset");
                EnsurePlanarUvAndTangents(fixedMesh);
                var pivot = new GameObject($"Vong{i + 1}Pivot");
                pivot.transform.SetParent(bandTransform, false);
                pivot.transform.localPosition = new Vector3(0f, L1IslandY, 0f);
                pivot.transform.localScale = Vector3.one * L1IslandScale;

                var vong = (GameObject)PrefabUtility.InstantiatePrefab(vongAsset, pivot.transform);
                vong.name = $"Vong{i + 1}";
                StripNonVisuals(vong);
                var mf = vong.GetComponentInChildren<MeshFilter>(true)
                    ?? throw new InvalidOperationException($"Missing MeshFilter on Vong{i + 1}.");
                var mr = vong.GetComponentInChildren<MeshRenderer>(true)
                    ?? throw new InvalidOperationException($"Missing MeshRenderer on Vong{i + 1}.");
                mf.sharedMesh = fixedMesh;
                mr.sharedMaterial = bandMat;
                if (Mathf.Abs(mr.bounds.max.y) > 0.05f || mr.bounds.extents.x < 5f)
                    throw new InvalidOperationException($"Unexpected Vong{i + 1} bounds {mr.bounds}.");
            }

            var artRoot = new GameObject("Creative L1 Art");
            var island = (GameObject)PrefabUtility.InstantiatePrefab(mapAsset, artRoot.transform);
            island.name = "Creative_Map1";
            island.transform.localPosition = new Vector3(0f, L1IslandY, 0f);
            island.transform.localScale = Vector3.one * L1IslandScale;
            StripNonVisuals(island);
            PrefabUtility.UnpackPrefabInstance(island, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var leftoverNames = new[]
            {
                "Cylinder.001", "Cylinder.002", "Cylinder.003", "Cylinder.004",
                "Torus.001", "Torus.002", "Torus.003", "Torus.004", "Torus.005", "Torus.006",
                "Sphere.006", "Cube.001", "Circle.001", "Circle.002", "Circle.003", "Circle.009",
            };
            foreach (var junkName in leftoverNames)
            {
                var junk = FindChild(island.transform, junkName);
                if (junk != null) Object.DestroyImmediate(junk.gameObject);
            }

            foreach (var mr in island.GetComponentsInChildren<MeshRenderer>(true).ToArray())
            {
                if (mr == null) continue;
                var child = mr.transform;
                if (child.name.StartsWith("Plane.", StringComparison.Ordinal))
                {
                    var s = child.localScale;
                    if (s.x < 0f || s.y < 0f || s.z < 0f)
                        child.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                    mr.sharedMaterial = woodMat;
                }

                var center = mr.bounds.center;
                var r = Mathf.Sqrt(center.x * center.x + center.z * center.z);
                var targetBand = 0;
                for (var b = 3; b >= 0; b--)
                {
                    if (r <= bandOuterRadii[b]) { targetBand = b; break; }
                }
                child.SetParent(bandTransforms[targetBand], true);
            }

            if (island.GetComponentsInChildren<Renderer>(true).Length != 0)
                throw new InvalidOperationException("L1 Creative_Map1 still has unparented renderers after Band assignment.");
            Object.DestroyImmediate(island);

            var floatsRoot = new GameObject("CreativeFloats");
            floatsRoot.transform.SetParent(artRoot.transform, false);
            var floatPlacements = new[]
            {
                ("PhaoTron1.glb", "PhaoTron1", new Vector3(-17.180616f, -2.4056528f, -23.788546f), Quaternion.identity, 0f),
                ("PhaoUnicorn1.glb", "PhaoUnicorn1", new Vector3(17.180616f, -2.4056528f, -26.431717f), new Quaternion(0f, 0.47715876f, 0f, 0.87881714f), 1.3f),
                ("PhaoTron2.glb", "PhaoTron2", new Vector3(-17.84141f, -2.4056528f, 2.6431718f), new Quaternion(0f, 0.83867055f, 0f, 0.54463905f), 2.6f),
                ("PhaoUnicorn2.glb", "PhaoUnicorn2", new Vector3(17.84141f, -2.4056528f, 7.9295154f), new Quaternion(0f, 0.99691737f, 0f, 0.07845909f), 3.9f),
                ("PhaoTron3.glb", "PhaoTron3", new Vector3(-14.537445f, -2.4056528f, 27.753304f), new Quaternion(0f, 0.9135455f, 0f, -0.40673658f), 5.2f),
                ("PhaoLgbt.glb", "PhaoLgbt", new Vector3(13.215858f, -2.4056528f, 29.07489f), new Quaternion(0f, 0.6087614f, 0f, -0.7933533f), 6.5f),
            };
            foreach (var (file, name, pos, rot, phase) in floatPlacements)
            {
                var asset = LoadAsset<GameObject>($"{ArtRoot}/L1/{file}");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, floatsRoot.transform);
                go.name = name;
                go.transform.SetLocalPositionAndRotation(pos, rot);
                StripNonVisuals(go);
                go.AddComponent<FloatBob>().Configure(0.15f, 1.2f, 8f, phase);
            }

            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.SetParent(artRoot.transform, false);
            water.transform.localPosition = new Vector3(0f, -2.1056528f, 0f);
            water.transform.localScale = new Vector3(4.361233f, 1f, 7.202643f);
            water.GetComponent<Renderer>().sharedMaterial = waterMat;

            CreateArenaBounds(level, balance);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, level.ScenePath);
            Debug.Log($"[LevelArtSceneBuilder] Built {level.ScenePath}");
        }

        [MenuItem("Squishy Bump/Build Level Art/L2 (Moving Ground)")]
        public static void BuildL2()
        {
            LevelSceneBuilder.BuildL2();
            var scene = EditorSceneManager.GetActiveScene();
            var level = LoadLevel(2);
            var hazards = LoadHazards();

            var ringBounds = new Bounds(Vector3.zero,
                new Vector3(hazards.ObstacleRingOuterRadius * 2f, 0f, hazards.ObstacleRingOuterRadius * 2f));
            var turntableBounds = new Bounds(Vector3.zero,
                new Vector3(hazards.TurntableRadius * 2f, 0f, hazards.TurntableRadius * 2f));

            var (root, model) = SpawnArt($"{ArtRoot}/L2/L2.glb", "Creative L2 Art");
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var copper = renderers.Single(r => r.name == "Cylinder");
            var center = renderers.Single(r => r.name == "Cylinder.001");

            FitArt(root, copper.bounds, ringBounds, coverDepth: false);
            UnityEngine.Physics.SyncTransforms();

            center.transform.localScale *= turntableBounds.size.x / center.bounds.size.x;
            UnityEngine.Physics.SyncTransforms();
            AlignTop(center, turntableBounds, 0.002f);

            var obstacleFiles = new[] { "chuotlatdat.glb", "meolatdat.glb", "chuotlatdat.glb" };
            var obstaclesRoot = new GameObject("Obstacles").transform;
            obstaclesRoot.SetParent(root.transform, false);
            var obstacleArts = new Transform[obstacleFiles.Length];
            for (var i = 0; i < obstacleFiles.Length; i++)
            {
                var obstacleAsset = LoadAsset<GameObject>($"{ArtRoot}/L2/{obstacleFiles[i]}");
                var obstacle = (GameObject)PrefabUtility.InstantiatePrefab(obstacleAsset, obstaclesRoot);
                obstacle.name = $"ObstacleArt_{i}";
                StripNonVisuals(obstacle);
                obstacleArts[i] = obstacle.transform;
            }

            var binder = root.AddComponent<LevelArtBinder>();
            binder.ConfigureMovingGround(center.transform, copper.transform, 0.002f, obstacleArts);
            EditorUtility.SetDirty(binder);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, level.ScenePath);
            Debug.Log($"[LevelArtSceneBuilder] Built {level.ScenePath}");
        }

        [MenuItem("Squishy Bump/Build Level Art/L3 (Cannon Court)")]
        public static void BuildL3()
        {
            LevelSceneBuilder.BuildL3();
            var scene = EditorSceneManager.GetActiveScene();
            var level = LoadLevel(3);

            var floor = GameObject.Find("Floor r 9 m")?.GetComponent<Renderer>()
                ?? throw new InvalidOperationException("Missing Floor r 9 m in L3.");

            var (root, model) = SpawnArt($"{ArtRoot}/L3/Map3.glb", "Creative L3 Art");
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var disc = renderers.Single(r => r.name == "Cylinder.002");

            var target = floor.bounds;
            var raw = disc.bounds;
            float scale = target.extents.x / Mathf.Max(raw.extents.x, raw.extents.z);
            root.transform.localScale = Vector3.one * scale;
            var sourceTop = new Vector3(raw.center.x, raw.max.y, raw.center.z);
            var targetTop = new Vector3(target.center.x, target.max.y, target.center.z);
            root.transform.position = targetTop - sourceTop * scale;

            floor.enabled = false;
            var centerSphere = renderers.SingleOrDefault(r => r.name == "Sphere");
            if (centerSphere != null) centerSphere.enabled = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, level.ScenePath);
            Debug.Log($"[LevelArtSceneBuilder] Built {level.ScenePath}");
        }

        [MenuItem("Squishy Bump/Build Level Art/L4 (Frost Ring)")]
        public static void BuildL4()
        {
            IceSceneBuilder.BuildL4();
            var scene = EditorSceneManager.GetActiveScene();
            var level = LoadLevel(4);
            var iceProfile = LoadAsset<IceProfile>(IceSceneBuilder.ProfilePath);
            var (iceBounds, tileSize) = ComputeIceFieldBounds(iceProfile);

            var bakedMesh = BakeAndSaveL4TileMesh(Vector3.Scale(tileSize, new Vector3(0.975f, 1f, 0.975f)));
            var (root, model) = SpawnArt($"{ArtRoot}/L4/map 4.glb", "Creative L4 Art");
            var renderers = model.GetComponentsInChildren<Renderer>(true);

            var duplicateTiles = renderers.Where(r => L4StaticTileNames.Contains(r.name)).ToArray();
            if (duplicateTiles.Length != 37)
                throw new InvalidOperationException($"Expected 37 L4 static Ice Floor renderers, found {duplicateTiles.Length}.");

            FitArt(root, BoundsOf(duplicateTiles), iceBounds, coverDepth: false);
            UnityEngine.Physics.SyncTransforms();

            var palette = duplicateTiles.Select(tile =>
            {
                var c = tile.bounds.center;
                return new LevelArtBinder.IcePaletteEntry
                {
                    Xz = new Vector2(c.x, c.z),
                    Color = ReadArtColor(tile.sharedMaterial),
                };
            }).ToArray();

            if (palette.Select(e => e.Color).Distinct().Count() != 2)
                throw new InvalidOperationException("L4 expected two authored ice colors.");

            foreach (var tile in duplicateTiles) tile.enabled = false;
            foreach (var r in renderers.Where(r => r.sharedMaterials.Length > 0 &&
                r.sharedMaterials.All(m => m != null && m.name.StartsWith("COLLIDER", StringComparison.Ordinal))))
            {
                r.enabled = false;
            }
            var bottomMarker = renderers.SingleOrDefault(r => r.name == "Cylinder.037");
            if (bottomMarker != null) bottomMarker.enabled = false;

            var binder = root.AddComponent<LevelArtBinder>();
            binder.ConfigureIceMeshTiles(bakedMesh, palette);
            EditorUtility.SetDirty(binder);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, level.ScenePath);
            Debug.Log($"[LevelArtSceneBuilder] Built {level.ScenePath}");
        }

        [MenuItem("Squishy Bump/Build Level Art/L5 (Chaos Core)")]
        public static void BuildL5()
        {
            IceSceneBuilder.BuildL5();
            var scene = EditorSceneManager.GetActiveScene();
            var level = LoadLevel(5);
            var iceProfile = LoadAsset<IceProfile>(IceSceneBuilder.ProfilePath);
            var (iceBounds, _) = ComputeIceFieldBounds(iceProfile);

            var (root, model) = SpawnArt($"{ArtRoot}/L5/L5.glb", "Creative L5 Art");
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                r.shadowCastingMode = ShadowCastingMode.Off;

            var board = renderers.Where(r => r.name.StartsWith("Brick2x", StringComparison.Ordinal)).ToArray();
            if (board.Length != 84)
                throw new InvalidOperationException($"Expected 84 Brick2x renderers on L5, found {board.Length}.");

            FitArt(root, BoundsOf(board), iceBounds, coverDepth: true);
            UnityEngine.Physics.SyncTransforms();

            var entries = board.Select(brick => new LevelArtBinder.IceBrickEntry
            {
                Renderer = brick,
                Color = ReadArtColor(brick.sharedMaterial),
            }).ToArray();

            var binder = root.AddComponent<LevelArtBinder>();
            binder.ConfigureIceBricks(entries);
            EditorUtility.SetDirty(binder);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, level.ScenePath);
            Debug.Log($"[LevelArtSceneBuilder] Built {level.ScenePath}");
        }

        private static void ConfigureL1SandMaterial(Material bandMat, string normalPath)
        {
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter importer)
            {
                var dirty = importer.textureType != TextureImporterType.NormalMap || importer.wrapMode != TextureWrapMode.Repeat;
                if (dirty)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.SaveAndReimport();
                }
            }
            var sandNormal = LoadAsset<Texture2D>(normalPath);
            // Creative cat vong 1..4 Sand material baseColorFactor (linear 0.800, 0.565, 0.125 -> sRGB).
            var sandColor = new Color(0.8001648f, 0.5653464f, 0.1247587f, 1f).gamma;
            bandMat.SetColor("_BaseColor", sandColor);
            bandMat.SetColor("_Color", sandColor);
            bandMat.SetTexture("_BumpMap", sandNormal);
            bandMat.SetTextureScale("_BaseMap", new Vector2(4f, 4f));
            bandMat.SetTextureScale("_BumpMap", new Vector2(4f, 4f));
            bandMat.SetFloat("_BumpScale", 0.9f);
            bandMat.SetFloat("_Smoothness", 0.25f);
            bandMat.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(bandMat);
        }

        private static Material LoadOrCreateWoodMaterial(string matPath, string texturePath)
        {
            var woodTex = LoadAsset<Texture2D>(texturePath);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? throw new InvalidOperationException("Missing Universal Render Pipeline/Lit shader.");
                mat = new Material(shader) { name = "WoodPlank" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", woodTex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Smoothness", 0.18f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsurePlanarUvAndTangents(Mesh mesh)
        {
            var dirty = false;
            if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
            {
                mesh.uv = mesh.vertices.Select(v => new Vector2(v.x, v.z) * 0.25f).ToArray();
                dirty = true;
            }
            if (dirty || mesh.tangents == null || mesh.tangents.Length != mesh.vertexCount)
            {
                mesh.RecalculateTangents();
                dirty = true;
            }
            if (dirty) EditorUtility.SetDirty(mesh);
        }

        private static (GameObject Root, GameObject Model) SpawnArt(string assetPath, string rootName)
        {
            var asset = LoadAsset<GameObject>(assetPath);
            var root = new GameObject(rootName);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            StripNonVisuals(model);
            return (root, model);
        }

        private static void StripNonVisuals(GameObject root)
        {
            foreach (var cam in root.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
            foreach (var light in root.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            foreach (var col in root.GetComponentsInChildren<Collider>(true)) col.enabled = false;
        }

        private static void CreateArenaBounds(LevelDefinition level, BalanceProfile balance)
        {
            var arena = new GameObject(level.ArenaName);
            var spawns = level.SpawnPoints.Select((p, i) =>
            {
                var t = new GameObject($"Spawn {i + 1}").transform;
                t.SetParent(arena.transform, false);
                t.position = p;
                return (Object)t;
            }).ToArray();
            var bounds = arena.AddComponent<TrainingRingBounds>();
            var so = new SerializedObject(bounds);
            so.FindProperty("outerRadius").floatValue = level.ArenaRadiusMeters;
            so.FindProperty("killPlaneY").floatValue = balance.KillPlaneY;
            var list = so.FindProperty("spawnPoints");
            list.arraySize = spawns.Length;
            for (var i = 0; i < spawns.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static (Bounds FieldBounds, Vector3 TileSize) ComputeIceFieldBounds(IceProfile profile)
        {
            var width = profile.FlatToFlat;
            var radius = width / Mathf.Sqrt(3f);
            var tileSize = new Vector3(width, profile.Thickness, 2f * radius);
            var rules = IceRules.FromProfile(profile, 1f / 60f, false);
            var field = new IceField(rules, 0);

            var bounds = default(Bounds);
            for (var i = 0; i < field.Cells.Count; i++)
            {
                var h = field.Cells[i];
                var center = new Vector3(
                    width * (h.Q + h.R * 0.5f),
                    -profile.Thickness * 0.5f,
                    width * Mathf.Sqrt(3f) * 0.5f * h.R);
                var tileBounds = new Bounds(center, tileSize);
                if (i == 0) bounds = tileBounds;
                else bounds.Encapsulate(tileBounds);
            }
            return (bounds, tileSize);
        }

        private static Mesh BakeAndSaveL4TileMesh(Vector3 targetSize)
        {
            var tileAsset = LoadAsset<GameObject>($"{ArtRoot}/L4/cuc bang 2.glb");
            var temp = Object.Instantiate(tileAsset);
            Mesh baked;
            try
            {
                var source = temp.GetComponentInChildren<MeshFilter>(true)
                    ?? throw new InvalidOperationException("Missing MeshFilter on L4 cuc bang 2.glb.");
                baked = BakeTile(source, targetSize);
                baked.name = "L4CreativeIceTile";
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(IceTileMeshPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(baked, existing);
                Object.DestroyImmediate(baked);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            EnsureFolder(Path.GetDirectoryName(IceTileMeshPath));
            AssetDatabase.CreateAsset(baked, IceTileMeshPath);
            return baked;
        }

        private static Mesh BakeTile(MeshFilter source, Vector3 targetSize)
        {
            var mesh = Object.Instantiate(source.sharedMesh);
            var matrix = source.transform.localToWorldMatrix;
            var vertices = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
            var bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (var vertex in vertices) bounds.Encapsulate(vertex);

            var anchor = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            var scale = new Vector3(
                targetSize.x / bounds.size.x,
                targetSize.y / bounds.size.y,
                targetSize.z / bounds.size.z);
            mesh.vertices = vertices.Select(v => Vector3.Scale(v - anchor, scale)).ToArray();

            var normals = matrix.inverse.transpose;
            mesh.normals = mesh.normals.Select(n =>
            {
                var world = normals.MultiplyVector(n);
                return new Vector3(world.x / scale.x, world.y / scale.y, world.z / scale.z).normalized;
            }).ToArray();

            if (matrix.determinant < 0f)
            {
                var triangles = mesh.triangles;
                for (var i = 0; i < triangles.Length; i += 3)
                    (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
                mesh.triangles = triangles;
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void FitArt(GameObject root, Bounds source, Bounds target, bool coverDepth)
        {
            var scale = target.size.x / source.size.x;
            if (coverDepth) scale = Mathf.Max(scale, target.size.z / source.size.z);
            root.transform.localScale = Vector3.one * scale;
            var sourceTop = new Vector3(source.center.x, source.max.y, source.center.z);
            var targetTop = new Vector3(target.center.x, target.max.y, target.center.z);
            root.transform.position = targetTop - sourceTop * scale;
        }

        private static void AlignTop(Renderer art, Bounds target, float lift)
        {
            var source = art.bounds;
            art.transform.position += new Vector3(
                target.center.x - source.center.x,
                target.max.y - source.max.y + lift,
                target.center.z - source.center.z);
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static Color ReadArtColor(Material material)
        {
            if (material == null) throw new InvalidOperationException("Cannot read authored color from null material.");
            if (material.HasProperty("baseColorFactor")) return material.GetColor("baseColorFactor");
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            throw new InvalidOperationException($"Cannot read authored color from {material.name}.");
        }

        private static Transform FindChild(Transform parent, string name) =>
            parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        private static HashSet<string> CreateL4StaticTileNames()
        {
            var set = new HashSet<string>(StringComparer.Ordinal) { "Cylinder" };
            for (var i = 1; i <= 36; i++) set.Add($"Cylinder.{i:000}");
            return set;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || Directory.Exists(path)) return;
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }

        private static T LoadAsset<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path)
            ?? throw new InvalidOperationException($"Missing asset: {path}");

        private static LevelDefinition LoadLevel(int number) =>
            LoadAsset<LevelDefinition>($"{ProjectRoot}/Data/Levels/L{number}.asset");

        private static BalanceProfile LoadBalance() =>
            LoadAsset<BalanceProfile>($"{ProjectRoot}/Data/Balance/BalanceProfile.asset");

        private static HazardProfile LoadHazards() =>
            LoadAsset<HazardProfile>($"{ProjectRoot}/Data/Hazards/HazardProfile.asset");
    }
}
