using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquishyBump.Editor
{
    public static class LobbySceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Lobby.unity";
        private const string LayoutName = "Store aisle";

        [MenuItem("Squishy Bump/Build Lobby Greybox")]
        public static void Build()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            if (scene.isDirty) throw new InvalidOperationException("Save Lobby.unity before rebuilding its greybox.");

            try
            {
                SceneManager.SetActiveScene(scene);
                var previousLayout = scene.GetRootGameObjects().FirstOrDefault(go => go.name == LayoutName);
                if (previousLayout != null) UnityEngine.Object.DestroyImmediate(previousLayout);

                var layout = new GameObject(LayoutName).transform;
                Box(layout, "Floor", new Vector3(0f, -0.2f, 0f), new Vector3(14f, 0.4f, 18f));
                Box(layout, "Left shelf", new Vector3(-6f, 1f, 0f), new Vector3(1.5f, 2f, 11f));
                Box(layout, "Right shelf", new Vector3(6f, 1f, 0f), new Vector3(1.5f, 2f, 11f));
                Box(layout, "Host desk", new Vector3(0f, 0.65f, 6.5f), new Vector3(4f, 1.3f, 1.2f));
                Box(layout, "Info board", new Vector3(-2.8f, 1.7f, 7.2f), new Vector3(2f, 2f, 0.25f));
                Box(layout, "Controls board", new Vector3(2.8f, 1.7f, 7.2f), new Vector3(2f, 2f, 0.25f));

                var stands = new GameObject("Fruit stands").transform;
                stands.SetParent(layout, false);
                for (int i = 0; i < 6; i++)
                {
                    int side = i < 3 ? -1 : 1;
                    float z = (i % 3 - 1) * 3.3f;
                    var stand = Box(stands, $"Stand {i + 1}", new Vector3(side * 4.4f, 0.45f, z),
                        new Vector3(1.2f, 0.9f, 1.5f));
                    var pick = new GameObject("Pick volume").AddComponent<BoxCollider>();
                    pick.transform.SetParent(stand.transform, false);
                    pick.transform.localPosition = new Vector3(-side, 0.3f, 0f);
                    pick.size = new Vector3(0.9f, 2f, 1f);
                    pick.isTrigger = true;
                }

                var camera = scene.GetRootGameObjects().Select(go => go.GetComponent<Camera>()).First(c => c != null);
                camera.transform.SetPositionAndRotation(new Vector3(0f, 15f, -13f), Quaternion.Euler(47f, 0f, 0f));
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException($"Could not save {ScenePath}.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (openedHere && !scene.isDirty) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            return box;
        }
    }
}
