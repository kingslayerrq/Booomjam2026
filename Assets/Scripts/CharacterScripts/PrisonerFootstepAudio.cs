using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(PrisonerActionController))]
public class PrisonerFootstepAudio : MonoBehaviour
{
    private const float FadeSeconds = 0.06f;

    private PrisonerActionController controller;
    private AudioSource footstepSource;
    private bool forcedLoop;

    private void Awake()
    {
        controller = GetComponent<PrisonerActionController>();
    }

    private void Update()
    {
        if (controller == null)
            return;

        GameAudioManager audioManager = GameAudioManager.Instance;
        bool shouldPlay = (forcedLoop || controller.HasMoveTarget)
                          && controller.Prisoner is { IsLockedUp: false }
                          && audioManager.CanHearRoom(controller.CurrentRoom);

        if (shouldPlay)
        {
            EnsureSource(audioManager);
            PlayLoop(GetTargetVolume(audioManager));
        }
        else
        {
            StopLoop();
        }
    }

    public void SetForcedLoop(bool active)
    {
        forcedLoop = active;
        if (!forcedLoop)
        {
            StopLoop();
        }
    }

    private void EnsureSource(GameAudioManager audioManager)
    {
        if (footstepSource != null)
            return;

        footstepSource = audioManager.CreateFootstepLoopSource(transform);
    }

    private float GetTargetVolume(GameAudioManager audioManager)
    {
        float volume = audioManager.FootstepVolume;
        if (forcedLoop)
        {
            volume *= PrisonerEvidenceManager.Instance.NightAudioPromptMultiplier;
        }

        return volume;
    }

    private void PlayLoop(float targetVolume)
    {
        if (footstepSource == null || footstepSource.clip == null)
            return;

        footstepSource.DOKill();
        if (!footstepSource.isPlaying)
        {
            footstepSource.volume = 0f;
            footstepSource.Play();
        }

        footstepSource.DOFade(targetVolume, FadeSeconds);
    }

    private void StopLoop()
    {
        if (footstepSource == null)
            return;

        footstepSource.DOKill();
        if (!footstepSource.isPlaying)
            return;

        footstepSource.DOFade(0f, FadeSeconds).OnComplete(() =>
        {
            if (footstepSource != null)
            {
                footstepSource.Stop();
            }
        });
    }

    private void OnDisable()
    {
        StopLoop();
    }
}
