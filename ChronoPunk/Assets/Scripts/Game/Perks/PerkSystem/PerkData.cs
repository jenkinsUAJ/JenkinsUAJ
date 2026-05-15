using UnityEngine;

/// <summary>
/// ScriptableObject que almacena los datos de configuración de un perk (nombre, icono, parámetros).
/// Sirve como “fabrica” para crear y enganchar el PerkBehaviour al jugador.
/// </summary>
public abstract class PerkData : ScriptableObject
{
    [Header("Info General")]
    [Tooltip("Nombre para mostrar del perk.")]
    public string perkName;

    [Tooltip("Icono que se mostrará en la UI cuando el jugador lo obtenga.")]
    public Sprite perkIcon;

    /// <summary>
    /// Crea y adjunta el componente <see cref="PerkBehaviour"/> correspondiente en el objeto <paramref name="player"/>.
    /// Debe devolver la instancia del componente para almacenar referencia en el controlador.
    /// </summary>
    /// <param name="player">GameObject del jugador al que añadir el comportamiento.</param>
    public abstract PerkBehaviour AttachPerkTo(GameObject player);
}
