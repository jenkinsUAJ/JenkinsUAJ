using UnityEngine;
using System.Collections.Generic;
using Cronopunk.Movement;

[RequireComponent(typeof(Collider2D), typeof(MovingPlatformController))]
public class PlatformPassengerHandler : MonoBehaviour
{
    [SerializeField] private LayerMask solidifiedShadowLayer;
    [SerializeField] private LayerMask playerLayers;
    
    private readonly HashSet<KinematicMover> passengers = new HashSet<KinematicMover>();
    private MovingPlatformController platformController;

    private void Awake() {
        platformController = GetComponent<MovingPlatformController>();
        if (platformController != null) {
            platformController.OnPlatformMoved += OnPlatformMoved;
        }
    }

    private void OnDestroy() {
        if (platformController != null) {
            platformController.OnPlatformMoved -= OnPlatformMoved;
        }
    }

    private void OnPlatformMoved(Vector2 delta) {
        if (delta == Vector2.zero) return;

        HashSet<KinematicMover> allPassengersToMove = new HashSet<KinematicMover>(passengers);

        // Mover pasajeros directos
        foreach (KinematicMover passenger in passengers) {
            if (passenger != null) {
                passenger.AddMovement(delta);
                
                // Si es una sombra solidificada, detectar pasajeros indirectos encima
                if (IsInLayerMask(passenger.gameObject, solidifiedShadowLayer))
                {
                    DetectAndMoveIndirectPassengers(passenger, delta, allPassengersToMove);
                }
            }
        }
    }
    
    private void DetectAndMoveIndirectPassengers(KinematicMover solidifiedShadow, Vector2 delta, HashSet<KinematicMover> alreadyMoved)
    {
        BoxCollider2D shadowCollider = solidifiedShadow.GetComponent<BoxCollider2D>();
        if (shadowCollider == null) return;
        
        // Calcular bounds de la sombra en world space
        Bounds shadowBounds = shadowCollider.bounds;
        
        // Lanzar BoxCast desde la parte superior de la sombra hacia arriba
        Vector2 castOrigin = new Vector2(shadowBounds.center.x, shadowBounds.max.y);
        Vector2 boxSize = new Vector2(shadowBounds.size.x, 0.1f);
        float castDistance = shadowBounds.size.y;
        
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            castOrigin,
            boxSize,
            0f,
            Vector2.up,
            castDistance,
            playerLayers | solidifiedShadowLayer
        );
        
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;
            
            KinematicMover indirectPassenger = hit.collider.GetComponent<KinematicMover>();
            
            // Solo mover si tiene KinematicMover y no ha sido movido ya
            if (indirectPassenger != null && !alreadyMoved.Contains(indirectPassenger))
            {
                indirectPassenger.AddMovement(delta);
                alreadyMoved.Add(indirectPassenger);
                
                // Recursión: si esta también es una sombra solidificada, buscar más arriba
                if (IsInLayerMask(indirectPassenger.gameObject, solidifiedShadowLayer))
                {
                    DetectAndMoveIndirectPassengers(indirectPassenger, delta, alreadyMoved);
                }
            }
        }
    }
    
    private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return ((1 << obj.layer) & layerMask) != 0;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        KinematicMover user = other.gameObject.GetComponent<KinematicMover>();
        if (user != null) {
            // Solo añadir como pasajero si NO es un perk Y NO es un enemigo y NO es un globo aerostático.
            if(!user.GetComponent<PerkGrabable>() && 
            !user.GetComponent<EnemyBase>() &&
            !user.GetComponent<HotAirBalloon>()) 
            {
                passengers.Add(user);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        KinematicMover user = other.gameObject.GetComponent<KinematicMover>();
        if (user != null) {
            passengers.Remove(user);
        }
    }
}
