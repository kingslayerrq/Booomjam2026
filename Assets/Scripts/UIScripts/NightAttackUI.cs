using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NightAttackUI : MonoBehaviour
{
    [System.Serializable]
    public class DoorTimerSlot
    {
        [Tooltip("The door this slot displays. Match to DoorInteractable in scene.")]
        public DoorInteractable door;
        public GameObject slotRoot;
        public Slider timerSlider;
        public TMP_Text timerLabel;
        public TMP_Text doorStateLabel;
    }

    [SerializeField] private NightAttackManager nightAttackManager;
    [SerializeField] private List<DoorTimerSlot> doorSlots = new List<DoorTimerSlot>();
    [SerializeField] private float maxCountdownSeconds = 5f;

    private void Update()
    {
        if (nightAttackManager == null) return;

        IReadOnlyDictionary<DoorInteractable, float> timers = nightAttackManager.ActiveDoorTimers;

        foreach (DoorTimerSlot slot in doorSlots)
        {
            if (slot.door == null) continue;

            bool active = timers.TryGetValue(slot.door, out float remaining);

            if (slot.slotRoot != null)
                slot.slotRoot.SetActive(active);

            if (!active) continue;

            if (slot.timerSlider != null)
                slot.timerSlider.value = Mathf.Clamp01(remaining / maxCountdownSeconds);

            if (slot.timerLabel != null)
                slot.timerLabel.text = Mathf.CeilToInt(remaining).ToString();

            if (slot.doorStateLabel != null)
                slot.doorStateLabel.text = slot.door.IsOpen ? "OPEN" : "CLOSED";
        }
    }
}
