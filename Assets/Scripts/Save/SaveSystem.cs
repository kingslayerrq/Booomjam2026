using System.IO;
using UnityEngine;

public static class SaveSystem 
{
    private const string SaveFileName = "game_save.json";
    
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    
    public static void Save(GameSaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("Save failed: data is null.");
            return;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log($"Game saved to: {SavePath}");
    }
    
    public static bool TryLoad(out GameSaveData data)
    {
        data = null;

        if (!File.Exists(SavePath))
        {
            Debug.Log("No save file found.");
            return false;
        }

        string json = File.ReadAllText(SavePath);
        data = JsonUtility.FromJson<GameSaveData>(json);

        if (data == null)
        {
            Debug.LogWarning("Save file exists, but could not be read.");
            return false;
        }

        Debug.Log($"Game loaded from: {SavePath}");
        return true;
    }
    
    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }
    
    public static void DeleteSave()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("No save file to delete.");
            return;
        }

        File.Delete(SavePath);
        Debug.Log("Save file deleted.");
    }
    
    public static string GetSavePath()
    {
        return SavePath;
    }
}
