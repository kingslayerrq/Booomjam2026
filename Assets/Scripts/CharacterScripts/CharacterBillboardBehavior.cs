using System;
using UnityEngine;

public class CharacterBillboardBehavior : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera currentCamera;

    private void LateUpdate()
    {
        if (currentCamera == null) return;
        
        transform.rotation = currentCamera.transform.rotation;
    }
    
    public void SetCamera(Camera cam)
    {
        if (cam == null) return;
        currentCamera = cam;
    }
}
