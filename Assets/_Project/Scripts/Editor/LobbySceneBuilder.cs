using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquishyBump.Editor
{
    /// <summary>
    /// SB-76: dresses Lobby.unity with the "STAR CATCHER" claw machine (Art/Lobby/Lobby.glb). The gameplay anchors keep
    /// their names and logic (Floor, Fruit stands/Stand n/Pick volume, Host desk, Info board, Controls board) and move
    /// onto the model: pedestals -> stands, prize-chute button -> host desk, back wall -> boards, boxes -> floor and walls.
    /// </summary>
    public static class LobbySceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Lobby.unity";
        private const string ModelPath = "Assets/_Project/Art/Lobby/Lobby.glb";
        private const string LayoutName = "Store aisle";
        private const string ModelName = "Claw machine";

        // The machine is ~5 m wide; x3 gives a floor of about 15 x 16 m for six fighters.
        private const float ModelScale = 3f;
        private const float PedestalHeightFactor = 0.2f; // flat pads: fighters walk over them
        private const string ChuteHousing = "wall_low_boxblue2_0";
        private const float PatchThickness = 0.5f;
        private const float WallHeight = 8f;
        private const float WallThickness = 1f;
        private const float PickHeight = 2.4f;
        private const float BoardHeight = 2.6f;
        private const float BoardInset = 1.2f;
        private const float BoardSpread = 3.5f;

        // Embedded characters, camera, light and the sky sphere of the source scene; the front glass would block the camera.
        private static readonly string[] Hidden =
        {
            "gau.obj", "heo.obj", "meo.obj", "meo.obj.001", "ga.obj", "ech.obj", "canh cut2",
            "Cube.008", "Cube.021", "Cube.031", "sky", "Camera", "Camera.001", "Light", "Sun",
            "Plane", "wall2_low_glass2.001_0", ChuteHousing,
        };
        private static readonly string[] Pedestals = { "Cylinder", "Cylinder.001", "Cylinder.002", "Cylinder.003", "Cylinder.004", "Cylinder.005" };

        [MenuItem("Squishy Bump/Build Lobby")]
        public static void Build()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            if (scene.isDirty) throw new InvalidOperationException("Save Lobby.unity before rebuilding it.");

            try
            {
                SceneManager.SetActiveScene(scene);
                var previousLayout = scene.GetRootGameObjects().FirstOrDefault(go => go.name == LayoutName);
                if (previousLayout != null) UnityEngine.Object.DestroyImmediate(previousLayout);

                var layout = new GameObject(LayoutName).transform;
                var model = PlaceModel(layout);
                var floor = FloorBounds(model);
                FloorAndWalls(layout, floor);
                Stands(layout, model);
                HostDesk(layout, model);
                Boards(layout, floor);

                var camera = scene.GetRootGameObjects().Select(go => go.GetComponent<Camera>()).First(c => c != null);
                camera.transform.SetPositionAndRotation(new Vector3(0f, 14f, -22f), Quaternion.Euler(32f, 0f, 0f));
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException($"Could not save {ScenePath}.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (openedHere && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
            }
        }

        // Instance turned 180 degrees so the front (glass, joystick) faces -Z like the old camera, floor top at y = 0, floor centred.
        private static Transform PlaceModel(Transform layout)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath)
                ?? throw new InvalidOperationException($"Missing {ModelPath}.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, layout);
            instance.name = ModelName;
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            instance.transform.localScale = Vector3.one * ModelScale;

            foreach (var c in instance.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (var l in instance.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(l);

            foreach (var name in Pedestals)
            {
                var pad = Find(instance.transform, name);
                var before = pad.GetComponent<Renderer>().bounds;
                var s = pad.localScale;
                pad.localScale = new Vector3(s.x, s.y * PedestalHeightFactor, s.z);
                pad.position += Vector3.down * (before.extents.y * (1f - PedestalHeightFactor));
            }

            var floor = FloorBounds(instance.transform);
            instance.transform.position -= new Vector3(floor.center.x, floor.max.y, floor.center.z);

            // The chute housing sits in a notch of the floor mesh; hiding it leaves a hole, so patch it with the floor material.
            var notch = Find(instance.transform, ChuteHousing).GetComponent<Renderer>().bounds;
            var patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "Floor patch";
            UnityEngine.Object.DestroyImmediate(patch.GetComponent<Collider>());
            patch.transform.SetParent(layout, false);
            patch.transform.position = new Vector3(notch.center.x, -PatchThickness / 2f, notch.center.z);
            patch.transform.localScale = new Vector3(notch.size.x, PatchThickness, notch.size.z);
            patch.GetComponent<Renderer>().sharedMaterial = Find(instance.transform, "yellowplane_low_star3_0").GetComponent<Renderer>().sharedMaterial;

            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                if (Array.IndexOf(Hidden, t.name) >= 0) t.gameObject.SetActive(false);
            return instance.transform;
        }

        private static Transform Find(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        private static Bounds FloorBounds(Transform model) =>
            Find(model, "yellowplane_low_star3_0").GetComponent<Renderer>().bounds;

        private static void FloorAndWalls(Transform layout, Bounds floor)
        {
            var colliders = new GameObject("Colliders").transform;
            colliders.SetParent(layout, false);
            Box(layout, "Floor", new Vector3(floor.center.x, -0.25f, floor.center.z), new Vector3(floor.size.x, 0.5f, floor.size.z));
            Box(colliders, "Wall back", new Vector3(floor.center.x, WallHeight / 2f, floor.max.z + WallThickness / 2f), new Vector3(floor.size.x, WallHeight, WallThickness));
            Box(colliders, "Wall front", new Vector3(floor.center.x, WallHeight / 2f, floor.min.z - WallThickness / 2f), new Vector3(floor.size.x, WallHeight, WallThickness));
            Box(colliders, "Wall left", new Vector3(floor.min.x - WallThickness / 2f, WallHeight / 2f, floor.center.z), new Vector3(WallThickness, WallHeight, floor.size.z));
            Box(colliders, "Wall right", new Vector3(floor.max.x + WallThickness / 2f, WallHeight / 2f, floor.center.z), new Vector3(WallThickness, WallHeight, floor.size.z));
        }

        // Stand n sits so the display fighter's feet touch the pad (LobbyController puts them 0.6 m + radius above the stand).
        // Order: back row left to right, then the right row back to front.
        private static void Stands(Transform layout, Transform model)
        {
            var pads = Pedestals.Select(n => Find(model, n).GetComponent<Renderer>().bounds)
                .OrderByDescending(b => b.center.z > 0f ? 1 : 0).ThenBy(b => b.center.z > 0f ? b.center.x : -b.center.z).ToArray();
            var stands = new GameObject("Fruit stands").transform;
            stands.SetParent(layout, false);
            for (var i = 0; i < pads.Length; i++)
            {
                var pad = pads[i];
                var stand = new GameObject($"Stand {i + 1}").transform;
                stand.SetParent(stands, false);
                stand.position = new Vector3(pad.center.x, pad.max.y - 0.6f, pad.center.z);
                var pick = new GameObject("Pick volume").AddComponent<BoxCollider>();
                pick.transform.SetParent(stand, false);
                pick.transform.position = new Vector3(pad.center.x, PickHeight / 2f, pad.center.z);
                pick.size = new Vector3(pad.size.x, PickHeight, pad.size.z);
                pick.isTrigger = true;
            }
        }

        // The big prize-chute button is the host desk: LobbyController measures DeskReach from this point.
        private static void HostDesk(Transform layout, Transform model)
        {
            var button = Find(model, "BoomButton_01_button_plastic_mat_01_0").GetComponent<Renderer>().bounds;
            new GameObject("Host desk").transform.SetParent(layout, false);
            layout.Find("Host desk").position = new Vector3(button.center.x, Mathf.Max(button.center.y - 1f, 0.6f), button.center.z);
        }

        // Back wall, side by side: room info on the left, controls on the right. WorldText faces -Z.
        private static void Boards(Transform layout, Bounds floor)
        {
            var z = floor.max.z - BoardInset;
            Marker(layout, "Info board", new Vector3(floor.center.x - BoardSpread, BoardHeight, z));
            Marker(layout, "Controls board", new Vector3(floor.center.x + BoardSpread, BoardHeight, z));
        }

        private static void Marker(Transform parent, string name, Vector3 position)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.position = position;
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var box = new GameObject(name).AddComponent<BoxCollider>();
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.size = size;
        }
    }
}
