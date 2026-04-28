using UnityEngine;

/// <summary>
/// Gestiona el comportamiento de un enemigo a distancia.
/// </summary>
[RequireComponent(typeof(Shoot))]
public class EnemyShootBehaviour : EnemyBase
{
    #region Enums
    /// <summary>
    /// Estados posibles del enemigo shooter.
    /// </summary>
    private enum ShootState
    {
        Idle,
        Aim,
        Shooting
    }
    #endregion

    #region Campos Serializados
    [Header("Combat")]
    [Tooltip("Tiempo que el enemigo permanece apuntando antes de disparar.")]
    [SerializeField] private float _aimCooldown = 1f;

    [Header("Idle Surveillance")]
    [Tooltip("Si está activo, en Idle el enemigo gira de lado a lado de forma periódica.")]
    [SerializeField] private bool _idleLookAround = false;
    [Tooltip("Valor mínimo del intervalo entre giros en Idle.")]
    [SerializeField] private float _idleFlipIntervalMin = 1.0f;
    [Tooltip("Valor máximo del intervalo entre giros en Idle.")]
    [SerializeField] private float _idleFlipIntervalMax = 2.0f;

    [Header("Debug")]
    [SerializeField] private ShootState _currentState;
    [SerializeField] private float _currentAimCooldown;
    [SerializeField] private float _currentIdleFlipTimer;
    #endregion

    #region Componentes y Estado
    private Shoot _shoot;
    [SerializeField] private Animator _animator;
    #endregion

    #region Ciclo de Vida

    protected override void Awake()
    {
        base.Awake();
        _shoot = GetComponent<Shoot>();
    }

    protected override void Start()
    {
        base.Start();
        TransitionTo(ShootState.Idle);
    }
    #endregion

    #region Comportamiento Principal

    protected override void ExecuteBehaviour()
    {
        switch (_currentState)
        {
            case ShootState.Idle:
                UpdateIdle();
                break;
            case ShootState.Aim:
                UpdateAim();
                break;
            case ShootState.Shooting:
                UpdateShooting();
                break;
        }
    }

    private void TransitionTo(ShootState newState)
    {
        if (_currentState == newState) return;

        // Exit del estado anterior
        ExitState(_currentState);

        // Enter del nuevo estado
        _currentState = newState;
        EnterState(newState);
    }

    private void EnterState(ShootState state)
    {
        switch (state)
        {
            case ShootState.Idle:
                EnterIdle();
                break;
            case ShootState.Aim:
                EnterAim();
                break;
            case ShootState.Shooting:
                EnterShooting();
                break;
        }
    }

    private void ExitState(ShootState state)
    {
        switch (state)
        {
            case ShootState.Idle:
                ExitIdle();
                break;
            case ShootState.Aim:
                ExitAim();
                break;
            case ShootState.Shooting:
                ExitShooting();
                break;
        }
    }
    #endregion

    #region Estados - Idle

    private void EnterIdle()
    {
        ResetIdleFlipTimer();
    }

    private void UpdateIdle()
    {
        if (_idleLookAround)
        {
            _currentIdleFlipTimer -= Time.fixedDeltaTime;
            if (_currentIdleFlipTimer <= 0f)
            {
                Flip();
                ResetIdleFlipTimer();
            }
        }

        if (HasDetectedPlayer)
        {
            TransitionTo(ShootState.Aim);
        }
    }

    private void ExitIdle()
    {
    }
    #endregion

    #region Estados - Aim

    private void EnterAim()
    {
        ResetAimCooldown();
    }

    private void UpdateAim()
    {
        if (!HasDetectedPlayer)
        {
            TransitionTo(ShootState.Idle);
            return;
        }

        AimAtDetectedPlayer();

        _currentAimCooldown -= Time.fixedDeltaTime;
        if (_currentAimCooldown <= 0f)
        {
            TransitionTo(ShootState.Shooting);
        }
    }

    private void ExitAim()
    {
    }
    #endregion

    #region Estados - Shooting

    private void EnterShooting()
    {
        if (!HasDetectedPlayer)
        {
            TransitionTo(ShootState.Idle);
            return;
        }

        AimAtDetectedPlayer();

        if (_animator != null)
        {
            _animator.SetTrigger("Shoot");
        }
        else
        {
            _shoot.TryShoot();
            OnShootAnimationFinished();
        }
    }

    private void UpdateShooting()
    {
        if (HasDetectedPlayer)
        {
            AimAtDetectedPlayer();
        }
    }

    private void ExitShooting()
    {
    }
    #endregion

    #region Animation Events

    public void OnShootAnimationFinished()
    {
        GetComponent<EnemyShooterAudio>().PlayShoot();

        if (_currentState != ShootState.Shooting)
        {
            return;
        }

        if (HasDetectedPlayer)
        {
            TransitionTo(ShootState.Aim);
        }
        else
        {
            TransitionTo(ShootState.Idle);
        }
    }

    #endregion

    #region Helpers

    private void ResetAimCooldown()
    {
        _currentAimCooldown = _aimCooldown;
    }

    private void ResetIdleFlipTimer()
    {
        _currentIdleFlipTimer = Mathf.Max(0.1f, GetRandomIdleFlipInterval());
    }

    private float GetRandomIdleFlipInterval()
    {
        float min = Mathf.Min(_idleFlipIntervalMin, _idleFlipIntervalMax);
        float max = Mathf.Max(_idleFlipIntervalMin, _idleFlipIntervalMax);

        if (Mathf.Approximately(min, max))
        {
            return min;
        }

        float mean = (min + max) * 0.5f;
        float stdDev = (max - min) / 6f;

        float u1 = Mathf.Max(float.Epsilon, Random.value);
        float u2 = Random.value;
        float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);

        float sampled = mean + stdDev * randStdNormal;
        return Mathf.Clamp(sampled, min, max);
    }

    private void AimAtDetectedPlayer()
    {
        if (!HasDetectedPlayer) return;

        Vector2 firePointPos = _shoot.FirePoint.position;
        Vector2 playerPos = DetectedPlayer.position;
        Vector2 directionToPlayer = (playerPos - firePointPos).normalized;
        
        FaceTowards(DetectedPlayer.position);
        _shoot.SetAim(directionToPlayer);
    }

    #endregion
}
