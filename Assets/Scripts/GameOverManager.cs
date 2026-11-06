using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Referencias UI de Game Over")]
    [Tooltip("Panel principal de Game Over dentro del Canvas (desactivado al iniciar)")]
    public GameObject gameOverPanel;

    [Tooltip("Botón de 'JUGAR DE NUEVO'")]
    public Button restartButton;

    [Tooltip("Grupo de Canvas opcional para hacer un Fade-In suave al morir")]
    public CanvasGroup panelCanvasGroup;

    [Header("Configuración")]
    [Tooltip("Pequeña pausa antes de mostrar el Game Over para ver el golpe final")]
    public float delayBeforePause = 0.30f;

    private bool isGameOver = false;
    private Vector3 buttonBaseScale = Vector3.one;
    private float targetButtonScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Asegurar que el tiempo corra normalmente al iniciar/recargar el nivel
        Time.timeScale = 1f;

        EnsureEventSystemExists();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            buttonBaseScale = restartButton.transform.localScale;
            restartButton.onClick.RemoveListener(RestartCurrentLevel);
            restartButton.onClick.AddListener(RestartCurrentLevel);
            SetupButtonHoverEffects(restartButton.gameObject);
        }
    }

    private void Update()
    {
        if (!isGameOver || restartButton == null)
            return;

        // Animación suave de escala en el botón "JUGAR DE NUEVO" al pasar el mouse (funciona con Time.timeScale = 0)
        restartButton.transform.localScale = Vector3.Lerp(
            restartButton.transform.localScale,
            buttonBaseScale * targetButtonScale,
            Time.unscaledDeltaTime * 14f
        );
    }

    /// <summary>
    /// Activa la pantalla de Game Over cuando la vida del jugador llega a 0.
    /// </summary>
    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        StartCoroutine(ShowGameOverRoutine());
    }

    private IEnumerator ShowGameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforePause);

        if (gameOverPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform found = canvas.transform.Find("GameOverPanel");
                if (found != null)
                {
                    gameOverPanel = found.gameObject;
                    panelCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
                }
            }
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            float duration = 0.25f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;
        }

        // Pausar el juego mientras está la pantalla de Game Over
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Método público conectado al OnClick() del botón "JUGAR DE NUEVO".
    /// Restablece Time.timeScale = 1 y recarga la escena actual.
    /// </summary>
    public void RestartCurrentLevel()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    private void SetupButtonHoverEffects(GameObject btnObj)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = btnObj.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((_) => { targetButtonScale = 1.05f; });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((_) => { targetButtonScale = 1.0f; });
        trigger.triggers.Add(exitEntry);

        EventTrigger.Entry downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener((_) => { targetButtonScale = 0.95f; });
        trigger.triggers.Add(downEntry);

        EventTrigger.Entry upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback.AddListener((_) => { targetButtonScale = 1.05f; });
        trigger.triggers.Add(upEntry);
    }

    private void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }
    }
}
