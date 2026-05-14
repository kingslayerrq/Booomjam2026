using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayManager dayManager;
    [SerializeField] private PrisonerManager prisonerManager;
    [SerializeField] private PlayerResource playerResource;
    [SerializeField] private PlayerHealth playerHealth;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject dayCompletePanel;
    [SerializeField] private GameObject halfDayCompletePanel;
    [SerializeField] private GameObject gameCompletePanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject[] gameplayObjectsToHideWhileMenuOpen = new GameObject[0];
    [Tooltip("Panels that will unlock cursor (locks player camera movement) when active")]
    [SerializeField] private GameObject[] cursorUnlockPanels;
    [Tooltip("Panels that will block camera control input when active")]
    [SerializeField] private GameObject[] blockCamControlPanels;
    
    [Header("Options")]
    [SerializeField] private bool loadSaveOnStart = false;

    public static bool IsMenuOpen { get; private set; }
    public static bool IsCursorUnlocked { get; private set; }
    public static bool BlockCamControl { get; private set; }
    
    private void OnEnable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayEnded += HandleDayEnd;
        }

        if (playerHealth != null)
        {
            playerHealth.OnPlayerHealthDepleted += HandleGameOver;
        }
        
    }
    

    private void OnDisable()
    {
        if (dayManager != null)
        {
            dayManager.OnDayEnded -= HandleDayEnd;
        }
        
        if (playerHealth != null)
        {
            playerHealth.OnPlayerHealthDepleted -= HandleGameOver;
        }
    }
    
    private void LateUpdate()
    {
        ApplyCursorState();
        ApplyCamControlState();
    }

    private void ApplyCamControlState()
    {
        BlockCamControl = IsAnyPanelActive(blockCamControlPanels);
    }

    private void ApplyCursorState()
    {
        bool shouldUnlockCursor = IsAnyPanelActive(cursorUnlockPanels);
        IsCursorUnlocked = shouldUnlockCursor;

        Cursor.lockState = shouldUnlockCursor
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = shouldUnlockCursor;
    }
    
    private static bool IsAnyPanelActive(GameObject[] panels)
    {
        foreach (GameObject panel in panels)
        {
            if (panel != null && panel.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void HandleGameOver()
    {
        dayManager.StopDay();
        HideGameStatePanels();
        gameOverPanel.SetActive(true);
        SaveSystem.DeleteSave();
    }

    /// <summary>
    /// Start the next day from morning
    /// </summary>
    public void StartNextDay()
    {
        HideGameStatePanels();
        dayManager.StartDay(dayManager.CurrentDay + 1);
        SaveCurrentGame();
    }

    /// <summary>
    /// Continues current day from mid day(afternoon)
    /// </summary>
    public void ContinueFromMidDay()
    {
        ContinueFromNight();
    }

    public void ContinueFromNight()
    {
        HideGameStatePanels();
        dayManager.StartDay(dayManager.CurrentDay, DayPhase.Night);
    }

    private void HandleDayEnd()
    {
        HideGameStatePanels();
        if (dayManager.CurrentDay >= dayManager.TotalDays)
        {
            gameCompletePanel.SetActive(true);
            SaveSystem.DeleteSave();
        }
        else
        {
            dayCompletePanel.SetActive(true);
        }
    }
    
    private void Start()
    {
        if (loadSaveOnStart && SaveSystem.TryLoad(out GameSaveData saveData))
        {
            LoadFromSaveData(saveData);
            return;
        }

        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        HideGameStatePanels();
        SetMenuOpen(true);
        RefreshContinueButton();
    }

    public void StartNewGame()
    {
        HideGameStatePanels();
        SetMenuOpen(false);

        playerResource.ResetResources();
        playerHealth.ResetHealth();
        if (prisonerManager != null) prisonerManager.InitPrisoners();
        
        dayManager.StartDay(1, DayPhase.Day);

        SaveCurrentGame();
    }

    public void ContinueGame()
    {
        LoadGame();
    }
    
    public void SaveCurrentGame()
    {
        GameSaveData saveData = new GameSaveData(
            dayManager.CurrentDay,
            playerResource.CurrentBatteryLevel,
            playerHealth.CurrentHealth
        );

        SaveSystem.Save(saveData);
    }
    
    public void DeleteSaveAndStartNewGame()
    {
        SaveSystem.DeleteSave();
        StartNewGame();
    }
    
    public void LoadGame()
    {
        if (!SaveSystem.TryLoad(out GameSaveData saveData))
        {
            RefreshContinueButton();
            return;
        }
        
        LoadFromSaveData(saveData);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    private void LoadFromSaveData(GameSaveData saveData)
    {
        HideGameStatePanels();
        SetMenuOpen(false);

        playerResource.SetBatteryLevel(saveData.batteryLevel);
        playerHealth.SetHealth(saveData.playerHealth);

        if (prisonerManager != null)
            prisonerManager.InitPrisoners();

        dayManager.StartDay(saveData.currentDay, DayPhase.Day);
    }
    
    private void HideGameStatePanels()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (dayCompletePanel != null)
            dayCompletePanel.SetActive(false);
        
        if (halfDayCompletePanel != null)
            halfDayCompletePanel.SetActive(false);

        if (gameCompletePanel != null)
            gameCompletePanel.SetActive(false);
    }

    private void SetMenuOpen(bool isOpen)
    {
        IsMenuOpen = isOpen;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(isOpen);

        foreach (GameObject gameplayObject in gameplayObjectsToHideWhileMenuOpen)
        {
            if (gameplayObject != null)
                gameplayObject.SetActive(!isOpen);
        }
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null)
            continueButton.interactable = SaveSystem.HasSave();
    }
}
