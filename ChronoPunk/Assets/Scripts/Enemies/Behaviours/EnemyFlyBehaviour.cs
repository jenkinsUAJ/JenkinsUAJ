using UnityEngine;

/// <summary>
/// Gestiona el comportamiento de un enemigo volador.
/// </summary>
public class EnemyFlyBehaviour : EnemyBase
{
    #region Enums
    /// <summary>
    /// Estados posibles del enemigo volador.
    /// </summary>
    private enum FlyState
    {
        Idle,       // Sin patrulla, esperando
        Patrolling, // Patrullando entre puntos
        Chasing     // Persiguiendo al jugador
    }
    #endregion

    #region Campos Serializados
    [Header("Persecución")]
    [SerializeField] private float _chaseSpeed = 5f;

    [Header("Debug")]
    [SerializeField] private FlyState _currentState;
    #endregion

    #region Componentes y Estado
    private PatrolBehaviour _patrol;
    private Vector2 _currentMovementDirection = Vector2.right;

    /// <summary>
    /// Indica si el componente de patrulla está disponible.
    /// </summary>
    private bool CanPatrol => _patrol != null && _patrol.IsConfigured;
    #endregion

    #region Ciclo de Vida de Unity

    protected override void Awake()
    {
        base.Awake();
        _patrol = GetComponent<PatrolBehaviour>();
    }

    protected override void Start()
    {
        base.Start();

        // Determina el estado inicial según si tiene patrulla o no
        TransitionTo(CanPatrol ? FlyState.Patrolling : FlyState.Idle);
    }
    #endregion

    #region Comportamiento Principal

    protected override void ExecuteBehaviour()
    {
        switch (_currentState)
        {
            case FlyState.Idle:
                UpdateIdle();
                break;
            case FlyState.Patrolling:
                UpdatePatrol();
                break;
            case FlyState.Chasing:
                UpdateChase();
                break;
        }
    }

    private void TransitionTo(FlyState newState)
    {
        if (_currentState == newState) return;

        // Exit del estado anterior
        ExitState(_currentState);

        // Enter del nuevo estado
        _currentState = newState;
        EnterState(newState);
    }

    private void EnterState(FlyState state)
    {
        switch (state)
        {
            case FlyState.Patrolling:
                EnterPatrol();
                break;
            case FlyState.Chasing:
                EnterChase();
                break;
        }
    }

    private void ExitState(FlyState state)
    {
        switch (state)
        {
            case FlyState.Patrolling:
                ExitPatrol();
                break;
            case FlyState.Chasing:
                ExitChase();
                break;
        }
    }
    #endregion

    #region Estados - Idle

    private void UpdateIdle()
    {
        if (HasDetectedPlayer)
        {
            TransitionTo(FlyState.Chasing);
        }
    }
    #endregion

    #region Estados - Patrol

    private void EnterPatrol()
    {
        if (CanPatrol)
        {
            _patrol.Initialize();
            FaceTowards(_patrol.TargetPosition);
        }
    }

    private void UpdatePatrol()
    {
        if (HasDetectedPlayer)
        {
            TransitionTo(FlyState.Chasing);
            return;
        }

        if (!CanPatrol)
        {
            TransitionTo(FlyState.Idle);
            return;
        }

        // Actualiza la patrulla (el PatrolBehaviour se autogestiona)
        _patrol.Tick(Time.fixedDeltaTime);

        // Actualiza la orientación hacia el target actual
        FaceTowards(_patrol.TargetPosition);
    }

    private void ExitPatrol()
    {
        if (CanPatrol)
        {
            _patrol.Stop();
        }
    }
    #endregion

    #region Estados - Chase

    private void EnterChase()
    {
        // No requiere inicialización especial
    }

    private void UpdateChase()
    {
        // Si perdió al jugador
        if (!HasDetectedPlayer)
        {
            TransitionTo(CanPatrol ? FlyState.Patrolling : FlyState.Idle);
            return;
        }

        // Persigue al jugador
        _currentMovementDirection = (DetectedPlayer.position - transform.position).normalized;
        MoveInDirection(_chaseSpeed, _currentMovementDirection);
        FaceTowards(DetectedPlayer.position);
    }

    private void ExitChase()
    {
    }
    #endregion
}
