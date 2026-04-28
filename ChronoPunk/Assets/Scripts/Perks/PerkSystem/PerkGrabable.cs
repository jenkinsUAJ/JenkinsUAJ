using UnityEngine;

/// <summary>
/// Componente que, al colisionar con el jugador, le otorga el perk asociado y desactiva el objeto en la escena.
/// </summary>
public class PerkGrabable : MonoBehaviour
{
    [Tooltip("Arrastra aquí el ScriptableObject del perk que quieres que este objeto otorgue.")]
    [SerializeField] private PerkData perkData;

    private void OnTriggerEnter2D(Collider2D collision) {

        // Intentamos obtener el controlador de perks del jugador
        PlayerPerkController perkController = collision.GetComponent<PlayerPerkController>();
        if (perkController == null)
            return;

        // Si el jugador no tiene ya un perk, lo añadimos y desactivamos este objeto
        if (perkController.TryAddPerk(perkData)) {
            GetComponent<PickUpAudio>().PlayPickUp();
            gameObject.SetActive(false);   
        }
    }
}