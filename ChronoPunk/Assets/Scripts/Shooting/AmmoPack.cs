using UnityEngine;

/// <summary>
/// Componente para paquetes de munición que pueden ser recogidos por entidades con el componente Shoot.
/// Solo funciona si la entidad puede recoger munición (CanCollectAmmo = true).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AmmoPack : MonoBehaviour
{
    [Header("Configuración del Paquete")]
    [Tooltip("Cantidad de munición que otorga este paquete")]
    [SerializeField] private int ammoAmount = 10;

    [Tooltip("¿Se destruye inmediatamente al ser recogido?")]
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Efectos Visuales (Opcional)")]
    [Tooltip("Efecto visual a reproducir cuando se recoge (opcional)")]
    [SerializeField] private GameObject pickupEffect;

    [Header("Audio (Opcional)")]
    [Tooltip("¿Reproducir sonido al recoger?")]
    [SerializeField] private bool playPickupSound = true;

    private AudioSource audioSource;
    private bool hasBeenPickedUp = false;

    private void Awake()
    {
        // Configurar el trigger del collider
        var collider = GetComponent<Collider2D>();
        collider.isTrigger = true;

        // Obtener AudioSource si existe
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Evitar múltiples activaciones
        if (hasBeenPickedUp) return;

        // Intentar obtener el componente Shoot del objeto que colisiona
        Shoot shootComponent = other.GetComponent<Shoot>();
        if (shootComponent == null) return;

        // Verificar si puede recoger munición
        if (!shootComponent.CanCollectAmmo) return;

        // Verificar si tiene munición infinita (no necesita más munición)
        if (shootComponent.HasInfiniteAmmo) return;

        // Intentar añadir munición
        int ammoAdded = shootComponent.AddAmmo(ammoAmount);

        if (ammoAdded > 0)
        {
            hasBeenPickedUp = true;

            GetComponent<PickUpAudio>().PlayPickUp();

            // Reproducir efectos
            PlayPickupEffects();

            // Notificar a la UI para actualización inmediata
            other.GetComponentInChildren<AmmoUI>().ForceUpdate();

            // Manejar destrucción o desactivación
            if (destroyOnPickup)
            {
                // Si tiene audio, esperar a que termine antes de destruir
                if (playPickupSound && audioSource != null && audioSource.clip != null)
                {
                    // Desvincular del parent para que el audio se reproduzca completamente
                    transform.SetParent(null);
                    // Destruir el objeto visual pero mantener el audio
                    var renderer = GetComponent<Renderer>();
                    var collider = GetComponent<Collider2D>();
                    if (renderer != null) renderer.enabled = false;
                    if (collider != null) collider.enabled = false;

                    Destroy(gameObject, audioSource.clip.length);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                gameObject.SetActive(false);
            }

            Debug.Log($"Paquete de munición recogido: +{ammoAdded} munición para {other.name}");
        }
    }

    private void PlayPickupEffects()
    {
        // Efecto visual
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, transform.rotation);
        }

        
    }

    /// <summary>
    /// Establece la cantidad de munición que otorga este paquete
    /// </summary>
    /// <param name="amount">Nueva cantidad de munición</param>
    public void SetAmmoAmount(int amount)
    {
        ammoAmount = Mathf.Max(1, amount);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ammoAmount <= 0)
        {
            ammoAmount = 1;
            Debug.LogWarning($"AmmoPack en {gameObject.name}: La cantidad de munición debe ser mayor que 0.");
        }
    }
#endif
}
