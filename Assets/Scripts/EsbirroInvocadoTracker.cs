using UnityEngine;

/// <summary>
/// Componente ligero añadido a cada uva enemiga (Merlot, Cab. Sau o Nebbiolo) invocada por el Wine Monster.
/// Al ser derrotada en combate, notifica al jefe para el conteo de 6 enemigos -> aparición de Corazón curativo.
/// </summary>
public class EsbirroInvocadoTracker : MonoBehaviour
{
    private bool yaReportado = false;

    private void OnDestroy()
    {
        if (yaReportado) return;
        yaReportado = true;

        // Ignorar destrucciones al descargar escena o durante limpieza al derrotar al jefe
        if (!gameObject.scene.isLoaded) return;

        JefeWineMonster boss = JefeWineMonster.Instancia;
        if (boss == null || boss.EstaDerrotado || boss.LimpiandoEsbirros) return;

        boss.NotificarEsbirroDerrotado(transform.position);
    }
}
