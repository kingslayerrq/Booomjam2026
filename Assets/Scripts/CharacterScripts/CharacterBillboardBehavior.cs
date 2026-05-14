using System;
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterBillboardBehavior : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera currentCamera;
    [SerializeField] private bool followCameraRotation = true;
    [FormerlySerializedAs("collider")] [SerializeField] private Collider col;

    private void Awake()
    {
        if  (col == null)
        {
            col = GetComponentInParent<Collider>();
        }
    }

    private void LateUpdate()
    {
        if (currentCamera == null) return;

        if (followCameraRotation)
        {
            // transform.rotation = currentCamera.transform.rotation;
            Vector3 targetPos = currentCamera.transform.position;
            targetPos.y = transform.position.y; // Lock the height so it doesn't tilt up/down
            transform.LookAt(targetPos);
        }
    }
    
    public void SetCamera(Camera cam)
    {
        if (cam == null) return;
        currentCamera = cam;
    }
    
    public void SetFollowCameraRotation(bool b)
    {
        followCameraRotation = b;
    }
}
