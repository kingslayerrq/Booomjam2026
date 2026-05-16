using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PrisonerScheduleUI : MonoBehaviour
{
    private const int PrisonersPerPage = 5;

    [Header("References")]
    [SerializeField] private PrisonerManager prisonerManager;
    [SerializeField] private DayManager dayManager;

    [Header("Slot UI (5 slots)")]
    [SerializeField] private PrisonerSlotUI[] slots;

    [Header("Pagination")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI pageLabel;

    [Header("Popup")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Image popupProfilePic;
    [SerializeField] private TextMeshProUGUI popupName;
    [SerializeField] private TextMeshProUGUI popupID;
    [SerializeField] private TextMeshProUGUI popupArrestedReason;
    [SerializeField] private PrisonerScheduleEntryUI[] popupScheduleEntries;

    private int currentPage = 0;
    private int totalPages = 1;
    private IReadOnlyList<Prisoner> prisoners;

    private void Awake()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        foreach (var entry in popupScheduleEntries)
            if (entry != null) entry.gameObject.SetActive(false);

        if (dayManager != null)
            dayManager.OnDayStarted += RefreshSchedules;
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void OnDestroy()
    {
        if (dayManager != null)
            dayManager.OnDayStarted -= RefreshSchedules;
    }

    public void Open()
    {
        currentPage = 0;
        gameObject.SetActive(true);
        ClosePopup();
        Refresh();
    }

    public void Close()
    {
        ClosePopup();
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        prisoners = prisonerManager != null ? prisonerManager.PrisonerList : null;
        int count = prisoners?.Count ?? 0;
        totalPages = Mathf.Max(1, Mathf.CeilToInt(count / (float)PrisonersPerPage));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
        RebuildPage();
    }

    // Called by DayManager.OnDayStarted — schedule data is fresh at this point
    private void RefreshSchedules()
    {
        Refresh();
        ClosePopup();
    }

    private void RebuildPage()
    {
        int startIndex = currentPage * PrisonersPerPage;

        for (int i = 0; i < slots.Length; i++)
        {
            int prisonerIndex = startIndex + i;
            if (prisoners != null && prisonerIndex < prisoners.Count)
            {
                slots[i].Setup(prisoners[prisonerIndex], OnSlotClicked);
                slots[i].gameObject.SetActive(true);
            }
            else
            {
                slots[i].Clear();
                slots[i].gameObject.SetActive(false);
            }
        }

        if (pageLabel != null)
            pageLabel.text = $"{currentPage + 1}/{totalPages}";

        if (prevButton != null) prevButton.interactable = currentPage > 0;
        if (nextButton != null) nextButton.interactable = currentPage < totalPages - 1;
    }

    public void OnPrevPage()
    {
        if (currentPage <= 0) return;
        currentPage--;
        ClosePopup();
        RebuildPage();
    }

    public void OnNextPage()
    {
        if (currentPage >= totalPages - 1) return;
        currentPage++;
        ClosePopup();
        RebuildPage();
    }

    private void OnSlotClicked(Prisoner prisoner)
    {
        OpenPopup(prisoner);
    }

    private void OpenPopup(Prisoner prisoner)
    {
        if (popupPanel == null) return;

        PrisonerData data = prisoner.PrisonerData;

        if (popupProfilePic != null)
        {
            popupProfilePic.sprite = data.PrisonerHeadProfilePic;
            popupProfilePic.enabled = data.PrisonerHeadProfilePic != null;
        }

        if (popupName != null)    popupName.text    = data.PrisonerName;
        if (popupID != null)      popupID.text      = $"{data.PrisonerID}";
        if (popupArrestedReason != null) popupArrestedReason.text = data.ArrestedReason;

        var schedule = PrisonerSchedule.Instance != null
            ? PrisonerSchedule.Instance.GetSchedule(prisoner.PrisonerID)
            : null;

        for (int i = 0; i < popupScheduleEntries.Length; i++)
        {
            if (schedule != null && i < schedule.Count)
                popupScheduleEntries[i].Setup(schedule[i]);
            else
                popupScheduleEntries[i].Clear();
        }

        foreach (var entry in popupScheduleEntries)
            if (entry != null) entry.gameObject.SetActive(true);

        popupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        foreach (var entry in popupScheduleEntries)
            if (entry != null) entry.gameObject.SetActive(false);
    }
}
