using System;
using UnityEngine;

[Serializable]
public class SurveillanceFeed
{
    public string cameraId;
    public string displayName;
    public Camera camera;
    public RenderTexture renderTexture;
}
