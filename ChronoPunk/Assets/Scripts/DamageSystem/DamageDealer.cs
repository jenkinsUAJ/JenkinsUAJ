// DamageDealer.cs
using UnityEngine;
using static Unity.VisualScripting.Member;

[DisallowMultipleComponent]
public class DamageDealer : MonoBehaviour
{
    [Tooltip("Cantidad de da�o que inflige.")]
    public int damageAmount = 1;

    [Tooltip("Capas con las que este DamageDealer puede da�ar (filtrado).")]
    [SerializeField] private LayerMask _damageableLayers = ~0; // por defecto todo

    [Tooltip("Si true: al impactar destruye el objeto que hace da�o (�til para balas).")]
    public bool destroySelfOnHit = true;

    [Tooltip("Fuente real del da�o. Si es null se usar� this.gameObject como fallback.")]
    [HideInInspector] public GameObject damageSource = null;

    public void SetDamageableLayers(LayerMask layers)
    {
        _damageableLayers = layers;
    }

    /// <summary>
    /// Determina si el target con HealthSystem debe ser da�ado bas�ndose en la layer.
    /// </summary>
    public bool CanDamage(HealthSystem target) {
        if (target == null) return false;

        // Comprueba layer del target con damageableLayers
        if (((1 << target.gameObject.layer) & _damageableLayers) == 0)
            return false;

        // Evitar self-damage: usamos damageSource si existe, sino fallback a este objeto
        GameObject sourceToCheck = damageSource != null ? damageSource : gameObject;
        if (sourceToCheck != null && sourceToCheck.transform.root == target.transform.root)
            return false;

        return true;
    }
}
