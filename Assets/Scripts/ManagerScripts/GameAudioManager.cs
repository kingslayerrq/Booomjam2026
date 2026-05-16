using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameAudioManager : MonoBehaviour
{
    private const string DefaultCatalogResourcePath = "Audio/DefaultAudioCatalog";
    private const float LoopFadeSeconds = 0.08f;
    private const float OneShotDestroyPaddingSeconds = 0.25f;

    private static GameAudioManager instance;

    [SerializeField] private AudioCatalog catalog;
    [SerializeField] private float uiVolume = 1f;
    [SerializeField] private float oneShotVolume = 1f;
    [SerializeField] private float ambientVolume = 0.5f;
    [SerializeField] private float cameraMoveVolume = 0.6f;
    [SerializeField] private float footstepVolume = 0.65f;
    [SerializeField] private float max3dDistance = 22f;

    private AudioSource uiSource;
    private AudioSource roomToneSource;
    private AudioSource cameraMoveSource;
    private AudioListener mainListener;
    private AudioListener activeCameraListener;
    private Camera activeSurveillanceCamera;
    private GameObject activeAudibleRoom;

    public static GameAudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameAudioManager>();
            }

            if (instance == null)
            {
                GameObject audioObject = new GameObject("GameAudioManager");
                instance = audioObject.AddComponent<GameAudioManager>();
            }

            return instance;
        }
    }

    public AudioCatalog Catalog => catalog;
    public GameObject ActiveAudibleRoom => activeAudibleRoom;
    public bool IsSurveillanceAudioActive => activeSurveillanceCamera != null && activeAudibleRoom != null;
    public float FootstepVolume => footstepVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Instance.BindButtonClickSfxInScene();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSetup();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        BindButtonClickSfxInScene();
        UpdateGameplayRoomToneState();
    }

    private void Update()
    {
        UpdateGameplayRoomToneState();
    }

    public void PlayUI(AudioClip clip)
    {
        EnsureSetup();
        if (clip == null || uiSource == null)
            return;

        uiSource.PlayOneShot(clip, uiVolume);
    }

    public void PlayOneShot(AudioClip clip, Vector3 worldPosition, GameObject room = null)
    {
        EnsureSetup();
        if (clip == null || !CanHearRoom(room))
            return;

        AudioSource source = CreateOneShotSource(worldPosition, catalog != null ? catalog.SfxGroup : null);
        source.PlayOneShot(clip, oneShotVolume);
        Destroy(source.gameObject, clip.length + OneShotDestroyPaddingSeconds);
    }

    public void PlayRoomOneShot(AudioClip clip, GameObject room, Vector3 worldPosition)
    {
        PlayOneShot(clip, worldPosition, room);
    }

    public void PlayCatalogUiClick()
    {
        PlayUI(catalog != null ? catalog.UiClick : null);
    }

    public void PlayDrinking()
    {
        PlayUI(catalog != null ? catalog.Drinking : null);
    }

    public void PlayRandomDoor(GameObject room, Vector3 worldPosition)
    {
        PlayRoomOneShot(catalog != null ? catalog.RandomTriggerDoor() : null, room, worldPosition);
    }

    public void PlayRandomAlarm()
    {
        PlayUI(catalog != null ? catalog.RandomAlarm() : null);
    }

    public void PlayRandomGlitch()
    {
        if (IsSurveillanceAudioActive)
        {
            PlayRoomOneShot(catalog != null ? catalog.RandomGlitch() : null, activeAudibleRoom, activeSurveillanceCamera.transform.position);
            return;
        }

        PlayUI(catalog != null ? catalog.RandomGlitch() : null);
    }

    public void PlayActiveSurveillanceCue(AudioClip clip)
    {
        if (clip == null)
            return;

        if (IsSurveillanceAudioActive)
        {
            PlayRoomOneShot(clip, activeAudibleRoom, activeSurveillanceCamera.transform.position);
            return;
        }

        PlayUI(clip);
    }

    public void PlayHit()
    {
        PlayUI(catalog != null ? catalog.Hit : null);
    }

    public void SetActiveSurveillanceCamera(Camera camera, GameObject room)
    {
        EnsureSetup();
        if (camera == null || room == null)
        {
            ClearActiveSurveillanceCamera();
            return;
        }

        if (activeSurveillanceCamera == camera && activeAudibleRoom == room)
            return;

        StopRoomTone();
        StopCameraMoveLoop();
        DisableActiveCameraListener();

        activeSurveillanceCamera = camera;
        activeAudibleRoom = room;
        EnableOnlyActiveCameraListener(camera);
    }

    public void ClearActiveSurveillanceCamera()
    {
        StopRoomTone();
        StopCameraMoveLoop();
        DisableActiveCameraListener();
        activeSurveillanceCamera = null;
        activeAudibleRoom = null;
        RestoreMainListener();
        UpdateGameplayRoomToneState();
    }

    public void SetCameraMoveLoopActive(bool active, Transform cameraTransform = null)
    {
        EnsureSetup();
        if (!IsSurveillanceAudioActive || catalog == null || catalog.CameraMoveLoop == null)
        {
            StopCameraMoveLoop();
            return;
        }

        if (!active)
        {
            StopCameraMoveLoop();
            return;
        }

        Transform parent = cameraTransform != null ? cameraTransform : activeSurveillanceCamera.transform;
        if (cameraMoveSource == null)
        {
            cameraMoveSource = CreateLoopSource("CameraMoveLoop", parent, catalog.SurveillanceGroup, cameraMoveVolume);
            cameraMoveSource.clip = catalog.CameraMoveLoop;
        }
        else if (cameraMoveSource.transform.parent != parent)
        {
            cameraMoveSource.transform.SetParent(parent, false);
            cameraMoveSource.transform.localPosition = Vector3.zero;
        }

        PlayLoop(cameraMoveSource, cameraMoveVolume);
    }

    public AudioSource CreateFootstepLoopSource(Transform parent)
    {
        EnsureSetup();
        AudioSource source = CreateLoopSource("PrisonerFootstepLoop", parent, catalog != null ? catalog.SfxGroup : null, footstepVolume);
        source.clip = catalog != null ? catalog.MetalFootstepLoop : null;
        return source;
    }

    public bool CanHearRoom(GameObject room)
    {
        if (!IsSurveillanceAudioActive)
            return true;

        return room != null && room == activeAudibleRoom;
    }

    public void BindButtonClickSfxInScene()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.GetComponent<UIButtonSfx>() == null)
            {
                button.gameObject.AddComponent<UIButtonSfx>();
            }
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mainListener = null;
        RestoreMainListener();
        BindButtonClickSfxInScene();
        UpdateGameplayRoomToneState();
    }

    private void EnsureSetup()
    {
        if (catalog == null)
        {
            catalog = Resources.Load<AudioCatalog>(DefaultCatalogResourcePath);
        }

        if (uiSource == null)
        {
            uiSource = gameObject.GetComponent<AudioSource>();
            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
            }

            Configure2DSource(uiSource, catalog != null ? catalog.UiGroup : null);
        }

        if (mainListener == null)
        {
            mainListener = FindMainListener();
        }
    }

    private AudioSource CreateOneShotSource(Vector3 worldPosition, AudioMixerGroup mixerGroup)
    {
        GameObject sourceObject = new GameObject("AudioOneShot");
        sourceObject.transform.position = worldPosition;
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        Configure3DSource(source, mixerGroup);
        source.loop = false;
        source.playOnAwake = false;
        return source;
    }

    private AudioSource CreateLoopSource(string sourceName, Transform parent, AudioMixerGroup mixerGroup, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(parent, false);
        sourceObject.transform.localPosition = Vector3.zero;
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        Configure3DSource(source, mixerGroup);
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        return source;
    }

    private void Configure2DSource(AudioSource source, AudioMixerGroup mixerGroup)
    {
        source.outputAudioMixerGroup = mixerGroup;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    private void Configure3DSource(AudioSource source, AudioMixerGroup mixerGroup)
    {
        source.outputAudioMixerGroup = mixerGroup;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = max3dDistance;
    }

    private void UpdateGameplayRoomToneState()
    {
        if (ShouldPlayGameplayRoomTone())
        {
            StartGameplayRoomTone();
            return;
        }

        StopRoomTone();
    }

    private bool ShouldPlayGameplayRoomTone()
    {
        return !IsSurveillanceAudioActive && !GameManager.IsMenuOpen;
    }

    private void StartGameplayRoomTone()
    {
        EnsureSetup();
        if (catalog == null || catalog.RoomToneLoop == null)
            return;

        if (roomToneSource == null)
        {
            GameObject sourceObject = new GameObject("GameplayRoomToneLoop");
            sourceObject.transform.SetParent(transform, false);
            sourceObject.transform.localPosition = Vector3.zero;

            roomToneSource = sourceObject.AddComponent<AudioSource>();
            Configure2DSource(roomToneSource, catalog.AmbientGroup);
            roomToneSource.loop = true;
            roomToneSource.volume = 0f;
            roomToneSource.clip = catalog.RoomToneLoop;
        }

        PlayLoop(roomToneSource, ambientVolume);
    }

    private void StopRoomTone()
    {
        StopAndDestroyLoop(roomToneSource);
        roomToneSource = null;
    }

    private void StopCameraMoveLoop()
    {
        StopAndDestroyLoop(cameraMoveSource);
        cameraMoveSource = null;
    }

    private static void PlayLoop(AudioSource source, float targetVolume)
    {
        if (source == null || source.clip == null)
            return;

        source.DOKill();
        if (!source.isPlaying)
        {
            source.volume = 0f;
            source.Play();
        }

        source.DOFade(targetVolume, LoopFadeSeconds);
    }

    private static void StopAndDestroyLoop(AudioSource source)
    {
        if (source == null)
            return;

        source.DOKill();
        if (!source.isPlaying)
        {
            Destroy(source.gameObject);
            return;
        }

        source.DOFade(0f, LoopFadeSeconds)
            .OnComplete(() =>
            {
                if (source != null)
                {
                    Destroy(source.gameObject);
                }
            });
    }

    private void EnableOnlyActiveCameraListener(Camera camera)
    {
        if (mainListener == null)
        {
            mainListener = FindMainListener();
        }

        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] != null)
            {
                listeners[i].enabled = false;
            }
        }

        activeCameraListener = camera.GetComponent<AudioListener>();
        if (activeCameraListener == null)
        {
            activeCameraListener = camera.gameObject.AddComponent<AudioListener>();
        }

        activeCameraListener.enabled = true;
    }

    private void DisableActiveCameraListener()
    {
        if (activeCameraListener != null)
        {
            activeCameraListener.enabled = false;
            activeCameraListener = null;
        }
    }

    private void RestoreMainListener()
    {
        if (mainListener == null)
        {
            mainListener = FindMainListener();
        }

        if (mainListener != null)
        {
            mainListener.enabled = true;
        }
    }

    private static AudioListener FindMainListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != null && listener.enabled)
                return listener;
        }

        return listeners.Length > 0 ? listeners[0] : null;
    }
}
