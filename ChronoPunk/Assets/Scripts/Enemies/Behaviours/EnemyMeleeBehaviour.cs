using UnityEngine;
using Cronopunk.Movement;

/// <summary>
/// Gestiona el comportamiento de un enemigo de combate cuerpo a cuerpo.
/// </summary>
public class EnemyMeleeBehaviour : EnemyBase
{
    #region Enums
    /// <summary>
    /// Estados posibles del enemigo melee.
    /// </summary>
    private enum MeleeState
    {
        Idle,       // Sin patrulla, esperando
        Patrolling, // Patrullando entre puntos
        Chasing,    // Persiguiendo al jugador
        Waiting     // Esperando a que el jugador sea alcanzable
    }
    #endregion

    #region Campos Serializados
    [Header("Persecución")]
    [Tooltip("Velocidad del enemigo mientras persigue al jugador.")]
    [SerializeField] private float _chaseSpeed = 4f;
    [Tooltip("Tiempo de espera antes de poder volver a perseguir tras detenerse.")]
    [SerializeField] private float _chaseCooldown = 1.5f;

    [Header("Debug")]
    [SerializeField] private MeleeState _currentState;
    #endregion

    #region Componentes y Estado
    private Animator _animator;
    private PatrolBehaviour _patrol;
    private ObstacleDetector _obstacleDetector;
    private float _chaseCooldownTimer = 0f;

    // Jump tracking: persigue la última posición en suelo del jugador
    private PlayerMovementKinematic _playerMovement;
    private Vector2 _lastGroundedPosition;
    private bool _isTrackingJump = false;

    /// <summary>
    /// Indica si el componente de patrulla está disponible.
    /// </summary>
    private bool CanPatrol => _patrol != null && _patrol.IsConfigured;

    /// <summary>
    /// Indica si puede transicionar al estado Chasing.
    /// </summary>
    private bool CanChase => _chaseCooldownTimer <= 0f;

    /// <summary>
    /// Verifica si el jugador está en una posición alcanzable (sin obstáculos).
    /// </summary>
    private bool IsPlayerReachable
    {
        get
        {
            if (!HasDetectedPlayer) return false;

            Vector2 targetPosition = _isTrackingJump ? _lastGroundedPosition : (Vector2)DetectedPlayer.position;
            float directionX = Mathf.Sign(targetPosition.x - transform.position.x);

            // Verifica si hay obstáculo en la dirección del jugador
            return _obstacleDetector == null || !_obstacleDetector.IsBlockedHorizontal(directionX);
        }
    }
    #endregion

    #region Ciclo de Vida de Unity

    protected override void Awake()
    {
        base.Awake();
        _patrol = GetComponent<PatrolBehaviour>();
        _obstacleDetector = GetComponent<ObstacleDetector>();
        _animator = GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        base.Start();

        // Determina el estado inicial según si tiene patrulla o no
        TransitionTo(CanPatrol ? MeleeState.Patrolling : MeleeState.Idle);
    }
    #endregion

    #region Comportamiento Principal

    protected override void ExecuteBehaviour()
    {
        // Decrementa el cooldown de persecución
        if (_chaseCooldownTimer > 0f)
        {
            _chaseCooldownTimer -= Time.fixedDeltaTime;
        }

        switch (_currentState)
        {
            case MeleeState.Idle:
                UpdateIdle();
                break;
            case MeleeState.Patrolling:
                UpdatePatrol();
                break;
            case MeleeState.Chasing:
                UpdateChase();
                break;
            case MeleeState.Waiting:
                UpdateWaiting();
                break;
        }
    }

    /// <summary>
    /// Solicita transición a un nuevo estado.
    /// </summary>
    private void TransitionTo(MeleeState newState)
    {
        if (_currentState == newState) return;

        // Exit del estado anterior
        ExitState(_currentState);

        // Enter del nuevo estado
        _currentState = newState;
        EnterState(newState);
    }

    private void EnterState(MeleeState state)
    {
        switch (state)
        {
            case MeleeState.Patrolling:
                EnterPatrol();
                break;
            case MeleeState.Chasing:
                EnterChase();
                break;
        }
    }

    private void ExitState(MeleeState state)
    {
        switch (state)
        {
            case MeleeState.Patrolling:
                ExitPatrol();
                break;
            case MeleeState.Chasing:
                ExitChase();
                break;
        }
    }
    #endregion


    #region Estados - Idle

    /// <summary>
    /// Estado Idle: el enemigo no tiene patrulla, solo busca al jugador.
    /// </summary>
    private void UpdateIdle()
    {
        if (HasDetectedPlayer && CanChase)
        {
            TransitionTo(MeleeState.Chasing);
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
            _animator.Play("EnemyMeleeWalk");
        }
    }

    private void UpdatePatrol()
    {
        if (HasDetectedPlayer && CanChase)
        {
            TransitionTo(MeleeState.Chasing);
            return;
        }

        // Si no tiene patrulla, transiciona a Idle
        if (!CanPatrol)
        {
            TransitionTo(MeleeState.Idle);
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
        // Obtiene la referencia al movimiento del jugador
        if (DetectedPlayer != null)
        {
            _playerMovement = DetectedPlayer.GetComponent<PlayerMovementKinematic>();
            _lastGroundedPosition = DetectedPlayer.position;
            _animator.Play("EnemyMeleeChase");
        }
    }

    private void UpdateChase()
    {
        // Si perdió al jugador
        if (!HasDetectedPlayer)
        {
            TransitionTo(CanPatrol ? MeleeState.Patrolling : MeleeState.Idle);
            return;
        }


        // Lógica de jump tracking: persigue al jugador en suelo, o su última posición conocida si está en el aire
        bool playerOnGround = _playerMovement == null || _playerMovement.IsOnGround;

        if (playerOnGround)
        {
            // El jugador está en el suelo: actualiza su posición y persigue normalmente
            _lastGroundedPosition = DetectedPlayer.position;
            _isTrackingJump = false;
        }
        else
        {
            // El jugador está en el aire: trackea su última posición en suelo
            _isTrackingJump = true;
        }

        // Determina el objetivo
        Vector2 targetPosition = _isTrackingJump ? _lastGroundedPosition : (Vector2)DetectedPlayer.position;
        float distanceToTarget = Mathf.Abs(targetPosition.x - transform.position.x);

        // Si ya llegó a la última posición en suelo y el jugador sigue en el aire, espera quieto
        if (_isTrackingJump && distanceToTarget < 0.2f)
        {
            FaceTowards(DetectedPlayer.position);
            return;
        }

        // Calcula la dirección hacia el objetivo
        float directionX = Mathf.Sign(targetPosition.x - transform.position.x);

        // Si hay obstáculo, pasa a estado Waiting
        if (_obstacleDetector != null && _obstacleDetector.IsBlockedHorizontal(directionX))
        {
            TransitionTo(MeleeState.Waiting);
            return;
        }

        // Persigue al objetivo
        MoveHorizontally(_chaseSpeed, directionX);
        FaceDirection(directionX);
    }

    private void ExitChase()
    {
        // Activa el cooldown para evitar volver inmediatamente a perseguir
        _chaseCooldownTimer = _chaseCooldown;
        _isTrackingJump = false;
    }
    #endregion

    #region Estados - Waiting

    /// <summary>
    /// Estado Waiting: el enemigo detecta al jugador pero hay un obstáculo.
    /// Espera a que el jugador sea alcanzable o salga del rango.
    /// </summary>
    private void UpdateWaiting()
    {
        // Si el jugador salió del rango de detección, vuelve a patrullar/idle
        if (!HasDetectedPlayer)
        {
            TransitionTo(CanPatrol ? MeleeState.Patrolling : MeleeState.Idle);
            return;
        }

        // Si el jugador se volvió alcanzable (sin obstáculos), vuelve a perseguir
        if (IsPlayerReachable && CanChase)
        {
            TransitionTo(MeleeState.Chasing);
            return;
        }

        // Mantiene la orientación hacia el jugador mientras espera
        if (DetectedPlayer != null)
        {
            float directionX = Mathf.Sign(DetectedPlayer.position.x - transform.position.x);
            FaceDirection(directionX);
        }
    }
    #endregion
}
