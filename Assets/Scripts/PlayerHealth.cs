using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Salud del Jugador")]
    [Tooltip("Vida máxima del jugador")]
    public int maxHealth = 5;

    [Tooltip("Vida actual del jugador")]
    public int currentHealth;

    [Header("Daño e Invulnerabilidad")]
    [Tooltip("Tiempo de invulnerabilidad tras recibir el impacto de una semilla")]
    public float invincibilityDuration = 1.0f;

    [Tooltip("Pequeño empuje hacia atrás al recibir daño")]
    public float knockbackForce = 4.5f;

    [Header("Interfaz Visual (HUD)")]
    [Tooltip("Mostrar barra de vida del jugador en la esquina superior izquierda")]
    public bool showHealthUI = true;

    private bool isInvincible = false;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Vector3 startSpawnPosition;
    private Color originalColor = Color.white;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        startSpawnPosition = transform.position;
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    public void RecibirDano(int amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    public void TakeDamageFromPosition(int amount, Vector3 attackerPosition)
    {
        float dirX = Mathf.Sign(transform.position.x - attackerPosition.x);
        if (dirX == 0f) dirX = 1f;
        TakeDamage(amount, new Vector2(dirX, 0.45f));
    }

    public void TakeDamage(int amount, Vector2 knockbackDir)
    {
        if (isInvincible || currentHealth <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        Debug.Log($"[PlayerHealth] ¡La semilla te hizo daño! Vida restante: {currentHealth}/{maxHealth}");

        if (rb != null && knockbackDir.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = new Vector2(knockbackDir.x * knockbackForce, knockbackForce * 0.65f);
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            StartCoroutine(InvincibilityFlashRoutine());
        }
    }

    private IEnumerator InvincibilityFlashRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                // Parpadeo entre rojo claro y semitransparente
                bool flash = Mathf.FloorToInt(elapsed * 14f) % 2 == 0;
                spriteRenderer.color = flash
                    ? new Color(1f, 0.45f, 0.45f, 0.85f)
                    : new Color(originalColor.r, originalColor.g, originalColor.b, 0.45f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        isInvincible = false;
    }

    private IEnumerator RespawnRoutine()
    {
        isInvincible = true;
        Debug.Log("[PlayerHealth] ¡El jugador fue derrotado! Reapareciendo en el punto inicial...");

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 0.25f, 0.25f, 0.6f);

        yield return new WaitForSeconds(0.4f);

        transform.position = startSpawnPosition;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        currentHealth = maxHealth;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        yield return new WaitForSeconds(0.5f);
        isInvincible = false;
    }

    private void OnGUI()
    {
        if (!showHealthUI) return;

        // Fondo de la barra de vida en esquina superior izquierda
        Rect panelRect = new Rect(16, 16, 210, 44);
        GUI.color = new Color(0.12f, 0.06f, 0.14f, 0.85f);
        GUI.Box(panelRect, GUIContent.none);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        labelStyle.normal.textColor = new Color(0.96f, 0.90f, 0.78f);
        GUI.color = Color.white;
        GUI.Label(new Rect(26, 20, 190, 20), $"VIDA JUGADOR: {currentHealth} / {maxHealth}", labelStyle);

        // Bloques de vida estilo retro
        for (int i = 0; i < maxHealth; i++)
        {
            Rect seg = new Rect(26 + i * 36, 40, 30, 12);
            GUI.color = (i < currentHealth)
                ? new Color(0.75f, 0.16f, 0.32f, 1f) // Rojo vino
                : new Color(0.28f, 0.22f, 0.26f, 0.7f);
            GUI.DrawTexture(seg, Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
    }
}
