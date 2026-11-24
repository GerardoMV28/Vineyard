using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Habilidad Especial del Jugador para el Nivel 4 ("¡SUPER CORCHAZO DE RESERVA!"):
/// - Se carga al combatir: +14% por cada corchazo acertado y +22% por cada uva invocada derrotada
///   (más carga pasiva suave durante el combate contra el Wine Monster).
/// - Al llegar al 100%, el jugador puede presionar Q, R o Click Derecho para lanzar un Super Corcho
///   dorado que atraviesa enemigos normales e inflige 350 HP masivos al Jefe Final.
/// </summary>
public class JugadorGolpeEspecial : MonoBehaviour
{
    public static JugadorGolpeEspecial Instancia { get; private set; }

    [Header("Configuración del Super Corchazo")]
    public float cargaMaxima = 100f;
    [Range(0f, 100f)]
    public float cargaActual = 50f;
    public int danoEspecialAlJefe = 350;
    public float cooldownDisparo = 0.55f;

    [Header("Referencias HUD")]
    public Slider barraCargaEspecial;
    public Image imagenRellenoBarra;
    public Text textoEstadoEspecial;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoLanzarSuperCorcho;
    public AudioClip sonidoCargaLista;

    private SpriteRenderer spriteRenderer;
    private PlayerAttack playerAttack;
    private float siguienteDisparoPermitido = 0f;
    private bool yaAvisoListo = false;
    private Color colorBaseRelleno = new Color(0.95f, 0.72f, 0.16f, 1f);
    private Color colorListoA = new Color(1.0f, 0.92f, 0.25f, 1f);
    private Color colorListoB = new Color(0.35f, 1.0f, 0.68f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instancia = null;
    }

    /// <summary>
    /// Método estático cómodo para sumar carga desde CorkProjectile o JefeWineMonster.
    /// </summary>
    public static void NotificarCarga(float cantidad)
    {
        if (Instancia != null)
        {
            Instancia.AgregarCarga(cantidad);
        }
    }

    private void Awake()
    {
        Instancia = this;
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerAttack = GetComponent<PlayerAttack>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        ActualizarHUD();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        // Efecto de brillo vibrante cuando el Super Corchazo está al 100%
        if (EstaListo())
        {
            if (imagenRellenoBarra != null)
            {
                float pulse = Mathf.PingPong(Time.time * 4.5f, 1f);
                imagenRellenoBarra.color = Color.Lerp(colorListoA, colorListoB, pulse);
            }
        }

        if (EstaListo() && Time.time >= siguienteDisparoPermitido && SePresionoTeclaEspecial())
        {
            DispararSuperCorchazo();
        }
    }

    public bool EstaListo()
    {
        return cargaActual >= cargaMaxima - 0.01f;
    }

    public void AgregarCarga(float cantidad)
    {
        if (cantidad <= 0f) return;

        bool estabaListo = EstaListo();
        cargaActual = Mathf.Clamp(cargaActual + cantidad, 0f, cargaMaxima);

        if (!estabaListo && EstaListo() && !yaAvisoListo)
        {
            yaAvisoListo = true;
            if (sonidoCargaLista != null && audioSource != null)
            {
                audioSource.PlayOneShot(sonidoCargaLista, 0.85f);
            }

            JefeWineMonster boss = JefeWineMonster.Instancia;
            if (boss != null && !boss.EstaDerrotado)
            {
                boss.MostrarMensajeBanner("★ ¡SUPER CORCHAZO LISTO! Presiona [Q] o [Click Der.] para golpear al Jefe ★", 3.2f);
            }
        }

        ActualizarHUD();
    }

    private bool SePresionoTeclaEspecial()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame))
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    private void DispararSuperCorchazo()
    {
        cargaActual = 0f;
        yaAvisoListo = false;
        siguienteDisparoPermitido = Time.time + cooldownDisparo;

        float dir = 1f;
        if (spriteRenderer != null)
            dir = spriteRenderer.flipX ? -1f : 1f;

        Vector3 spawnPos = (playerAttack != null && playerAttack.firePoint != null)
            ? playerAttack.firePoint.position
            : transform.position + new Vector3(dir * 0.85f, 0.12f, 0f);

        SuperCorchazoProyectil.CrearYLanzar(spawnPos, dir, danoEspecialAlJefe);

        if (sonidoLanzarSuperCorcho != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoLanzarSuperCorcho, 0.95f);
        }

        // Destello dorado de descorche en el punto de disparo
        EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
            spawnPos,
            16,
            new Color(1.0f, 0.88f, 0.25f, 1f),
            new Color(0.40f, 1.0f, 0.68f, 1f)
        );

        StartCoroutine(RetrocesoEspecialRoutine(dir));
        ActualizarHUD();
    }

    private IEnumerator RetrocesoEspecialRoutine(float dir)
    {
        if (spriteRenderer != null)
        {
            Color prev = spriteRenderer.color;
            spriteRenderer.color = new Color(1.0f, 0.95f, 0.60f, 1f);
            yield return new WaitForSeconds(0.12f);
            spriteRenderer.color = prev;
        }
    }

    private void ActualizarHUD()
    {
        if (barraCargaEspecial != null)
        {
            barraCargaEspecial.maxValue = cargaMaxima;
            barraCargaEspecial.value = cargaActual;
        }

        if (imagenRellenoBarra != null && !EstaListo())
        {
            imagenRellenoBarra.color = colorBaseRelleno;
        }

        if (textoEstadoEspecial != null)
        {
            if (EstaListo())
            {
                textoEstadoEspecial.text = "★ SUPER CORCHAZO LISTO [Q / Click Der.] ★";
                textoEstadoEspecial.color = new Color(1.0f, 0.96f, 0.45f, 1f);
            }
            else
            {
                int pct = Mathf.FloorToInt((cargaActual / Mathf.Max(1f, cargaMaxima)) * 100f);
                textoEstadoEspecial.text = $"SUPER CORCHAZO: {pct}%  (Pelea para cargar)";
                textoEstadoEspecial.color = new Color(0.95f, 0.88f, 0.72f, 1f);
            }
        }
    }
}
