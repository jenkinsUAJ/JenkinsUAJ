// ContactDamageHandler.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(DamageDealer))]
public class ContactDamageHandler : MonoBehaviour
{
    [Tooltip("Componente que define el da�o.")]
    public DamageDealer dealer;

    private void Reset()
    {
        dealer = GetComponent<DamageDealer>();
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDmg(other.gameObject);
    }

    void TryDealDmg(GameObject other)
    {
        if (dealer == null) return;
        if (other == gameObject) return;

        // Si el otro tiene HealthSystem, le hacemos da�o
        if (other.TryGetComponent<HealthSystem>(out var targetHealth))
        {
            if (dealer.CanDamage(targetHealth))
            {
                targetHealth.TakeDamage(dealer.damageAmount);
                if (dealer.destroySelfOnHit)
                    TryDestroySelf();
            }

        }
    }

    void TryDestroySelf()
    {
        // Si la entidad tiene HealthSystem, intenta matarla para dejar que EntityDeathHandler la gestione
        HealthSystem h = GetComponent<HealthSystem>();
        if (h != null && h.IsAlive)
        {
            h.TakeDamage(int.MaxValue);
            return;
        }

        Destroy(gameObject);
    }
}
