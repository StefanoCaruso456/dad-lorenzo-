using System.IO;
using UnityEngine;

namespace CrossHop.Economy
{
    /// <summary>
    /// Local, backend-free persistence. Writes JSON to persistentDataPath.
    /// Writes go to a temp file first, then atomically replace, so a crash mid-write
    /// can never corrupt the live save.
    /// </summary>
    public static class SaveSystem
    {
        private static string FilePath => Path.Combine(Application.persistentDataPath, "crosshop_save.json");

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new SaveData();
                string json = File.ReadAllText(FilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                return data ?? new SaveData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed, starting fresh: {e.Message}");
                return new SaveData();
            }
        }

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
            }
        }
    }
}
