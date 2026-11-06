using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [Header("Configuración de Puntos de Vida (HP)")]
    [Tooltip("Puntos de vida máximos del jugador (ej. 100 HP base)")]
    public float maxHealth = 100f;

    [Tooltip("Puntos de vida actuales del jugador")]
    public float currentHealth = 100f;

    [Header("Referencias UI (Canvas - Esquina Superior Izquierda)")]
    [Tooltip("Slider de Unity UI que representa la barra de vida")]
    public Slider healthSlider;

    [Tooltip("Imagen de relleno (Fill) del Slider para cambiar la tonalidad según la vida")]
    public Image fillImage;

    [Tooltip("Texto opcional para mostrar 'HP: 100/100'")]
    public Text hpLabelText;

    [Tooltip("Velocidad con la que la barra de vida baja suavemente al recibir daño")]
    public float smoothFillSpeed = 8f;

    [Header("Paleta de Colores (Estilo Viñedo / Pixel Art)")]
    public Color highHealthColor = new Color(0.88f, 0.18f, 0.22f, 1f); // Rojo carmesí vivo (como en la imagen de referencia)
    public Color midHealthColor = new Color(0.92f, 0.58f, 0.16f, 1f);  // Ámbar / Dorado viñedo
    public Color lowHealthColor = new Color(0.58f, 0.08f, 0.18f, 1f);  // Vino tinto crítico

    [Header("Reacción al Daño e Invulnerabilidad")]
    [Tooltip("Tiempo de invulnerabilidad en segundos tras recibir daño")]
    public float invincibilityDuration = 0.85f;

    [Tooltip("Fuerza de retroceso al ser golpeado por una semilla o enemigo")]
    public float knockbackForce = 4.2f;

    [Header("Game Over")]
    [Tooltip("Referencia al GameOverManager (se busca automáticamente si está vacío)")]
    public GameOverManager gameOverManager;

    private float targetSliderValue = 100f;
    private bool isInvincible = false;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private MovimientoJugador playerMovement;
    private PlayerAttack playerAttack;
    private Color originalSpriteColor = Color.white;

    private void Awake()
    {
        currentHealth = maxHealth;
        targetSliderValue = maxHealth;

        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<MovimientoJugador>();
        playerAttack = GetComponent<PlayerAttack>();

        if (spriteRenderer != null)
            originalSpriteColor = spriteRenderer.color;
    }

    private void Start()
    {
        if (gameOverManager == null)
            gameOverManager = FindFirstObjectByType<GameOverManager>();

        // Si el Slider no fue arrastrado manualmente, buscarlo en el Canvas
        if (healthSlider == null)
        {
            GameObject sliderObj = GameObject.Find("PlayerHealthSlider");
            if (sliderObj != null)
                healthSlider = sliderObj.GetComponent<Slider>();
        }

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;

            if (fillImage == null && healthSlider.fillRect != null)
                fillImage = healthSlider.fillRect.GetComponent<Image>();
        }

        if (hpLabelText == null)
        {
            GameObject txtObj = GameObject.Find("HPLabelText");
            if (txtObj != null)
                hpLabelText = txtObj.GetComponent<Text>();
        }

        UpdateHealthVisualsImmediate();
    }

    private void Update()
    {
        // Interpolación fluida del Slider hacia el valor real de HP
        if (healthSlider != null)
        {
            if (Mathf.Abs(healthSlider.value - targetSliderValue) > 0.05f)
            {
                healthSlider.value = Mathf.Lerp(
                    healthSlider.value,
                    targetSliderValue,
                    Time.unscaledDeltaTime * smoothFillSpeed
                );
            }
            else
            {
                healthSlider.value = targetSliderValue;
            }
        }
    }

    /// <summary>
    /// Resta puntos de vida al jugador y actualiza la barra de UI.
    /// </summary>
    public void TakeDamage(float damagePoints)
    {
        TakeDamageFromPosition(damagePoints, transform.position);
    }

    public void TakeDamage(int damagePoints)
    {
        TakeDamageFromPosition(damagePoints, transform.position);
    }

    public void RecibirDano(int damagePoints)
    {
        TakeDamageFromPosition(damagePoints, transform.position);
    }

    public void TakeDamageFromPosition(float damagePoints, Vector3 sourcePosition)
    {
        if (isDead || isInvincible)
            return;

        // Si algún script antiguo envía "1" de daño (del sistema de 5 corazones),
        // lo escalamos automáticamente a 20 HP para la base de 100 HP
        if (damagePoints <= 2f && maxHealth >= 50f)
        {
            damagePoints *= 20f;
        }

        currentHealth = Mathf.Clamp(currentHealth - damagePoints, 0f, maxHealth);
        targetSliderValue = currentHealth;

        UpdateHealthTextAndColor();

        Debug.Log($"[HealthSystem] Jugador recibió {damagePoints} de daño. HP actual: {currentHealth}/{maxHealth}");

        // Empuje (Knockback)
        if (rb != null)
        {
            float dirX = Mathf.Sign(transform.position.x - sourcePosition.x);
            if (dirX == 0f) dirX = -1f;
            rb.linearVelocity = new Vector2(dirX * knockbackForce, knockbackForce * 0.6f);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            StartCoroutine(DamageFlashRoutine());
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        targetSliderValue = currentHealth;
        UpdateHealthTextAndColor();
    }

    private void UpdateHealthVisualsImmediate()
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth;

        UpdateHealthTextAndColor();
    }

    private void UpdateHealthTextAndColor()
    {
        if (hpLabelText != null)
        {
            hpLabelText.text = $"HP: {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }

        if (fillImage != null)
        {
            float ratio = currentHealth / Mathf.Max(1f, maxHealth);
            if (ratio > 0.5f)
                fillImage.color = Color.Lerp(midHealthColor, highHealthColor, (ratio - 0.5f) * 2f);
            else
                fillImage.color = Color.Lerp(lowHealthColor, midHealthColor, ratio * 2f);
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                bool flash = Mathf.FloorToInt(elapsed * 15f) % 2 == 0;
                spriteRenderer.color = flash
                    ? new Color(1f, 0.38f, 0.38f, 0.9f)
                    : new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, 0.45f);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;

        isInvincible = false;
    }

    private void Die()
    {
        isDead = true;

        // Desactivar controles del jugador al morir
        if (playerMovement != null)
            playerMovement.enabled = false;
        if (playerAttack != null)
            playerAttack.enabled = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.65f, 0.2f, 0.25f, 0.8f);

        if (gameOverManager == null)
            gameOverManager = FindFirstObjectByType<GameOverManager>();

        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameOver();
        }
        else
        {
            Debug.LogWarning("[HealthSystem] No se encontró GameOverManager en la escena.");
        }
    }
}
