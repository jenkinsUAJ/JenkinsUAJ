using UnityEngine;

/// <summary>
/// Clase base para todos los comportamientos de perks que se añaden al jugador.
/// Define el contrato mínimo: un perk debe implementar su efecto en <see cref="ActivateEffect"/>.
/// </summary>
public abstract class PerkBehaviour : MonoBehaviour
{
    /// <summary>
    /// Se llama cuando el jugador activa el perk (por ejemplo, al pulsar un botón).
    /// Cada clase concreta debe implementar su propio efecto aquí.
    /// </summary>
    public abstract void ActivateEffect();
}