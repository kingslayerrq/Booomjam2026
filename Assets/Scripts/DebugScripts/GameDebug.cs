using UnityEngine;
using UnityEngine.InputSystem;

public class GameDebug : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    private void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
            gameManager.StartNextDay();
    }
}
