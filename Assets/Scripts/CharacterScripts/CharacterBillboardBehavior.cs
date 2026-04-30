using System;
using UnityEngine;

public class CharacterBillboardBehavior : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera currentCamera;

    private void LateUpdate()
    {
        transform.LookAt(transform.position + currentCamera.transform.rotation * Vector3.forward,
            currentCamera.transform.rotation * Vector3.up);
    }
}
