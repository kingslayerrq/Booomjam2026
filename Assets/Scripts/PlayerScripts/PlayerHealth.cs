using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;

    [Header("Damage Settings")] 
    [SerializeField] private int energyDepletionDamage;
    
    public int CurrentHealth => currentHealth;
    
    public event Action OnPlayerHealthDepleted;

    private void Awake()
    {
        if (maxHealth <= 0)
        {
            Debug.LogWarning("[PlayerHealth] Max Health is invalid.");
            return;
        }
    }

    public void TakeDamageFromEnergyDepletion()
    {
        TakeDamage(energyDepletionDamage);
    }

    private void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        if (currentHealth <= 0)
        {
            OnPlayerHealthDepleted?.Invoke();
        }
    }
    
    public void SetHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
    
    
}
