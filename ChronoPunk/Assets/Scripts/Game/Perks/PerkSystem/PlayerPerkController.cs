using UnityEngine;
using System;

/// <summary>
/// Controla qu� perk tiene actualmente el jugador y coordina su adquisici�n y activaci�n.
/// </summary>
public class PlayerPerkController : MonoBehaviour
{
    // Referencia al componente que implementa el comportamiento del perk
    private PerkBehaviour currentPerkBehaviour;

    // Datos asociados al perk actual (para mostrar nombre, icono, etc. en UI)
    private PerkData currentPerkData;

    /// <summary>
    /// Se lanza justo despu�s de a�adir un perk al jugador.
    /// </summary>
    public event Action<PerkData> OnPerkAdded;

    /// <summary>
    /// Se lanza justo despu�s de usar (o descartar) el perk actual.
    /// </summary>
    public event Action OnPerkUsed;

    /// <summary>
    /// Comprueba si el jugador ya tiene un perk activo.
    /// </summary>
    /// <returns>True si <see cref="currentPerkBehaviour"/> no es null.</returns>
    public bool HasPerk() => currentPerkBehaviour != null;

    /// <summary>
    /// Copia el perk actual a otro controlador, si existe y es compatible.
    /// </summary>
    /// <returns>True si se pudo copiar; false en caso contrario.</returns>
    public bool TryCopyPerkTo(PlayerPerkController target)
    {
        if (target == null || !HasPerk()) return false;
        return target.TryAddPerk(currentPerkData);
    }

    /// <summary>
    /// Intenta a�adir un nuevo perk al jugador.
    /// Genera <see cref="OnPerkAdded"/> si tiene �xito.
    /// </summary>
    /// <param name="perkData">Datos del perk a a�adir.</param>
    /// <returns>True si se a�adi� correctamente; false si ya hab�a uno.</returns>
    public bool TryAddPerk(PerkData perkData) 
    {
        if (HasPerk()) {
            Debug.LogWarning("El jugador ya tiene un perk. No se puede a�adir otro.");
            return false;
        }

        // Guardamos la data y creamos el componente de comportamiento
        currentPerkData = perkData;
        currentPerkBehaviour = perkData.AttachPerkTo(this.gameObject);

        // Disparamos evento para quien se suscriba
        OnPerkAdded?.Invoke(currentPerkData);

        Debug.Log($"Perk {currentPerkData.perkName} a�adido al jugador.");
        return true;
    }

    /// <summary>
    /// Llama al efecto del perk actual.
    /// Genera <see cref="OnPerkUsed"/> si tiene �xito.
    /// </summary>
    public void UsePerk()
    {
        if (!HasPerk()) return;

        // Activamos el efecto concreto
        currentPerkBehaviour.ActivateEffect();

        // Disparamos evento para quien se suscriba
        OnPerkUsed?.Invoke();

        Destroy(gameObject);
    }
}
