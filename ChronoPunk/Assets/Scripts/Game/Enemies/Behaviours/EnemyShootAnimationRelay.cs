using UnityEngine;

public class EnemyShootAnimationRelay : MonoBehaviour
{
    [SerializeField] private EnemyShootBehaviour _behaviour;
    [SerializeField] private Shoot _shoot;

    public void Configure(EnemyShootBehaviour behaviour, Shoot shoot)
    {
        _behaviour = behaviour;
        _shoot = shoot;
    }

    private void Awake()
    {
        if (_behaviour == null)
        {
            _behaviour = GetComponentInParent<EnemyShootBehaviour>();
        }

        if (_shoot == null)
        {
            _shoot = GetComponentInParent<Shoot>();
        }
    }

    public void TryShoot()
    {
        _shoot?.TryShoot();
    }

    public void OnShootAnimationFinished()
    {
        _behaviour?.OnShootAnimationFinished();
    }
}
