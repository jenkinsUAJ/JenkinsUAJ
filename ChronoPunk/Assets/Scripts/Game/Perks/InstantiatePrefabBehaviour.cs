using Cronopunk.Movement;
using UnityEngine;

/// <summary>
/// Comportamiento gen�rico para perks que simplemente instancian un prefab al activarse.
/// Sirve para rocas, explosiones, power-ups, etc.
/// </summary>
public class InstantiatePrefabBehaviour : PerkBehaviour
{
    // El prefab que se va a instanciar. Puede ser cualquier cosa.
    private GameObject prefabToInstantiate;

    /// <summary>
    /// M�todo de inicializaci�n para que el PerkData le pase el prefab espec�fico.
    /// </summary>
    /// <param name="prefab">El GameObject que se debe instanciar.</param>
    public void Initialize(GameObject prefab) {
        this.prefabToInstantiate = prefab;
    }

    /// <summary>
    /// Crea una instancia del prefab guardado en la posici�n actual del jugador.
    /// </summary>
    public override void ActivateEffect() {
        if (prefabToInstantiate != null) {
            KinematicMover mover = GetComponent<KinematicMover>();
            Instantiate(prefabToInstantiate, mover.Position, Quaternion.identity);
        } else {
            Debug.LogError("El prefab a instanciar no fue asignado. Aseg�rate de llamar a Initialize().");
        }
    }
}
