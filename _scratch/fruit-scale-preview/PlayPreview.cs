using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SquishyBump.App;
using SquishyBump.Hazards;
using SquishyBump.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class FruitScalePlayPreview
{
    private const float L2Scale = .80f;
    private const float L3Scale = .60f;
    private const float L4Scale = .90f;
    private const float L5Scale = .85f;
    private static bool loading;
    private static bool startHeld;
    private static bool nextHeld;
    private static bool waitingToPlay;

    public static Task<string> PlayL2() => PlayLevel(2);

    public static Task<string> PlayL3() => PlayLevel(3);

    public static Task<string> PlayL4() => PlayLevel(4);

    public static Task<string> PlayL5() => PlayLevel(5);

    private static async Task<string> PlayLevel(int level)
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Enter Play Mode from Boot first.");

        var scene = SceneManager.GetActiveScene();
        var factor = ScaleFor(level);
        var ready = scene.name == $"L{level}" && Time.timeScale == 0f &&
            Object.FindObjectsByType<RollVisual>(FindObjectsSortMode.None)
                .Where(visual => visual.gameObject.scene == scene)
                .Select(visual => visual.transform.Find("Fruit scale preview"))
                .Count(pivot => pivot && Mathf.Approximately(pivot.localScale.x, factor)) == 6;
        if (!ready) await Prepare(level);
        Time.timeScale = 1f;
        waitingToPlay = false;
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        return $"Playing L{level} at {factor:F2}x.";
    }

    public static async Task<string> Run()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "MainMenu")
            throw new InvalidOperationException("Start from Boot and wait for MainMenu.");

        await Prepare(2);
        await Prepare(3);
        await Prepare(2);
        EditorApplication.update += CheckKeys;
        EditorApplication.playModeStateChanged += Stop;
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        return $"L2 {L2Scale:F2}x -> L3 {L3Scale:F2}x verified. L2 ready.";
    }

    private static async Task Prepare(int level)
    {
        loading = true;
        try
        {
            Time.timeScale = 1f;
            var launcher = Object.FindFirstObjectByType<LevelLauncher>();
            launcher.StartSinglePlayer(level);
            var timeout = Time.realtimeSinceStartup + 15f;
            var sceneName = $"L{level}";
            while (EditorApplication.isPlaying && Time.realtimeSinceStartup < timeout)
            {
                await Awaitable.NextFrameAsync();
                var scene = SceneManager.GetActiveScene();
                if (scene.name != sceneName) continue;
                var visuals = Object.FindObjectsByType<RollVisual>(FindObjectsSortMode.None)
                    .Where(visual => visual.gameObject.scene == scene).ToArray();
                if (visuals.Length != 6) continue;

                if (level == 2) AddMovingGroundArt();
                if (level == 3) AddCannonCourtArt();
                if (level == 4) AddFrostRingArt();
                if (level == 5) AddChaosCoreArt();
                var factor = ScaleFor(level);
                foreach (var visual in visuals) ApplyVisualScale(visual, factor);
                Time.timeScale = 0f;
                waitingToPlay = true;
                Debug.Log($"[Fruit scale preview] {sceneName} {factor:F2}x. Six visuals scaled. F6 play; F8 L3.");
                return;
            }
            throw new InvalidOperationException($"{sceneName} did not initialize all fruit.");
        }
        finally
        {
            loading = false;
        }
    }

    private static float ScaleFor(int level) => level switch
    {
        2 => L2Scale,
        3 => L3Scale,
        4 => L4Scale,
        5 => L5Scale,
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    private static void ApplyVisualScale(RollVisual visual, float factor)
    {
        var body = visual.transform.Find("Body");
        if (!body) throw new InvalidOperationException($"No Body on {visual.name}.");
        var renderers = body.GetComponentsInChildren<Renderer>().Where(renderer => renderer.enabled).ToArray();
        if (renderers.Length == 0) throw new InvalidOperationException($"No rendered fruit on {visual.name}.");
        var baseBottom = renderers.Min(renderer => renderer.bounds.min.y);
        var pivot = new GameObject("Fruit scale preview").transform;
        pivot.SetParent(visual.transform, false);
        body.SetParent(pivot, false);
        pivot.localScale = Vector3.one * factor;
        var newBottom = renderers.Min(renderer => renderer.bounds.min.y);
        pivot.position += Vector3.up * (baseBottom - newBottom);
        if (Mathf.Abs(renderers.Min(renderer => renderer.bounds.min.y) - baseBottom) > .001f)
            throw new InvalidOperationException($"Preview moved the bottom on {visual.name}.");
    }

    private static void AddCannonCourtArt()
    {
        var floor = GameObject.Find("Floor r 9 m").GetComponent<Renderer>();
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Scratch/CreativeMap3/Map3.glb");
        if (!asset) throw new InvalidOperationException("Creative Map 3 GLB is missing.");
        var map = Object.Instantiate(asset);
        foreach (var camera in map.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        foreach (var light in map.GetComponentsInChildren<Light>(true)) light.enabled = false;
        foreach (var collider in map.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        var renderers = map.GetComponentsInChildren<Renderer>(true);
        var disc = renderers.Single(renderer => renderer.name == "Cylinder.002");
        var target = floor.bounds;
        var raw = disc.bounds;
        map.transform.localScale *= target.extents.x / Mathf.Max(raw.extents.x, raw.extents.z);
        var fitted = disc.bounds;
        map.transform.position += new Vector3(target.center.x - fitted.center.x,
            target.max.y - fitted.max.y, target.center.z - fitted.center.z);
        floor.enabled = false;
        renderers.Single(renderer => renderer.name == "Sphere").enabled = false;
    }

    private static void AddMovingGroundArt()
    {
        var moving = Object.FindFirstObjectByType<MovingGround>();
        var turntable = moving.transform.Find("Turntable").GetComponent<Renderer>();
        var ring = moving.transform.Find("Obstacle Ring").GetComponent<Renderer>();
        var map = SpawnArt("Assets/_Scratch/CreativeL2L5/L2.glb", "Creative L2 toy box");
        var renderers = map.GetComponentsInChildren<Renderer>();
        var copper = renderers.Single(renderer => renderer.name == "Cylinder");
        var center = renderers.Single(renderer => renderer.name == "Cylinder.001");
        FitArt(map, copper.bounds, ring.bounds, false);
        float centerScale = turntable.bounds.size.x / center.bounds.size.x;
        center.transform.localScale *= centerScale;
        AlignTop(center, turntable.bounds, .002f);
        center.transform.SetParent(turntable.transform, true);
        copper.transform.SetParent(ring.transform, true);
        turntable.enabled = false;
        ring.enabled = false;
    }

    private static void AddFrostRingArt()
    {
        const string original = "Assets/_Scratch/CreativeMap4";
        const string revised = "Assets/_Scratch/CreativeMap4Revised";
        var tiles = IceRenderers();
        var sourceNames = LoadArt($"{original}/map 4.glb").GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.sharedMaterials.Any(material => material && material.name == "Ice Floor"))
            .Select(renderer => renderer.name).ToHashSet();
        var map = SpawnArt($"{revised}/map 4.glb", "Creative L4 Frost Ring");
        var renderers = map.GetComponentsInChildren<Renderer>();
        var duplicateTiles = renderers.Where(renderer => sourceNames.Contains(renderer.name)).ToArray();
        if (duplicateTiles.Length != 37) throw new InvalidOperationException("L4 art tile layout changed.");
        FitArt(map, BoundsOf(duplicateTiles), BoundsOf(tiles), false);
        var palette = duplicateTiles.Select(tile =>
            (center: tile.bounds.center, color: ReadArtColor(tile.sharedMaterial))).ToArray();
        if (palette.Select(entry => entry.color).Distinct().Count() != 2)
            throw new InvalidOperationException("L4 expected two authored ice colors.");
        foreach (var renderer in duplicateTiles) renderer.enabled = false;
        foreach (var renderer in renderers.Where(r => r.sharedMaterials.All(m => m && m.name.StartsWith("COLLIDER", StringComparison.Ordinal))))
            renderer.enabled = false;
        renderers.Single(renderer => renderer.name == "Cylinder.037").enabled = false;

        var source = LoadArt($"{revised}/cuc bang 2.glb").GetComponentInChildren<MeshFilter>();
        var size = tiles[0].GetComponent<MeshFilter>().sharedMesh.bounds.size;
        var tileMesh = BakeTile(source, Vector3.Scale(size, new Vector3(.975f, 1f, .975f)));
        var material = new Material(tiles[0].sharedMaterial) { name = "L4 Creative ice preview" };
        var skins = new List<(Renderer carrier, Renderer art, Color color)>();
        foreach (var tile in tiles)
        {
            var variant = new GameObject("Creative ice tile");
            variant.transform.SetParent(tile.transform, false);
            variant.AddComponent<MeshFilter>().sharedMesh = tileMesh;
            var artwork = variant.AddComponent<MeshRenderer>();
            artwork.sharedMaterial = material;
            var closest = palette.OrderBy(entry => XzDistance(entry.center, tile.bounds.center)).First();
            skins.Add((tile, artwork, closest.color));
        }
        BindTileArt(skins);
    }

    private static void AddChaosCoreArt()
    {
        var tiles = IceRenderers();
        var map = SpawnArt("Assets/_Scratch/CreativeL2L5/L5.glb", "Creative L5 LEGO castle");
        var renderers = map.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var board = renderers.Where(renderer => renderer.name.StartsWith("Brick2x", StringComparison.Ordinal)).ToArray();
        if (board.Length != 84) throw new InvalidOperationException("L5 LEGO board layout changed.");
        FitArt(map, BoundsOf(board), BoundsOf(tiles), true);

        var material = new Material(tiles[0].sharedMaterial) { name = "L5 Creative LEGO preview" };
        var skins = new List<(Renderer carrier, Renderer art, Color color)>();
        foreach (var brick in board)
        {
            var tile = tiles.OrderBy(candidate => XzDistance(candidate.bounds.center, brick.bounds.center)).First();
            var color = ReadArtColor(brick.sharedMaterial);
            brick.sharedMaterial = material;
            brick.transform.position += Vector3.up * (tile.bounds.max.y - brick.bounds.max.y);
            brick.transform.SetParent(tile.transform, true);
            skins.Add((tile, brick, color));
        }
        BindTileArt(skins, tiles);
    }

    private static float XzDistance(Vector3 a, Vector3 b) =>
        new Vector2(a.x - b.x, a.z - b.z).sqrMagnitude;

    private static void AlignTop(Renderer art, Bounds target, float lift)
    {
        var source = art.bounds;
        art.transform.position += new Vector3(target.center.x - source.center.x,
            target.max.y - source.max.y + lift, target.center.z - source.center.z);
    }

    private static Color ReadArtColor(Material material)
    {
        if (material.HasProperty("baseColorFactor")) return material.GetColor("baseColorFactor");
        if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
        throw new InvalidOperationException($"Cannot read authored color from {material.name}.");
    }

    private static void BindTileArt(List<(Renderer carrier, Renderer art, Color color)> skins,
        Renderer[] carriers = null)
    {
        foreach (var renderer in carriers ?? skins.Select(skin => skin.carrier).Distinct().ToArray())
            renderer.forceRenderingOff = true;
        var scene = SceneManager.GetActiveScene();
        var data = new MaterialPropertyBlock();
        var paint = new MaterialPropertyBlock();
        int colorId = Shader.PropertyToID("_BaseColor");
        void Sync(ScriptableRenderContext context, Camera camera)
        {
            foreach (var (carrier, art, color) in skins)
            {
                if (!carrier || !art) continue;
                art.enabled = carrier.enabled;
                carrier.GetPropertyBlock(data);
                var state = data.GetColor(colorId);
                bool redFlash = state.r > state.b;
                var applied = redFlash ? state : color;
                applied.a = state.a;
                paint.SetColor(colorId, applied);
                art.SetPropertyBlock(paint);
            }
        }
        void Unbind(Scene unloaded)
        {
            if (unloaded != scene) return;
            RenderPipelineManager.beginCameraRendering -= Sync;
            SceneManager.sceneUnloaded -= Unbind;
        }
        RenderPipelineManager.beginCameraRendering += Sync;
        SceneManager.sceneUnloaded += Unbind;
    }

    private static Renderer[] IceRenderers()
    {
        var ice = Object.FindFirstObjectByType<IceTiles>();
        if (!ice || ice.TileCount != 61) throw new InvalidOperationException("Expected 61 H06 tiles.");
        return Enumerable.Range(0, ice.TileCount)
            .Select(index => ice.TileCollider(index).transform.Find("Visual").GetComponent<Renderer>()).ToArray();
    }

    private static GameObject LoadArt(string path) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new InvalidOperationException($"Missing asset: {path}");

    private static GameObject SpawnArt(string path, string name)
    {
        var map = Object.Instantiate(LoadArt(path));
        map.name = name;
        foreach (var camera in map.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        foreach (var light in map.GetComponentsInChildren<Light>(true)) light.enabled = false;
        foreach (var collider in map.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        return map;
    }

    private static Bounds BoundsOf(Renderer[] renderers)
    {
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static void FitArt(GameObject map, Bounds source, Bounds target, bool coverDepth)
    {
        var scale = target.size.x / source.size.x;
        if (coverDepth) scale = Mathf.Max(scale, target.size.z / source.size.z);
        map.transform.localScale *= scale;
        var sourceTop = new Vector3(source.center.x, source.max.y, source.center.z);
        var targetTop = new Vector3(target.center.x, target.max.y, target.center.z);
        map.transform.position += targetTop - sourceTop * scale;
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

    private static async void CheckKeys()
    {
        var keyboard = Keyboard.current;
        if (!EditorApplication.isPlaying || keyboard == null) return;
        var start = keyboard.f6Key.isPressed;
        var next = keyboard.f8Key.isPressed;
        var beginPlay = start && !startHeld;
        var goToL3 = next && !nextHeld;
        startHeld = start;
        nextHeld = next;
        if (loading) return;
        if (beginPlay && waitingToPlay)
        {
            Time.timeScale = 1f;
            waitingToPlay = false;
        }
        if (goToL3 && SceneManager.GetActiveScene().name == "L2")
        {
            try
            {
                await Prepare(3);
            }
            catch (Exception exception)
            {
                Time.timeScale = 1f;
                Debug.LogException(exception);
            }
        }
    }

    private static void Stop(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode) return;
        EditorApplication.update -= CheckKeys;
        EditorApplication.playModeStateChanged -= Stop;
        Time.timeScale = 1f;
    }
}
