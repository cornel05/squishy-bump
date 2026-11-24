using SquishyBump.App;
using SquishyBump.Core;
using UnityEditor;

namespace SquishyBump.Editor
{
    public static class SinglePlayerSetup
    {
        [MenuItem("Squishy Bump/Configure Single Player Profiles")]
        public static void Configure()
        {
            var setup = AssetDatabase.LoadAssetAtPath<MatchSetup>("Assets/_Project/Data/MatchSetup.asset");
            var data = new SerializedObject(setup);
            var profiles = data.FindProperty("botProfiles");
            profiles.arraySize = 5;
            for (var i = 0; i < 5; i++)
                profiles.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<BotProfile>($"Assets/_Project/Data/Bots/D{i + 1}.asset");
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(setup);
            AssetDatabase.SaveAssets();
        }
    }
}
