using SquishyBump.Core;
using UnityEditor;

namespace SquishyBump.Editor
{
    public static class BotDifficultySetup
    {
        [MenuItem("Squishy Bump/Configure Bot Weight Classes")]
        public static void ConfigureWeightClasses()
        {
            foreach (var category in new[] { FruitWeightClass.Normal, FruitWeightClass.Heavy, FruitWeightClass.Light })
            {
                var profile = AssetDatabase.LoadAssetAtPath<ClassProfile>(
                    $"Assets/_Project/Data/Classes/{category}.asset");
                var serialized = new SerializedObject(profile);
                serialized.FindProperty("category").enumValueIndex = (int)category;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
