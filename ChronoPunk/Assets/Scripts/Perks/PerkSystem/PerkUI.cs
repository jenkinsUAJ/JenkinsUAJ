using UnityEngine;
using UnityEngine.UI;

public class PerkUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Imagen donde se mostrar� el icono del perk.")]
    [SerializeField] private Image perkImageHolder;
    [SerializeField] private Image perkBackGroundImage;

    private PlayerPerkController perkController;

    private void Awake() {
        // Obtiene autom�ticamente el PlayerPerkController en este mismo GameObject
        perkController = GetComponent<PlayerPerkController>();
        if (perkController == null) {
            Debug.LogError("PerkUI requiere un PlayerPerkController en el mismo GameObject.");
        }

        // Aseguramos que la imagen y marco est�n ocultos al inicio
        perkImageHolder.enabled = false;
        perkBackGroundImage.enabled = false;
    }

    private void OnEnable() {
        // Nos suscribimos a los eventos del controlador
        perkController.OnPerkAdded += HandlePerkAdded;
        perkController.OnPerkUsed += HandlePerkRemoved;
    }

    private void OnDisable() {
        // Limpiamos las suscripciones al desactivarnos
        perkController.OnPerkAdded -= HandlePerkAdded;
        perkController.OnPerkUsed -= HandlePerkRemoved;
    }

    /// <summary>
    /// Muestra el icono del perk reci�n a�adido.
    /// </summary>
    /// <param name="data">Datos del perk obtenido.</param>
    private void HandlePerkAdded(PerkData data) {
        if (data.perkIcon != null) {
            perkImageHolder.sprite = data.perkIcon;
            perkImageHolder.enabled = true;
            perkBackGroundImage.enabled = true;
        }
    }

    /// <summary>
    /// Oculta el icono cuando el perk se usa o se descarta.
    /// </summary>
    private void HandlePerkRemoved() {
        perkImageHolder.sprite = null;
        perkImageHolder.enabled = false;
        perkBackGroundImage.enabled = false;
    }

    /// <summary>
    /// Oculta manualmente el icono del perk (por ejemplo, durante previews temporales).
    /// </summary>
    public void HidePerkImage()
    {
        HandlePerkRemoved();
    }
}
