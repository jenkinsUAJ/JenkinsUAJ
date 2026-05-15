using UnityEngine;
using System;

/// <summary>
/// Gestor central de la pausa del juego.
/// Implementa el patrón Singleton para asegurar que solo exista una instancia.
/// Notifica a los suscriptores cada vez que el estado de pausa cambia.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    /// <summary>
    /// Evento estático al que los objetos pueden suscribirse para reaccionar
    /// cuando cambia el estado de pausa.  
    /// El parámetro <c>bool</c> indica el nuevo estado:  
    /// <c>true</c> = pausado, <c>false</c> = reanudado.
    /// </summary>
    public static event Action<bool> OnPauseStateChanged;

    /// <summary>
    /// Indica si el juego está actualmente en pausa.
    /// </summary>
    public bool IsPaused { get; private set; }

    private void Awake() {
        // Patrón Singleton
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Alterna el estado de pausa.  
    /// Si estaba pausado, se reanuda. Si estaba en marcha, se pausa.
    /// </summary>
    public void TogglePause() {
        SetPause(!IsPaused);
    }

    /// <summary>
    /// Establece el estado de pausa explícitamente.
    /// </summary>
    /// <param name="pauseState">Nuevo estado de pausa deseado.</param>
    public void SetPause(bool pauseState) 
    {
        // Evitamos notificar si el estado no ha cambiado.
        if (IsPaused == pauseState) return;

        IsPaused = pauseState;

        OnPauseStateChanged?.Invoke(IsPaused);
    }
}
