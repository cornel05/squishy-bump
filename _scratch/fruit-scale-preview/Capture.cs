using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SquishyBump.Hazards;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FruitScaleCapture
{
    private const string Output = "_scratch/fruit-scale-preview";
    private const int Width = 1600;
    private const int Height = 900;
    private static readonly string[] Fruits =
        { "Watermelon", "Pineapple", "Apple", "Mangosteen", "Peach", "Blueberry" };
    private static readonly int[] SpawnSlots = { 4, 1, 3, 2, 5, 6 };
    private static readonly float[] Proposals = { .95f, .90f, .85f, .90f, .85f };
    private static readonly float[] FrameSizes = { 6.8f, 6.1f, 6.4f, 6.1f, 6.1f };
    private static readonly Vector2[] CannonCourtAnchors =
    {
        new(-1.3f, -1.1f), new(.65f, 1.4f), new(4.15f, -1.7f),
        new(3.7f, 2.3f), new(-4.15f, -1.7f), new(-3.7f, 2.3f)
    };

    public static string Run() => Capture(new[] { 1, 2, 3, 4, 5 }, false);

    public static string RunAlternatives() => Capture(new[] { 2, 3 }, true);

    private static string Capture(int[] levels, bool alternatives)
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Capture requires a clean Edit Mode scene.");
        var original = EditorSceneManager.GetSceneManagerSetup();
        var audit = new List<string>();
        Directory.CreateDirectory(Output);
        try
        {
            foreach (int level in levels)
                CaptureLevel(level, alternatives, audit);
            return $"Captured {levels.Length * (alternatives ? 1 : 2)} Unity renders. {audit.Count(line => line.Contains("contact="))} fruit placement checks.";
        }
        finally
        {
            File.WriteAllLines(Path.Combine(Output, alternatives ? "alternative-checks.txt" : "capture-checks.txt"), audit);
            EditorSceneManager.RestoreSceneManagerSetup(original);
        }
    }

    private static void CaptureLevel(int level, bool alternatives, List<string> audit)
    {
        var path = level == 1 ? "Assets/_Scratch/CreativeMap1/Map1Preview.unity"
            : $"Assets/_Project/Scenes/Levels/L{level}.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var existing in objects.SelectMany(t => t.GetComponents<Camera>())) existing.enabled = false;
        foreach (var transform in objects.Where(t => t.name.StartsWith("Preview qu", StringComparison.Ordinal)))
            transform.gameObject.SetActive(false);

        BuildHazards(level);
        var artFloor = level == 3 ? AddCannonCourtArt() : null;
        if (level == 4) AddFrostRingArt();
        UnityEngine.Physics.SyncTransforms();

        var camera = new GameObject("Fruit scale capture camera").AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = FrameSizes[level - 1];
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 175f;
        var eye = level == 2 ? new Vector3(3f, 12f, -17f) : new Vector3(3.8f, 12f, -18f);
        camera.transform.position = eye;
        camera.transform.LookAt(new Vector3(0f, .1f, .35f));
        audit.Add($"L{level} source={path} eye={eye:F3} rotation={camera.transform.rotation:F3} ortho={camera.orthographicSize:F2}");

        var previews = Fruits.Select((name, index) =>
        {
            var spawn = PreviewAnchor(level, index, objects);
            var ground = level == 3 ? SampleArtFloor(artFloor, spawn) : FindGround(level, spawn);
            var prefab = Load($"Assets/_Project/Prefabs/Fighters/{name}.prefab");
            var visualAsset = prefab.transform.Find("Visual");
            var pivot = new GameObject($"Preview fruit: {name}").transform;
            pivot.position = new Vector3(spawn.x, ground, spawn.z);
            pivot.rotation = Quaternion.LookRotation(new Vector3(eye.x - spawn.x, 0f, eye.z - spawn.z));
            var visual = Object.Instantiate(visualAsset.gameObject, pivot, false).transform;
            var meshes = visual.GetComponentsInChildren<MeshFilter>()
                .Where(mesh => mesh.GetComponent<MeshRenderer>().enabled).ToArray();
            if (meshes.Length == 0 || meshes.Any(mesh => !mesh.sharedMesh))
                throw new InvalidOperationException($"Missing visual mesh on {name}.");
            return (name, pivot, visual, meshes, scale: visual.localScale);
        }).ToArray();

        var variants = alternatives
            ? new[] { (factor: level == 2 ? .80f : .70f, suffix: "alternative") }
            : new[] { (factor: 1f, suffix: "baseline"), (factor: Proposals[level - 1], suffix: "proposed") };
        foreach (var (factor, suffix) in variants)
        {
            foreach (var fruit in previews)
            {
                fruit.visual.localPosition = Vector3.zero;
                fruit.visual.localScale = factor * fruit.scale;
                fruit.visual.position += Vector3.up * (fruit.pivot.position.y - Bottom(fruit.meshes));
                float contact = Bottom(fruit.meshes) - fruit.pivot.position.y;
                if (Mathf.Abs(contact) > .001f)
                    throw new InvalidOperationException($"L{level} {fruit.name} failed floor contact.");
                audit.Add($"L{level} {factor:F2} {fruit.name} anchor={fruit.pivot.position:F4} scale={fruit.visual.localScale:F4} contact={contact:F6}");
            }
            Shot(camera, Path.Combine(Output, $"L{level}-{suffix}.png"));
        }
    }

    private static void BuildHazards(int level)
    {
        if (level == 2)
        {
            Initialize(Object.FindFirstObjectByType<MovingGround>());
            Initialize(Object.FindFirstObjectByType<PusherBlocks>());
        }
        if (level == 3 || level == 5) Initialize(Object.FindFirstObjectByType<Cannons>());
        if (level >= 4) Initialize(Object.FindFirstObjectByType<IceTiles>());
    }

    private static void Initialize(MonoBehaviour hazard)
    {
        if (!hazard) throw new InvalidOperationException("Missing scene hazard.");
        // Build the actual initial scene geometry without advancing gameplay.
        hazard.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hazard, null);
    }

    private static float FindGround(int level, Vector3 spawn)
    {
        var hits = UnityEngine.Physics.RaycastAll(spawn + Vector3.up * 3f, Vector3.down, 6f)
            .Where(hit => hit.normal.y > .85f && Mathf.Abs(hit.point.y - spawn.y) < .5f)
            .OrderByDescending(hit => hit.point.y);
        foreach (var hit in hits) return hit.point.y;
        throw new InvalidOperationException($"L{level} no ground below {spawn:F3}.");
    }

    private static Vector3 PreviewAnchor(int level, int index, Transform[] objects)
    {
        var spawn = objects.Single(t => t.name == $"Spawn {SpawnSlots[index]}").position;
        if (level == 2 && (index == 2 || index == 4))
            return new Vector3(Mathf.Sign(spawn.x) * 3.1f, spawn.y, -1.7f);
        if (level == 2 && index == 1)
            return new Vector3(-1.1f, spawn.y, 4.65f);
        if (level == 3)
        {
            var point = CannonCourtAnchors[index];
            return new Vector3(point.x, 0f, point.y);
        }
        if (level == 4)
        {
            var ice = Object.FindFirstObjectByType<IceTiles>();
            return Enumerable.Range(0, ice.TileCount).Select(i => ice.TileCollider(i).transform.position)
                .OrderBy(center => (center - spawn).sqrMagnitude).First();
        }
        return spawn;
    }

    private static float SampleArtFloor(MeshFilter[] floor, Vector3 point)
    {
        float highest = float.NegativeInfinity;
        foreach (var filter in floor)
        {
            var mesh = filter.sharedMesh;
            var vertices = mesh.vertices.Select(filter.transform.TransformPoint).ToArray();
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var ab = vertices[triangles[i + 1]] - a;
                var ac = vertices[triangles[i + 2]] - a;
                float up = Vector3.Cross(ab, ac).y;
                if (Mathf.Abs(up) <= .00001f) continue;
                var p = point - a;
                float u = Vector3.Cross(p, ac).y / up;
                float v = Vector3.Cross(ab, p).y / up;
                if (u >= -.0001f && v >= -.0001f && u + v <= 1.0001f)
                    highest = Mathf.Max(highest, a.y + ab.y * u + ac.y * v);
            }
        }
        if (float.IsNegativeInfinity(highest))
            throw new InvalidOperationException($"L3 art has no ground below {point:F3}.");
        return highest;
    }

    private static float Bottom(IEnumerable<MeshFilter> filters)
    {
        float low = float.PositiveInfinity;
        foreach (var filter in filters)
        {
            var matrix = filter.transform.localToWorldMatrix;
            foreach (var vertex in filter.sharedMesh.vertices)
                low = Mathf.Min(low, matrix.MultiplyPoint3x4(vertex).y);
        }
        return low;
    }

    private static MeshFilter[] AddCannonCourtArt()
    {
        var floor = GameObject.Find("Floor r 9 m").GetComponent<Renderer>();
        var map = Object.Instantiate(Load("Assets/_Scratch/CreativeMap3/Map3.glb"));
        foreach (var camera in map.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        foreach (var light in map.GetComponentsInChildren<Light>(true)) light.enabled = false;
        var renderers = map.GetComponentsInChildren<Renderer>(true);
        var disc = renderers.Single(renderer => renderer.name == "Cylinder.002");
        var raw = disc.bounds;
        var target = floor.bounds;
        map.transform.localScale *= target.extents.x / Mathf.Max(raw.extents.x, raw.extents.z);
        var fitted = disc.bounds;
        map.transform.position += new Vector3(target.center.x - fitted.center.x,
            target.max.y - fitted.max.y, target.center.z - fitted.center.z);
        floor.enabled = false;
        renderers.Single(renderer => renderer.name == "Sphere").enabled = false;
        return renderers.Where(r => r.name == "Cylinder.001" || r.name == "Cylinder.002" || r.name == "Cylinder.004")
            .Select(r => r.GetComponent<MeshFilter>()).ToArray();
    }

    private static void AddFrostRingArt()
    {
        const string originals = "Assets/_Scratch/CreativeMap4";
        const string revised = "Assets/_Scratch/CreativeMap4Revised";
        var ice = Object.FindFirstObjectByType<IceTiles>();
        var tiles = Enumerable.Range(0, ice.TileCount)
            .Select(i => ice.TileCollider(i).transform.Find("Visual").GetComponent<MeshRenderer>()).ToArray();
        var originalTileNames = Load($"{originals}/map 4.glb").GetComponentsInChildren<Renderer>(true)
            .Where(r => r.sharedMaterials.Any(m => m && m.name == "Ice Floor"))
            .Select(r => r.name).ToHashSet();
        var map = Object.Instantiate(Load($"{revised}/map 4.glb"));
        var renderers = map.GetComponentsInChildren<Renderer>(true);
        var staticTiles = renderers.Where(renderer => originalTileNames.Contains(renderer.name)).ToArray();
        if (staticTiles.Length != 37 || tiles.Length != 61)
            throw new InvalidOperationException("L4 expected 37 art tiles and 61 arena tiles.");
        var floor = BoundsOf(staticTiles);
        var scale = BoundsOf(tiles).size.x / floor.size.x;
        map.transform.localScale *= scale;
        map.transform.position += new Vector3(-floor.center.x, -floor.max.y, -floor.center.z) * scale;
        foreach (var renderer in staticTiles) renderer.enabled = false;
        foreach (var renderer in renderers.Where(r => r.sharedMaterials.All(m => m && m.name.StartsWith("COLLIDER", StringComparison.Ordinal))))
            renderer.enabled = false;
        renderers.Single(renderer => renderer.name == "Cylinder.037").enabled = false;

        var template = tiles[0].GetComponent<MeshFilter>().sharedMesh.bounds;
        var source = Load($"{revised}/cuc bang 2.glb").GetComponentsInChildren<MeshFilter>(true).Single();
        // A narrow seam keeps the boundaries of adjacent art tiles visible in the overview.
        var visibleSize = Vector3.Scale(template.size, new Vector3(.975f, 1f, .975f));
        var tileMesh = BakeTile(source, visibleSize);
        foreach (var tile in tiles)
            tile.GetComponent<MeshFilter>().sharedMesh = tileMesh;
    }

    private static Mesh BakeTile(MeshFilter source, Vector3 targetSize)
    {
        var mesh = Object.Instantiate(source.sharedMesh);
        var matrix = source.transform.localToWorldMatrix;
        var vertices = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
        var bounds = new Bounds(vertices[0], Vector3.zero);
        foreach (var vertex in vertices) bounds.Encapsulate(vertex);
        var anchor = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        var scale = new Vector3(targetSize.x / bounds.size.x, targetSize.y / bounds.size.y, targetSize.z / bounds.size.z);
        mesh.vertices = vertices.Select(vertex => Vector3.Scale(vertex - anchor, scale)).ToArray();
        var normals = matrix.inverse.transpose;
        mesh.normals = mesh.normals.Select(normal =>
        {
            var world = normals.MultiplyVector(normal);
            return new Vector3(world.x / scale.x, world.y / scale.y, world.z / scale.z).normalized;
        }).ToArray();
        if (matrix.determinant < 0)
        {
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
                (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
            mesh.triangles = triangles;
        }
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    private static Bounds BoundsOf(Renderer[] renderers)
    {
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static GameObject Load(string path) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new FileNotFoundException(path);

    private static void Shot(Camera camera, string path)
    {
        var saved = RenderTexture.active;
        var target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = saved;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(image);
        }
    }
}
