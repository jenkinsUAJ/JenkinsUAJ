using UnityEngine;

[CreateAssetMenu(fileName = "NewInstantiatePrefabPerk", menuName = "Perks/Instantiate Prefab Perk")]
public class InstantiatePrefabPerkData : PerkData
{
    [Header("Configuración Instantiate Prefab")]
    [Tooltip("El prefab que se instanciará cuando este perk se active.")]
    [SerializeField] private GameObject perkPrefab;

    /// <summary>
    /// Añade el componente genérico <see cref="InstantiatePrefabBehaviour"/> al jugador
    /// y lo configura con el prefab específico de este asset.
    /// </summary>
    public override PerkBehaviour AttachPerkTo(GameObject player) {
        // Añadimos siempre el mismo tipo de componente
        var behaviour = player.AddComponent<InstantiatePrefabBehaviour>();

        // Pero lo inicializamos con el prefab que hemos definido en el Inspector
        behaviour.Initialize(perkPrefab);

        return behaviour;
    }
}