using UnityEngine;

public class RoomStaticLightsController : MonoBehaviour
{
    [SerializeField] private Light[] staticLights;

    public void SetStaticLights(bool isEnabled)
    {
        for (int i = 0; i < staticLights.Length; i++)
        {
            if (staticLights[i] == null) continue;
            staticLights[i].enabled = isEnabled;
        }
    }
}
