using UnityEngine;

public class RoomAudioSettings : MonoBehaviour
{
    [SerializeField] private AudioClip roomToneOverride;

    public AudioClip RoomToneOverride => roomToneOverride;
}
