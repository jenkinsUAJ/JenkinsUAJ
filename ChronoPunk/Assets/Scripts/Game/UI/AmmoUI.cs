using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

/// <summary>
/// Componente UI que muestra la munición actual del jugador.
/// Se actualiza automáticamente cuando cambia la munición.
/// </summary>
public class AmmoUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Texto que muestra la munición actual")]
    [SerializeField] private TextMeshProUGUI ammoText;

    [Tooltip("La imagen de bala que acompaña al texto")]
    [SerializeField] private Image bulletImage;

    [Tooltip("Imagen de fondo del indicador de munición (opcional)")]
    [SerializeField] private Image ammoBackgroundImage;

    [Header("Configuración Visual")]
    [Tooltip("Color cuando la munición está en cantidad normal")]
    [SerializeField] private Color normalAmmoColor = Color.white;

    [Tooltip("Color cuando la munición está baja")]
    [SerializeField] private Color lowAmmoColor = Color.red;

    [Tooltip("Cantidad considerada como munición baja")]
    [SerializeField] private int lowAmmoThreshold = 5;

    [Header("Efectos")]
    [Tooltip("¿Animar cuando la munición está baja?")]
    [SerializeField] private bool animateWhenLow = true;

    [Tooltip("Velocidad de parpadeo cuando la munición está baja")]
    [SerializeField] private float blinkSpeed = 2f;

    private Shoot playerShootComponent;
    private bool isBlinking = false;
    private float blinkTimer = 0f;

    private void Start()
    {
        // Buscar el componente Shoot del jugador
        FindShootComponent();
    }

    private void Update()
    {
        if (playerShootComponent == null)
        {
            FindShootComponent();
            return;
        }

        // Actualizar la UI
        UpdateAmmoDisplay();

        // Manejar animación de parpadeo si es necesario
        if (animateWhenLow && isBlinking)
        {
            HandleBlinkAnimation();
        }
    }

    private void FindShootComponent()
    {
        GameObject shooter = GetComponentInParent<Shoot>().gameObject;
        // Buscar el jugador por tag o por componente específico
        if (shooter == null)
        {
            // Alternativa: buscar por componente PlayerMovementKinematic
            var playerMovement = FindFirstObjectByType<Cronopunk.Movement.PlayerMovementKinematic>();
            if (playerMovement != null)
            {
                shooter = playerMovement.gameObject;
            }
        }

        if (shooter != null)
        {
            playerShootComponent = shooter.GetComponent<Shoot>();
            if (playerShootComponent != null)
            {
                SetUIVisible(!playerShootComponent.HasInfiniteAmmo);
            }
        }
    }

    private void UpdateAmmoDisplay()
    {
        if (playerShootComponent == null) return;

        // Si tiene munición infinita, ocultar la UI
        if (playerShootComponent.HasInfiniteAmmo || playerShootComponent.CurrentAmmo <= 0)
        {
            SetUIVisible(false);
            return;
        }

        SetUIVisible(true);

        int currentAmmo = playerShootComponent.CurrentAmmo;

        // Actualizar texto
        if (ammoText != null)
        {
            ammoText.text = currentAmmo.ToString();
        }

        
        // Determinar color basado en la cantidad
        Color targetColor = GetAmmoColor(currentAmmo);

        // Aplicar color a los elementos UI
        if (ammoText != null)
        {
            ammoText.color = targetColor;
        }
        

        // Manejar animación de munición baja
        bool shouldBlink = currentAmmo <= lowAmmoThreshold && currentAmmo > 0;
        if (shouldBlink != isBlinking)
        {
            isBlinking = shouldBlink;
            blinkTimer = 0f;
        }

        // Ocultar la UI si no tenemos munición
        if(currentAmmo <= 0)
        {
        
        }
    }

    private Color GetAmmoColor(int currentAmmo)
    {
        if (currentAmmo <= lowAmmoThreshold)
        {
            return lowAmmoColor;
        }
        else
        {
            return normalAmmoColor;
        }
    }

    private void HandleBlinkAnimation()
    {
        blinkTimer += Time.deltaTime * blinkSpeed;
        float alpha = (Mathf.Sin(blinkTimer) + 1f) * 0.5f; // Oscillate between 0 and 1

        if (ammoText != null)
        {
            Color textColor = ammoText.color;
            textColor.a = Mathf.Lerp(0.3f, 1f, alpha);
            ammoText.color = textColor;
        }
    }
    /// <summary>
    /// Hace invisibles los elementos UI de munición.
    /// </summary>
    /// <param name="visible"></param>
    private void SetUIVisible(bool visible)
    {
        ammoBackgroundImage.gameObject.SetActive(visible);
        ammoText.gameObject.SetActive(visible);
        bulletImage.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Fuerza una actualización inmediata de la UI (útil cuando se recoge munición)
    /// </summary>
    public void ForceUpdate()
    {
        if (playerShootComponent != null)
        {
            UpdateAmmoDisplay();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        lowAmmoThreshold = Mathf.Max(0, lowAmmoThreshold);
        blinkSpeed = Mathf.Max(0.1f, blinkSpeed);
    }
#endif
}
