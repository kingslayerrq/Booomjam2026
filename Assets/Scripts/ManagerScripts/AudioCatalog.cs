using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioCatalog", menuName = "Audio/Audio Catalog")]
public class AudioCatalog : ScriptableObject
{
    [Header("Clips")]
    [SerializeField] private AudioClip metalFootstepLoop;
    [SerializeField] private AudioClip hit;
    [SerializeField] private AudioClip[] glitchVariants;
    [SerializeField] private AudioClip drinking;
    [SerializeField] private AudioClip cameraMoveLoop;
    [SerializeField] private AudioClip[] alarmVariants;
    [SerializeField] private AudioClip roomToneLoop;
    [SerializeField] private AudioClip[] triggerDoorVariants;
    [SerializeField] private AudioClip uiClick;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup ambientGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup surveillanceGroup;

    public AudioClip MetalFootstepLoop => metalFootstepLoop;
    public AudioClip Hit => hit;
    public AudioClip Drinking => drinking;
    public AudioClip CameraMoveLoop => cameraMoveLoop;
    public AudioClip RoomToneLoop => roomToneLoop;
    public AudioClip UiClick => uiClick;
    public AudioMixerGroup MasterGroup => masterGroup;
    public AudioMixerGroup SfxGroup => sfxGroup;
    public AudioMixerGroup AmbientGroup => ambientGroup;
    public AudioMixerGroup UiGroup => uiGroup;
    public AudioMixerGroup SurveillanceGroup => surveillanceGroup;

    public AudioClip RandomGlitch()
    {
        return RandomClip(glitchVariants);
    }

    public AudioClip RandomAlarm()
    {
        return RandomClip(alarmVariants);
    }

    public AudioClip RandomTriggerDoor()
    {
        return RandomClip(triggerDoorVariants);
    }

    private static AudioClip RandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        int startIndex = Random.Range(0, clips.Length);
        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[(startIndex + i) % clips.Length];
            if (clip != null)
                return clip;
        }

        return null;
    }
}
