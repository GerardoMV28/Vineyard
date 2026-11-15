using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo cuerpo a cuerpo "Cab. Sau" (Racimo de uvas Cabernet Sauvignon rojo rubí) para el Nivel 2.
/// Patrulla lentamente entre dos puntos detectando bordes de plataforma y paredes para no caerse.
/// Cuando el jugador entra en su rangoDeteccion horizontal (y altura similar), entra en modo furia:
/// activa el parámetro bool "EstaCorriendo" en su Animator, reproduce su grito de alerta y embiste
/// a gran velocidad (velocidadSprint) hacia el jugador.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(AudioSource))]
public class EnemigoCabSau : MonoBehaviour
{
    private static readonly int EstaCorriendoHash = Animator.StringToHash("EstaCorriendo");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    [Header("Salud y Puntuación")]
    [Tooltip("Puntos de vida del enemigo Cab. Sau (impactos de corcho necesarios)")]
    public int maxHealth = 3;
    public int currentHealth = 3;

    [Tooltip("Puntos que suma al marcador del nivel al ser derrotado")]
    public int puntosAlDerrotar = 150;

    [Header("Patrulla Terrestre y Sensores de Borde")]
    [Tooltip("Velocidad al caminar tranquilamente durante la patrulla")]
    public float velocidadPatrulla = 1.65f;

    [Tooltip("Distancia hacia izquierda y derecha desde su punto inicial (si no se asignan puntos A y B)")]
    public float distanciaPatrulla = 3.5f;

    [Tooltip("Punto opcional izquierdo de patrulla (si se deja vacío usa distanciaPatrulla)")]
    public Transform puntoPatrullaA;

    [Tooltip("Punto opcional derecho de patrulla (si se deja vacío usa distanciaPatrulla)")]
    public Transform puntoPatrullaB;

    [Tooltip("Transform hijo (DetectorBorde) desde donde se lanza el Raycast hacia abajo apuntando a la capa Suelo")]
    public Transform detectorBorde;

    [Tooltip("Detecta bordes de plataforma con un Raycast hacia abajo para no caerse")]
    public bool detectarBordes = true;

    [Tooltip("Evita que caiga al vacío o al ácido incluso mientras corre en modo furia")]
    public bool frenarEnBordesDuranteSprint = true;

    [Tooltip("Distancia horizontal delante del enemigo donde se comprueba si hay suelo")]
    public float distanciaSensorBordeX = 0.48f;

    [Tooltip("Longitud hacia abajo del rayo que verifica el suelo en el borde")]
    public float longitudRayoSuelo = 1.15f;

    [Tooltip("Distancia frontal para detectar paredes, cajas o barriles y girar")]
    public float distanciaSensorPared = 0.52f;

    [Tooltip("Capas consideradas como suelo/obstáculo (por defecto Suelo y Default)")]
    public LayerMask capaSuelo;

    [Header("Detección y Embestida (Modo Furia)")]
    [Tooltip("Distancia horizontal a la que detecta al jugador para entrar en modo furia")]
    public float rangoDeteccion = 6.8f;

    [Tooltip("Diferencia máxima de altura (Y) para detectar y embestir al jugador")]
    public float diferenciaAlturaMaxima = 2.1f;

    [Tooltip("Velocidad de carrera cuando entra en modo furia y embiste al jugador")]
    public float velocidadSprint = 5.4f;

    [Tooltip("Nombre del parámetro booleano en el Animator al correr")]
    public string parametroAnimatorCorriendo = "EstaCorriendo";

    [Header("Combate Cuerpo a Cuerpo y Daño por Salto (Stomp)")]
    [Tooltip("Daño cuerpo a cuerpo al chocar lateralmente con el jugador")]
    public float danoContacto = 22f;

    [Tooltip("Tiempo mínimo entre golpes cuerpo a cuerpo para no matar instantáneamente al jugador")]
    public float tiempoEntreGolpes = 0.90f;

    [Tooltip("Empuje horizontal aplicado al jugador al ser embestido")]
    public float fuerzaEmpujeHorizontal = 6.5f;

    [Tooltip("Impulso vertical aplicado al jugador al ser embestido")]
    public float fuerzaEmpujeVertical = 4.2f;

    [Tooltip("Permite que el jugador lo dañe/elimine saltándole encima")]
    public bool recibirDanoAlSaltarEncima = true;

    [Tooltip("Daño que recibe Cab. Sau cuando el jugador salta sobre su cabeza")]
    public int danoPorSaltoEncima = 2;

    [Tooltip("Impulso de rebote hacia arriba que recibe el jugador al pisarlo")]
    public float fuerzaReboteJugador = 10.2f;

    [Header("Audio (SFX de Cab. Sau)")]
    public AudioSource audioSource;

    [Tooltip("Sonido cuando detecta al jugador y entra en modo furia/embestida")]
    public AudioClip sonidoDetectarJugador;

    [Tooltip("Sonido cuando recibe daño por corcho o pisotón")]
    public AudioClip sonidoRecibirDano;

    [Tooltip("Sonido cuando es derrotado y explota en jugo rojo rubí")]
    public AudioClip sonidoMuerte;

    [Range(0f, 1f)]
    public float volumenAudio = 0.88f;

    [Header("Sprites de Animación (Cab. Sau 1 al 13)")]
    [Tooltip("Sprites de patrulla caminando (cabsau_5, 6, 8, 12, 10, 4)")]
    public Sprite[] spritesPatrulla;

    [Tooltip("Sprite de alerta/furia al detectar al jugador (cabsau_1)")]
    public Sprite spriteAlertaFuria;

    [Tooltip("Sprites de sprint/embestida con boca abierta (cabsau_2, 3, 7, 9, 11, 13)")]
    public Sprite[] spritesSprint;

    [Tooltip("Velocidad de cuadros por segundo durante la patrulla")]
    public float fpsPatrulla = 7f;

    [Tooltip("Velocidad de cuadros por segundo durante la embestida en sprint")]
    public float fpsSprint = 14f;

    [Header("Gizmos en Editor")]
    public bool mostrarGizmosSiempre = true;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator animator;
    private Transform playerTransform;

    private Vector3 posicionInicial;
    private Vector3 escalaBase;
    private float limiteIzquierdo;
    private float limiteDerecho;

    private int direccionMovimiento = 1; // 1 = derecha, -1 = izquierda
    private bool estaCorriendo = false;
    private bool estaHerido = false;
    private bool estaMuerto = false;

    private float proximoGolpePermitido = 0f;
    private float proximoSonidoAlerta = 0f;
    private float temporizadorPausaBorde = 0f;
    private float temporizadorAnim = 0f;

    public bool EstaCorriendo => estaCorriendo;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (rb != null)
        {
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        int sueloLayer = LayerMask.NameToLayer("Suelo");
        if (capaSuelo.value == 0)
        {
            if (sueloLayer >= 0)
                capaSuelo = 1 << sueloLayer;
            else
                capaSuelo = ~0;
        }

        if (detectorBorde == null)
        {
            Transform child = transform.Find("DetectorBorde");
            if (child == null)
            {
                GameObject dbObj = new GameObject("DetectorBorde");
                dbObj.transform.SetParent(transform);
                dbObj.transform.localPosition = new Vector3(distanciaSensorBordeX, -0.18f, 0f);
                child = dbObj.transform;
            }
            detectorBorde = child;
        }

        escalaBase = transform.localScale;
        posicionInicial = transform.position;
        CalcularLimitesPatrulla();
    }

    private void Start()
    {
        posicionInicial = transform.position;
        CalcularLimitesPatrulla();
        BuscarJugador();
    }

    private void CalcularLimitesPatrulla()
    {
        if (puntoPatrullaA != null && puntoPatrullaB != null)
        {
            limiteIzquierdo = Mathf.Min(puntoPatrullaA.position.x, puntoPatrullaB.position.x);
            limiteDerecho = Mathf.Max(puntoPatrullaA.position.x, puntoPatrullaB.position.x);
        }
        else
        {
            limiteIzquierdo = posicionInicial.x - Mathf.Abs(distanciaPatrulla);
            limiteDerecho = posicionInicial.x + Mathf.Abs(distanciaPatrulla);
        }
    }

    private void BuscarJugador()
    {
        if (playerTransform != null) return;

        MovimientoJugador mov = FindFirstObjectByType<MovimientoJugador>();
        if (mov != null)
        {
            playerTransform = mov.transform;
            if (mov.GetComponent<HealthSystem>() == null)
            {
                mov.gameObject.AddComponent<HealthSystem>();
            }
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private void Update()
    {
        if (estaMuerto) return;

        if (playerTransform == null)
            BuscarJugador();

        CalcularLimitesPatrulla();

        bool jugadorDetectado = VerificarDeteccionJugador(out float deltaXJugador);

        if (jugadorDetectado && !estaHerido)
        {
            if (!estaCorriendo)
            {
                ActivarModoFuria();
            }

            if (Mathf.Abs(deltaXJugador) > 0.12f)
            {
                direccionMovimiento = deltaXJugador > 0f ? 1 : -1;
            }
        }
        else
        {
            if (estaCorriendo)
            {
                DesactivarModoFuria();
            }
        }

        ActualizarOrientacionVisual();
        ActualizarAnimacionSprites();
    }

    private void FixedUpdate()
    {
        if (estaMuerto || estaHerido || rb == null)
            return;

        if (temporizadorPausaBorde > 0f)
        {
            temporizadorPausaBorde -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        bool hayParedAlFrente = DetectarParedAlFrente();
        bool faltaSueloAlFrente = detectarBordes && !DetectarSueloAlFrente();

        if (estaCorriendo)
        {
            // En modo furia/embestida corre hacia el jugador, pero respeta precipicios si está configurado
            if (hayParedAlFrente || (frenarEnBordesDuranteSprint && faltaSueloAlFrente))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            rb.linearVelocity = new Vector2(direccionMovimiento * velocidadSprint, rb.linearVelocity.y);
        }
        else
        {
            // En modo patrulla gira al llegar a los puntos A/B, a un borde de plataforma o a una pared/barril
            bool fueraDeRangoIzquierdo = direccionMovimiento < 0 && transform.position.x <= limiteIzquierdo;
            bool fueraDeRangoDerecho = direccionMovimiento > 0 && transform.position.x >= limiteDerecho;

            if (fueraDeRangoIzquierdo || fueraDeRangoDerecho || hayParedAlFrente || faltaSueloAlFrente)
            {
                direccionMovimiento *= -1;
                temporizadorPausaBorde = 0.12f;
                ActualizarOrientacionVisual();
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            rb.linearVelocity = new Vector2(direccionMovimiento * velocidadPatrulla, rb.linearVelocity.y);
        }
    }

    private bool VerificarDeteccionJugador(out float deltaX)
    {
        deltaX = 0f;
        if (playerTransform == null)
            return false;

        deltaX = playerTransform.position.x - transform.position.x;
        float deltaY = Mathf.Abs(playerTransform.position.y - transform.position.y);

        return Mathf.Abs(deltaX) <= rangoDeteccion && deltaY <= diferenciaAlturaMaxima;
    }

    private void ActivarModoFuria()
    {
        estaCorriendo = true;
        SincronizarAnimatorCorriendo(true);

        if (sonidoDetectarJugador != null && Time.time >= proximoSonidoAlerta)
        {
            proximoSonidoAlerta = Time.time + 2.2f;
            if (audioSource != null)
                audioSource.PlayOneShot(sonidoDetectarJugador, volumenAudio);
            else
                CorazonVida.ReproducirSonidoPersistente(sonidoDetectarJugador, transform.position, volumenAudio);
        }
    }

    private void DesactivarModoFuria()
    {
        estaCorriendo = false;
        SincronizarAnimatorCorriendo(false);
    }

    private void SincronizarAnimatorCorriendo(bool corriendo)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.SetBool(EstaCorriendoHash, corriendo);
        if (!string.IsNullOrEmpty(parametroAnimatorCorriendo) && parametroAnimatorCorriendo != "EstaCorriendo")
        {
            animator.SetBool(parametroAnimatorCorriendo, corriendo);
        }
    }

    private bool DetectarSueloAlFrente()
    {
        Vector2 origen = ObtenerOrigenSensorBorde();
        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, Vector2.down, longitudRayoSuelo, capaSuelo);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCol = hits[i].collider;
            if (hitCol == null || hitCol == col || hitCol.isTrigger)
                continue;

            if (hitCol.CompareTag("Player") || hitCol.GetComponent<EnemigoCabSau>() != null || hitCol.GetComponent<MerlotGrapeEnemy>() != null)
                continue;

            return true;
        }

        return false;
    }

    private bool DetectarParedAlFrente()
    {
        Vector2 origen = (Vector2)transform.position + new Vector2(0f, 0.05f);
        Vector2 dir = new Vector2(direccionMovimiento, 0f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, dir, distanciaSensorPared, capaSuelo);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCol = hits[i].collider;
            if (hitCol == null || hitCol == col || hitCol.isTrigger)
                continue;

            if (hitCol.CompareTag("Player") || hitCol.GetComponent<CorkProjectile>() != null)
                continue;

            // Si choca con pared, caja, barril u otro enemigo, gira
            if (Mathf.Abs(hits[i].normal.x) > 0.55f)
                return true;
        }

        return false;
    }

    private Vector2 ObtenerOrigenSensorBorde()
    {
        if (detectorBorde != null)
        {
            detectorBorde.localPosition = new Vector3(
                direccionMovimiento * Mathf.Abs(distanciaSensorBordeX),
                -0.18f,
                0f
            );
            return detectorBorde.position;
        }

        float offsetY = col != null ? -col.bounds.extents.y * 0.45f : -0.15f;
        return new Vector2(
            transform.position.x + (direccionMovimiento * distanciaSensorBordeX),
            transform.position.y + offsetY
        );
    }

    private void ActualizarOrientacionVisual()
    {
        if (spriteRenderer == null) return;

        // Los sprites originales de Cab. Sau miran hacia la DERECHA:
        // flipX = false cuando va a la derecha (1), flipX = true cuando va a la izquierda (-1)
        spriteRenderer.flipX = direccionMovimiento < 0;

        if (detectorBorde != null)
        {
            detectorBorde.localPosition = new Vector3(
                direccionMovimiento * Mathf.Abs(distanciaSensorBordeX),
                -0.18f,
                0f
            );
        }
    }

    private void ActualizarAnimacionSprites()
    {
        if (estaHerido || spriteRenderer == null)
            return;

        Sprite[] bancoActual = estaCorriendo ? spritesSprint : spritesPatrulla;
        float fps = estaCorriendo ? fpsSprint : fpsPatrulla;

        if (bancoActual == null || bancoActual.Length == 0)
            return;

        temporizadorAnim += Time.deltaTime * fps;
        int idx = Mathf.FloorToInt(temporizadorAnim) % bancoActual.Length;
        if (bancoActual[idx] != null)
        {
            spriteRenderer.sprite = bancoActual[idx];
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (estaMuerto) return;

        currentHealth -= Mathf.Max(1, damageAmount);
        Debug.Log($"[EnemigoCabSau] ¡{name} recibió daño! Vida restante: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            AudioClip clipFinal = sonidoMuerte != null ? sonidoMuerte : sonidoRecibirDano;
            if (clipFinal != null)
            {
                CorazonVida.ReproducirSonidoPersistente(clipFinal, transform.position, volumenAudio);
            }

            StartCoroutine(MorirRoutine());
        }
        else
        {
            if (sonidoRecibirDano != null && audioSource != null)
            {
                audioSource.PlayOneShot(sonidoRecibirDano, volumenAudio);
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(HurtHash);
            }

            // Al recibir un corchazo se gira hacia el jugador y entra en alerta
            if (playerTransform != null)
            {
                float dx = playerTransform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.05f)
                    direccionMovimiento = dx > 0f ? 1 : -1;
            }

            StopAllCoroutines();
            StartCoroutine(ReaccionDanoRoutine());
        }
    }

    public void RecibirDano(int damageAmount)
    {
        TakeDamage(damageAmount);
    }

    private IEnumerator ReaccionDanoRoutine()
    {
        estaHerido = true;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(-direccionMovimiento * 2.4f, 2.2f);
        }

        if (spriteAlertaFuria != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = spriteAlertaFuria;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.45f, 0.45f, 1f);
        }

        transform.localScale = new Vector3(escalaBase.x * 0.84f, escalaBase.y * 1.20f, escalaBase.z);
        yield return new WaitForSeconds(0.14f);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        transform.localScale = escalaBase;
        estaHerido = false;
    }

    private IEnumerator MorirRoutine()
    {
        estaMuerto = true;
        estaCorriendo = false;
        estaHerido = false;

        MetaNivel.RegistrarUvaEliminada(puntosAlDerrotar);

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (spriteAlertaFuria != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = spriteAlertaFuria;
        }

        Vector3 escalaPop = escalaBase * 1.35f;
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaBase, escalaPop, t / 0.12f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(Color.white, new Color(0.95f, 0.15f, 0.30f, 1f), t / 0.12f);
            }
            yield return null;
        }

        t = 0f;
        while (t < 0.10f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaPop, Vector3.zero, t / 0.10f);
            yield return null;
        }

        EnemyDeathFeedback deathFeedback = GetComponent<EnemyDeathFeedback>();
        if (deathFeedback != null)
        {
            deathFeedback.PlayDeathFeedback();
        }
        else
        {
            EnemyDeathFeedback.SpawnRuntimeGrapeJuiceSplash(
                transform.position,
                28,
                new Color(0.76f, 0.08f, 0.22f, 1f),
                new Color(0.95f, 0.24f, 0.36f, 1f)
            );
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ProcesarColisionJugador(collision, esImpactoInicial: true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ProcesarColisionJugador(collision, esImpactoInicial: false);
    }

    private void ProcesarColisionJugador(Collision2D collision, bool esImpactoInicial)
    {
        if (estaMuerto) return;

        GameObject otro = collision.gameObject;
        if (!otro.CompareTag("Player") && otro.GetComponent<MovimientoJugador>() == null)
            return;

        // 1. Verificar si el jugador cayó encima de Cab. Sau (Stomp)
        if (esImpactoInicial && recibirDanoAlSaltarEncima)
        {
            bool pisadoDesdeArriba = false;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.45f)
                {
                    pisadoDesdeArriba = true;
                    break;
                }
            }

            if (!pisadoDesdeArriba && otro.transform.position.y > transform.position.y + 0.42f)
            {
                pisadoDesdeArriba = true;
            }

            if (pisadoDesdeArriba)
            {
                Rigidbody2D playerRb = otro.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, fuerzaReboteJugador);
                }

                proximoGolpePermitido = Time.time + 0.45f;
                TakeDamage(Mathf.Max(1, danoPorSaltoEncima));
                return;
            }
        }

        // 2. Choque lateral cuerpo a cuerpo: infligir daño con enfriamiento y empuje
        if (Time.time < proximoGolpePermitido)
            return;

        proximoGolpePermitido = Time.time + tiempoEntreGolpes;

        HealthSystem hs = otro.GetComponent<HealthSystem>();
        if (hs == null)
            hs = otro.AddComponent<HealthSystem>();

        hs.TakeDamageFromPosition(danoContacto, transform.position);

        // Empuje físico sobre el jugador y pequeño rebote de Cab. Sau para no matarlo instantáneamente
        float dirEmpuje = Mathf.Sign(otro.transform.position.x - transform.position.x);
        if (Mathf.Abs(dirEmpuje) < 0.01f)
            dirEmpuje = direccionMovimiento;

        Rigidbody2D rbJugador = otro.GetComponent<Rigidbody2D>();
        if (rbJugador != null)
        {
            rbJugador.linearVelocity = new Vector2(dirEmpuje * fuerzaEmpujeHorizontal, fuerzaEmpujeVertical);
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(-dirEmpuje * 2.8f, 2.0f);
        }

        temporizadorPausaBorde = 0.28f;
    }

    private void OnGUI()
    {
        if (estaMuerto || currentHealth >= maxHealth || Camera.main == null)
            return;

        Vector3 worldPos = transform.position + new Vector3(0f, 1.05f, 0f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0f) return;

        float guiX = screenPos.x - 24f;
        float guiY = Screen.height - screenPos.y - 8f;

        GUI.color = new Color(0.18f, 0.04f, 0.08f, 0.88f);
        GUI.DrawTexture(new Rect(guiX - 2, guiY - 2, 52, 9), Texture2D.whiteTexture);

        float fill = Mathf.Clamp01((float)currentHealth / maxHealth);
        GUI.color = new Color(0.88f, 0.14f, 0.28f, 1f); // Rojo rubí Cabernet Sauvignon
        GUI.DrawTexture(new Rect(guiX, guiY, 48 * fill, 5), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void OnDrawGizmos()
    {
        if (mostrarGizmosSiempre)
        {
            DibujarGizmosCabSau(seleccionado: false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmosSiempre)
        {
            DibujarGizmosCabSau(seleccionado: true);
        }
        else
        {
            DibujarGizmosCabSau(seleccionado: true);
        }
    }

    private void DibujarGizmosCabSau(bool seleccionado)
    {
        Vector3 centro = Application.isPlaying ? posicionInicial : transform.position;
        float izq = puntoPatrullaA != null ? puntoPatrullaA.position.x : centro.x - Mathf.Abs(distanciaPatrulla);
        float der = puntoPatrullaB != null ? puntoPatrullaB.position.x : centro.x + Mathf.Abs(distanciaPatrulla);

        // 1. Rango de Patrulla (Cian / Verde)
        Gizmos.color = seleccionado ? new Color(0.15f, 0.95f, 0.85f, 0.95f) : new Color(0.15f, 0.95f, 0.85f, 0.55f);
        Vector3 pIzq = new Vector3(izq, transform.position.y, transform.position.z);
        Vector3 pDer = new Vector3(der, transform.position.y, transform.position.z);
        Gizmos.DrawLine(pIzq, pDer);
        Gizmos.DrawWireSphere(pIzq, 0.22f);
        Gizmos.DrawWireSphere(pDer, 0.22f);

        // 2. Caja / Radio de Detección de Embestida (Amarillo en patrulla, Rojo rubí en furia)
        Color colorDeteccion = estaCorriendo
            ? new Color(1.0f, 0.18f, 0.28f, seleccionado ? 0.85f : 0.45f)
            : new Color(1.0f, 0.78f, 0.15f, seleccionado ? 0.75f : 0.35f);

        Gizmos.color = colorDeteccion;
        Vector3 tamanoCajaDeteccion = new Vector3(rangoDeteccion * 2f, diferenciaAlturaMaxima * 2f, 0.1f);
        Gizmos.DrawWireCube(transform.position, tamanoCajaDeteccion);

        if (seleccionado)
        {
            Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        }

        // 3. Sensores de Borde de Plataforma y Pared
        if (detectarBordes)
        {
            Gizmos.color = new Color(0.25f, 1f, 0.35f, 0.9f);
            Vector2 origenBorde = ObtenerOrigenSensorBorde();
            Gizmos.DrawLine(origenBorde, origenBorde + Vector2.down * longitudRayoSuelo);
            Gizmos.DrawWireSphere(origenBorde + Vector2.down * longitudRayoSuelo, 0.08f);
        }
    }
}
