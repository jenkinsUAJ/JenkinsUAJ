using UnityEngine;

public class Solidify : MonoBehaviour
{

    [SerializeField] private bool featureEnabled = true;
    [SerializeField] private string solidifiedLayerName = "SombraSolidified";

    private CapsuleCollider2D originalCapsuleCollider;
    private BoxCollider2D solidifiedBoxCollider;
    private Animator[] shadowAnimators;

    private void Awake()
    {
        originalCapsuleCollider = GetComponent<CapsuleCollider2D>();
        shadowAnimators = GetComponentsInChildren<Animator>(true);
    }

    public void ActivateSolidification()
    {
        if (!featureEnabled) return;

        // Si la sombra está en un globo al solidificarse, expulsarla sin salto
        BalloonUser balloonUser = GetComponent<BalloonUser>();
        if (balloonUser != null && balloonUser.IsOnBalloon)
        {
            balloonUser.EjectFromBalloon(false);
        }

        // Cambiar la layer para solidificarse y colisionar con otras sombras y el player
        gameObject.layer = LayerMask.NameToLayer(solidifiedLayerName);

        StopShadowVisualAnimation();

        // Cambiar de CapsuleCollider a BoxCollider
        if (originalCapsuleCollider != null && solidifiedBoxCollider == null)
        {
            // Crear el BoxCollider con las propiedades del CapsuleCollider
            solidifiedBoxCollider = gameObject.AddComponent<BoxCollider2D>();
            
            // Trasladar propiedades comunes
            solidifiedBoxCollider.offset = originalCapsuleCollider.offset;
            solidifiedBoxCollider.isTrigger = originalCapsuleCollider.isTrigger;
            solidifiedBoxCollider.usedByEffector = originalCapsuleCollider.usedByEffector;
            solidifiedBoxCollider.usedByComposite = originalCapsuleCollider.usedByComposite;
            
            // Ajustar el tamaño y offset del box basándose en el capsule
            solidifiedBoxCollider.size = originalCapsuleCollider.size;
            solidifiedBoxCollider.offset = originalCapsuleCollider.offset;
            
            // Desactivar el CapsuleCollider original
            originalCapsuleCollider.enabled = false;
        }

        GetComponent<PlayerAudio>().PlayHarden();
    }

    private void StopShadowVisualAnimation()
    {
        if (shadowAnimators != null)
        {
            foreach (Animator animator in shadowAnimators)
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }
            }
        }
    }
}
