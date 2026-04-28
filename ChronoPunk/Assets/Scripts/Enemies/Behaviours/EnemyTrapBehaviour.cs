using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Controla un enemigo tipo trampa que cae desde una posición inicial hasta un objetivo
/// y luego regresa. El movimiento de caída y retorno es controlado por AnimationCurves.
/// </summary>
public class EnemyTrapBehaviour : EnemyBase
{

    [SerializeField]
    private Animator _animator;

    private bool _attackTriggered = false;

    #region Enums
    /// <summary>
    /// Estados posibles de la trampa.
    /// </summary>
    private enum TrapState
    {
        Idle,       // Esperando en la posición inicial (arriba)
        Slamming,   // Moviéndose hacia el suelo
        Grounded,   // En el suelo, antes de regresar
        Returning   // Volviendo a la posición inicial
    }
    #endregion

    #region Campos Serializados
    [Header("Timing")]
    [Tooltip("Tiempo en segundos que la trampa espera en la posición inicial antes de caer.")]
    [SerializeField] private float _idleTime = 2.0f;
    [Tooltip("Duración en segundos del movimiento de caída (slam).")]
    [SerializeField] private float _slamDuration = 0.5f;
    [Tooltip("Tiempo en segundos que la trampa espera en el suelo antes de regresar.")]
    [SerializeField] private float _groundedTime = 1.0f;
    [Tooltip("Duración en segundos del movimiento de retorno.")]
    [SerializeField] private float _returnDuration = 1.5f;

    [Header("Movement Curves")]
    [Tooltip("Curva que define la progresión del movimiento de caída.")]
    [SerializeField] private AnimationCurve _slamCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Curva que define la progresión del movimiento de retorno.")]
    [SerializeField] private AnimationCurve _returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Target")]
    [Tooltip("Transform que marca la posición objetivo a la que la trampa caerá.")]
    [SerializeField] private Transform _slamTarget;

    [Header("Debug")]
    [SerializeField] private TrapState _currentState;
    #endregion

    #region Variables Privadas
    private Vector2 _initialPosition;
    private Vector2 _targetPosition;
    private float _timer;
    #endregion

    #region Ciclo de Vida de Unity

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        // Guardar posición inicial desde Mover para determinismo
        _initialPosition = Mover.Position;

        if (_slamTarget == null)
        {
            Debug.LogError("El objetivo de la trampa (_slamTarget) no está asignado en " + gameObject.name);
            enabled = false;
            return;
        }
        _targetPosition = _slamTarget.position;
        TransitionTo(TrapState.Idle);
    }

    // Override porque la trampa no usa detección de jugador
    protected void OnDrawGizmosSelected()
    {
        if (_slamTarget != null)
        {
            Gizmos.color = Color.red;
            Vector2 startPos = Application.isPlaying ? _initialPosition : (Vector2)transform.position;
            Gizmos.DrawLine(startPos, _slamTarget.position);
            Gizmos.DrawWireSphere(_slamTarget.position, 0.25f);
        }
    }
    #endregion

    #region Comportamiento Principal

    protected override void ExecuteBehaviour()
    {
        _timer += Time.fixedDeltaTime;

        switch (_currentState)
        {
            case TrapState.Idle:
                UpdateIdle();
                break;
            case TrapState.Slamming:
                UpdateSlamming();
                break;
            case TrapState.Grounded:
                UpdateGrounded();
                break;
            case TrapState.Returning:
                UpdateReturning();
                break;
        }
    }

    private void TransitionTo(TrapState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        _timer = 0f; // Reset timer on transition
    }
    #endregion

    #region Estados - Idle

    private void UpdateIdle()
    {
        if (_timer >= _idleTime)
        {
            TransitionTo(TrapState.Slamming);
        }
    }
    #endregion

    #region Estados - Slamming

    private void UpdateSlamming()
    {
        float progress = Mathf.Clamp01(_timer / _slamDuration);
        float curveValue = _slamCurve.Evaluate(progress);

        Vector2 currentPos = Mover.Position;
        Vector2 targetPos = Vector2.Lerp(_initialPosition, _targetPosition, curveValue);
        Vector2 moveDelta = targetPos - currentPos;

        Mover.AddMovement(moveDelta);


        if (!_attackTriggered)
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
                _attackTriggered = true;
            }
        }
       

        if (progress >= 1.0f)
        {
            Mover.SetPosition(_targetPosition);
            TransitionTo(TrapState.Grounded);
        }
    }
    #endregion

    #region Estados - Grounded

    private void UpdateGrounded()
    {
        _attackTriggered = false;

        if (_timer >= _groundedTime)
        {
            TransitionTo(TrapState.Returning);
        }
    }
    #endregion

    #region Estados - Returning

    private void UpdateReturning()
    {
        float progress = Mathf.Clamp01(_timer / _returnDuration);
        float curveValue = _returnCurve.Evaluate(progress);

        Vector2 currentPos = Mover.Position;
        Vector2 targetPos = Vector2.Lerp(_targetPosition, _initialPosition, curveValue);
        Vector2 moveDelta = targetPos - currentPos;

        Mover.AddMovement(moveDelta);

        if (progress >= 1.0f)
        {
            Mover.SetPosition(_initialPosition);
            TransitionTo(TrapState.Idle);
        }
    }
    #endregion
}
