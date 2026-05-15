using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RockImpactHandler : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayers;
    private BoxCollider2D rockCollider;
    private bool initialOverlapsResolved = false;
    
    private void Start()
    {
        rockCollider = GetComponent<BoxCollider2D>();
        ResolveInitialOverlaps();
        
        initialOverlapsResolved = true;
    }
    
    private void ResolveInitialOverlaps()
    {
        Vector2 totalSeparation = Vector2.zero;
        
        // Detectar todos los colliders que están overlapeando con la roca
        Collider2D[] overlappingColliders = GetOverlapsWithGround();
        
        // Para cada collider detectado, calcular la penetración
        foreach (Collider2D groundCollider in overlappingColliders)
        {
            if (groundCollider == rockCollider) continue; // Ignorar el propio collider
            
            // Calcular la distancia entre los colliders
            ColliderDistance2D colliderDistance = rockCollider.Distance(groundCollider);
            
            // Si están overlapeando (distancia negativa), separar
            if (colliderDistance.distance < 0)
            {
                // La normal apunta desde el ground hacia la roca (dirección de separación)
                // La distancia es negativa cuando hay overlap, así que invertimos el signo
                totalSeparation += colliderDistance.normal * Mathf.Abs(colliderDistance.distance);
            }
        }
        
        // Aplicar el desplazamiento total a la roca
        transform.position -= (Vector3)totalSeparation;
    }
    
    private Collider2D[] GetOverlapsWithGround()
    {
        return Physics2D.OverlapBoxAll(
            rockCollider.bounds.center,
            rockCollider.bounds.size,
            0f,
            groundLayers
        );
    }
    
    private void OnTriggerEnter2D(Collider2D collision) {
        // Solo destruir después de haber resuelto los overlaps iniciales
        if (!initialOverlapsResolved) return;
        
        if (((1 << collision.gameObject.layer) & groundLayers) != 0) {
            Destroy(gameObject);
        }
    }
}
