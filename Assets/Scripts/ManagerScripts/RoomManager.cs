using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [System.Serializable]
    public class RoomEntry
    {
        public string roomName;
        public GameObject roomObject;
    }
    public static RoomManager Instance;
    
    // Visualize in inspector
    [SerializeField] private List<RoomEntry> roomEntries = new List<RoomEntry>();
    
    // Actual Data
    private Dictionary<string, GameObject> _roomNameToObject = new Dictionary<string, GameObject>();
    
    public IReadOnlyDictionary<string, GameObject> RoomNameToObject => _roomNameToObject;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            foreach (var entry in roomEntries)
            {
                if (!string.IsNullOrEmpty(entry.roomName) && entry.roomObject != null)
                {
                    // Check to prevent duplicate key crashes
                    if (!_roomNameToObject.ContainsKey(entry.roomName))
                    {
                        _roomNameToObject.Add(entry.roomName, entry.roomObject);
                    }
                    else
                    {
                        Debug.LogWarning($"[RoomManager] Duplicate room name found: {entry.roomName}");
                    }
                }
            }
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    private void OnValidate()
    {
        if (roomEntries == null) return;

        foreach (var entry in roomEntries)
        {
            // Automatically sync the name if the object is assigned
            if (entry.roomObject != null && entry.roomName != entry.roomObject.name)
            {
                entry.roomName = entry.roomObject.name;
            }
        }
    }

    public GameObject GetRoomByName(string roomName)
    {
        if (Instance != null && Instance._roomNameToObject.TryGetValue(roomName, out GameObject room))
        {
            return room;
        }

        Debug.LogWarning($"Room '{roomName}' not found or RoomManager not initialized.");
        return null;
    }
    
#if UNITY_EDITOR
    // A helper function for the Custom Editor dropdown
    public List<string> GetRoomNamesForEditor()
    {
        List<string> names = new List<string>();
        foreach(var entry in roomEntries)
        {
            if(!string.IsNullOrEmpty(entry.roomName))
            {
                names.Add(entry.roomName);
            }
        }
        return names;
    }
#endif
}