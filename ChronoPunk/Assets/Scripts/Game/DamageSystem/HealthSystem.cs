// HealthSystem.cs
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class HealthSystem : MonoBehaviour
{
    [SerializeField] private int _maxHealth = 1;
    [SerializeField] private bool _invulnerable = false;

    public UnityEvent OnDeath;
    public UnityEvent<int> OnDamaged;
    public UnityEvent<int> OnHealed;

    int currentHealth;

    private void Awake() {
        currentHealth = Mathf.Max(1, _maxHealth);
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsAlive => currentHealth > 0;

    public void SetInvulnerable(bool value) => _invulnerable = value;

    /// <summary>
    /// Devuelve true si la entidad muri� como resultado de este da�o.
    /// </summary>
    public bool TakeDamage(int damageAmount) {
        if (_invulnerable || currentHealth <= 0) return false;

        currentHealth -= Mathf.Max(0, damageAmount);
        OnDamaged?.Invoke(damageAmount);

        if (currentHealth <= 0) {
            currentHealth = 0;
            OnDeath?.Invoke();
            return true;
        }

        return false;
    }

    public void Heal(int amount) {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(_maxHealth, currentHealth + amount);
        OnHealed?.Invoke(amount);
    }

    public void Kill() {
        if (currentHealth <= 0) return;
        currentHealth = 0;
        OnDeath?.Invoke();
    }

    private void Update(){
        if(transform.position.y < -200f) {
            OnDeath?.Invoke();
        }
    }
}
