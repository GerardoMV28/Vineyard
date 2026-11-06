using UnityEngine;

/// <summary>
/// Adaptador de compatibilidad que redirige el daño al nuevo componente HealthSystem (100 HP con Slider en Canvas).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    private HealthSystem healthSystem;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            healthSystem = gameObject.AddComponent<HealthSystem>();
        }
    }

    public void TakeDamage(int amount)
    {
        if (healthSystem != null)
            healthSystem.TakeDamage(amount);
    }

    public void RecibirDano(int amount)
    {
        if (healthSystem != null)
            healthSystem.TakeDamage(amount);
    }

    public void TakeDamageFromPosition(int amount, Vector3 attackerPosition)
    {
        if (healthSystem != null)
            healthSystem.TakeDamageFromPosition(amount, attackerPosition);
    }
}
