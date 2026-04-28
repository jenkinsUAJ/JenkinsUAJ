using Cronopunk.Movement;
using UnityEngine;

public class BulletBehaviour : PausableMonoBehaviour
{
    private KinematicMover _km;

    private Vector2 _velocity;

    private void Awake() {
        _km = GetComponent<KinematicMover>();
    }

    public void Initialize(GameObject shooter, Vector2 direction, int damage, float speed, float lifetime, LayerMask collisionLayers) {
        // asignar damageSource si hay DamageDealer
        if (TryGetComponent<DamageDealer>(out var dd)) {
            dd.damageSource = shooter;
            dd.damageAmount = damage;
            dd.SetDamageableLayers(collisionLayers);
        }

        _velocity = direction.normalized * speed;

        if (lifetime == -1) return;
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate() {
        if (IsPaused) return;
        if (_km == null) return;

        _km.AddMovement(_velocity * Time.fixedDeltaTime);
        

    }
}
