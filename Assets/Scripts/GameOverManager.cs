using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Referencias UI de Game Over")]
    [Tooltip("Panel principal de Game Over dentro del Canvas (desactivado al iniciar)")]
    public GameObject gameOverPanel;

    [Tooltip("Botón de 'REINTENTAR'")]
    public Button restartButton;

    [Tooltip("Grupo de Canvas opcional para hacer un Fade-In suave al morir")]
    public CanvasGroup panelCanvasGroup;

    [Header("Configuración")]
    [Tooltip("Pequeña pausa antes de mostrar el Game Over para ver el golpe final")]
    public float delayBeforePause = 0.30f;

    [Tooltip("Permite reintentar también presionando R o Enter en la pantalla de Game Over")]
    public bool permitirReintentarConTeclado = true;

    private bool isGameOver = false;
    private bool isPanelReady = false;
    private bool isRestarting = false;
    private float gameOverShownUnscaledTime = 0f;
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

        // Asegurar que el tiempo corra normalmente si el Opener no está activo
        TitleScreenOpener opener = FindFirstObjectByType<TitleScreenOpener>();
        if (opener == null || !opener.enabled || TitleScreenOpener.OmitirOpenerEnReinicio)
        {
            Time.timeScale = 1f;
        }

        EnsureEventSystemExists();
        BuscarReferenciasUISiFaltan();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        VincularBotonReintentar();
    }

    private void Update()
    {
        if (!isGameOver)
            return;

        // Animación suave de escala en el botón "REINTENTAR" al pasar el mouse (funciona con Time.timeScale = 0)
        if (restartButton != null)
        {
            restartButton.transform.localScale = Vector3.Lerp(
                restartButton.transform.localScale,
                buttonBaseScale * targetButtonScale,
                Time.unscaledDeltaTime * 14f
            );
        }

        // Atajo cómodo de teclado (R o Enter) tras un breve margen para evitar reinicios accidentales
        if (permitirReintentarConTeclado && isPanelReady && !isRestarting &&
            Time.unscaledTime - gameOverShownUnscaledTime >= 0.35f &&
            Keyboard.current != null)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                RestartCurrentLevel();
            }
        }
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

        BuscarReferenciasUISiFaltan();
        VincularBotonReintentar();

        if (gameOverPanel != null)
        {
            // Colocar siempre por encima de las barras de vida, jefe y banners
            gameOverPanel.transform.SetAsLastSibling();

            // Oscurecer suavemente el fondo para que el cuadro de Game Over y el botón destaquen
            Image bgImg = gameOverPanel.GetComponent<Image>();
            if (bgImg != null && bgImg.color.a < 0.1f)
            {
                bgImg.color = new Color(0.05f, 0.01f, 0.04f, 0.68f);
            }

            gameOverPanel.SetActive(true);
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
            restartButton.interactable = true;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
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

        isPanelReady = true;
        gameOverShownUnscaledTime = Time.unscaledTime;

        // Pausar el juego mientras está la pantalla de Game Over
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Método público conectado al OnClick() del botón "REINTENTAR".
    /// Restablece Time.timeScale = 1 y recarga la escena actual (respetando checkpoints si aplica).
    /// </summary>
    public void RestartCurrentLevel()
    {
        if (isRestarting) return;
        isRestarting = true;

        TitleScreenOpener.OmitirOpenerEnReinicio = true;
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.buildIndex >= 0)
            SceneManager.LoadScene(currentScene.buildIndex);
        else
            SceneManager.LoadScene(currentScene.name);
    }

    private void BuscarReferenciasUISiFaltan()
    {
        if (gameOverPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform found = canvas.transform.Find("GameOverPanel");
                if (found != null)
                {
                    gameOverPanel = found.gameObject;
                }
            }
        }

        if (gameOverPanel != null)
        {
            if (panelCanvasGroup == null)
                panelCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();

            if (restartButton == null)
            {
                Transform btnTrans = gameOverPanel.transform.Find("GameOverArtwork/RestartButton");
                if (btnTrans != null)
                    restartButton = btnTrans.GetComponent<Button>();
                else
                    restartButton = gameOverPanel.GetComponentInChildren<Button>(true);
            }
        }
    }

    private void VincularBotonReintentar()
    {
        if (restartButton == null) return;

        buttonBaseScale = restartButton.transform.localScale;
        if (buttonBaseScale.sqrMagnitude < 0.001f)
            buttonBaseScale = Vector3.one;

        restartButton.onClick.RemoveListener(RestartCurrentLevel);
        restartButton.onClick.AddListener(RestartCurrentLevel);
        SetupButtonHoverEffects(restartButton.gameObject);
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
