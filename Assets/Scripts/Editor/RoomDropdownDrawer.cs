using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RoomDropdownAttribute))]
public class RoomDropdownDrawer : PropertyDrawer
{
    // Add a flag to prevent console spam!
    private bool _hasLoggedWarning = false;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
            return EditorGUIUtility.singleLineHeight * 2.5f; 

        RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>();
        
        if (roomManager == null || roomManager.GetRoomNamesForEditor().Count == 0)
            return (EditorGUIUtility.singleLineHeight * 2.2f) + EditorGUIUtility.singleLineHeight;

        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.HelpBox(position, "[RoomDropdown] can only be used on string variables!", MessageType.Error);
            return;
        }

        RoomManager roomManager = Object.FindFirstObjectByType<RoomManager>();

        if (roomManager != null)
        {
            List<string> roomNames = roomManager.GetRoomNamesForEditor();

            if (roomNames.Count > 0)
            {
                // Reset the flag if the user fixed the issue!
                _hasLoggedWarning = false;

                int currentIndex = Mathf.Max(0, roomNames.IndexOf(property.stringValue));
                currentIndex = EditorGUI.Popup(position, label.text, currentIndex, roomNames.ToArray());
                property.stringValue = roomNames[currentIndex];
            }
            else
            {
                LogWarningOnce($"[RoomDropdown] RoomManager on '{roomManager.gameObject.name}' has no rooms configured!");
                DrawWarningAndFallback(position, property, label, "RoomManager has no rooms configured! Add rooms to the manager.");
            }
        }
        else
        {
            LogWarningOnce("[RoomDropdown] No RoomManager found in the current scene. Cannot populate dropdown.");
            DrawWarningAndFallback(position, property, label, "No RoomManager found in the current scene.");
        }
    }

    // A helper method to safely log to the console only one time
    private void LogWarningOnce(string message)
    {
        if (!_hasLoggedWarning)
        {
            Debug.LogWarning(message);
            _hasLoggedWarning = true;
        }
    }

    private void DrawWarningAndFallback(Rect position, SerializedProperty property, GUIContent label, string warningMessage)
    {
        Rect helpBoxRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 2);
        EditorGUI.HelpBox(helpBoxRect, warningMessage, MessageType.Warning);

        Rect textFieldRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight * 2.2f), position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(textFieldRect, property, label);
    }
}