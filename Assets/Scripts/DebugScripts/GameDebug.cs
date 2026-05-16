using UnityEngine;
using UnityEngine.InputSystem;

public class GameDebug : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private DayManager dayManager;

    private void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
            gameManager.StartNextDay();

        if (Keyboard.current.zKey.wasPressedThisFrame)
            dayManager.ForceEndCurrentPhase();
    }
}
