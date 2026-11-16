using System;
using System.IO;
using SquishyBump.Core;
using UnityEngine;

namespace SquishyBump.App
{
    /// <summary>
    /// JSON save in the user data folder (GDD 10.5). Writes go to a temp file first and then replace the save,
    /// so a crash mid-write never leaves a half file. A file that cannot be read is kept as <c>.bad</c> and
    /// the game starts from defaults instead of failing.
    /// </summary>
    public sealed class LocalSave
    {
        public const string FileName = "save.json";
        private readonly string path;

        public LocalSave(string folder) => path = Path.Combine(folder, FileName);

        public string FilePath => path;

        public SaveData Load()
        {
            if (!File.Exists(path)) return new SaveData();
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.Settings == null) throw new FormatException("empty save");
                return data;
            }
            catch (Exception e) when (e is ArgumentException || e is FormatException || e is IOException)
            {
                Debug.LogWarning($"[LocalSave] unreadable {path}: {e.Message}; starting from defaults");
                File.Copy(path, path + ".bad", true);
                return new SaveData();
            }
        }

        public void Save(SaveData data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
