using System;
using UnityEngine;

public class CharacterBillboardBehavior : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera currentCamera;

    private void LateUpdate()
    {
        transform.rotation = currentCamera.transform.rotation;
    }
    
    // TODO: AssignCamera()
}
