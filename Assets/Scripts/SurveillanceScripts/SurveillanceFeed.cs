using System;
using UnityEngine;

[Serializable]
public class SurveillanceFeed
{
    public string cameraId;
    public string displayName;
    [RoomDropdown]
    public string roomName;
    public Camera camera;
    public RenderTexture renderTexture;

    public string RoomLabel => string.IsNullOrWhiteSpace(roomName) ? displayName : roomName;
}
