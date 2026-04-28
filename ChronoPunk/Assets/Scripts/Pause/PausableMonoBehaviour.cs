using UnityEngine;

/// <summary>
/// Clase base abstracta para cualquier objeto que deba reaccionar
/// al sistema de pausa del juego.  
/// Hereda de <c>MonoBehaviour</c> para poder usarse como componente de Unity.  
/// Se suscribe automáticamente al <see cref="PauseManager"/> para recibir notificaciones.
/// </summary>
public abstract class PausableMonoBehaviour : MonoBehaviour
{
    /// <summary>
    /// Indica si el objeto está actualmente en pausa.
    /// Actualizado automáticamente por el <see cref="PauseManager"/>.
    /// </summary>
    public bool IsPaused { get; private set; }


    protected virtual void OnEnable() {
        // Nos suscribimos al evento global de pausa.
        PauseManager.OnPauseStateChanged += SetPaused;
        // Aplicamos inmediatamente el estado de pausa actual,
        // para evitar que el objeto arranque en un estado incorrecto.
        if (PauseManager.Instance != null) {
            SetPaused(PauseManager.Instance.IsPaused);
        }
    }

    protected virtual void OnDisable() {
        PauseManager.OnPauseStateChanged -= SetPaused;
    }

    protected virtual void OnDestroy() {
        // Nos desuscribimos del evento global para evitar leaks
        PauseManager.OnPauseStateChanged -= SetPaused;
    }

    /// <summary>
    /// Método llamado automáticamente por el <see cref="PauseManager"/> cuando cambia el estado de pausa.
    /// Es virtual para que las clases hijas puedan sobrescribirlo si necesitan lógica adicional.
    /// </summary>
    /// <param name="isPaused">Nuevo estado de pausa (<c>true</c> = pausado).</param>
    public virtual void SetPaused(bool isPaused) {
        // Guardamos el nuevo estado en la propiedad pública.
        this.IsPaused = isPaused;
    }
}
