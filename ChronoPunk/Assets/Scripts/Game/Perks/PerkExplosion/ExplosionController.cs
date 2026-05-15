using UnityEngine;

/// <summary>
/// Orquesta los efectos de una explosión (animación, sonido) y gestiona su ciclo de vida.
/// No contiene lógica de daño; delega esa tarea a los componentes DamageDealer y ContactDamageHandler.
/// Este script debe ir en el prefab de la explosión.
/// </summary>
[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class ExplosionController : MonoBehaviour
{
    [Header("Efectos")]
    [Tooltip("Clip de sonido para la explosión.")]
    [SerializeField] private AudioClip explosionSound;

    private AudioSource audioSource;

    private void Awake() {
        // Obtenemos referencias a los componentes que vamos a dirigir.
        audioSource = GetComponent<AudioSource>();
    }

    private void Start() {
        // En cuanto la explosión es instanciada:
        // 1. Reproducimos el sonido de la explosión.
        if (explosionSound != null) {
            // PlayOneShot permite reproducir sonidos incluso si el objeto se destruye.
            audioSource.PlayOneShot(explosionSound);
        }
    }

    /// <summary>
    /// Este método público está diseñado para ser llamado por un Evento de Animación
    /// al final del clip de la animación de explosión.
    /// </summary>
    public void SelfDestruct() {
        // Una vez la animación ha terminado, destruimos el objeto.
        Destroy(gameObject);
    }
}