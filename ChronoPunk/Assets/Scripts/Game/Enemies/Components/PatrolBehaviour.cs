using UnityEngine;
using System.Collections.Generic;
using Cronopunk.Movement;

/// <summary>
/// Componente autónomo que gestiona el comportamiento de patrulla entre múltiples puntos.
/// Puede ser usado por cualquier enemigo que necesite patrullar.
/// Es completamente autogestionado: solo requiere Initialize() + Tick() + Stop().
/// Lanza eventos cuando ocurren situaciones interesantes durante la patrulla.
/// Requiere KinematicMover y ObstacleDetector en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(KinematicMover))]
[RequireComponent(typeof(ObstacleDetector))]
public class PatrolBehaviour : MonoBehaviour
{
    #region Enums
    /// <summary>
    /// Tipo de movimiento de patrulla.
    /// </summary>
    public enum PatrolMode
    {
        Ground,  // Movimiento horizontal (enemigos terrestres)
        Flying   // Movimiento 2D (enemigos voladores)
    }
    #endregion

    #region Eventos
    /// <summary>
    /// Se invoca cuando el enemigo alcanza un punto de patrulla.
    /// </summary>
    public event System.Action OnTargetReached;

    /// <summary>
    /// Se invoca cuando se detecta un obstáculo (pared o escalón).
    /// </summary>
    public event System.Action OnObstacleDetected;
    #endregion

    #region Campos Serializados
    [Header("Puntos de Patrulla")]
    [Tooltip("Lista de puntos de patrulla. El enemigo los recorrerá en orden cíclico.")]
    [SerializeField] private List<Transform> _patrolPoints = new List<Transform>();

    [Header("Configuración")]
    [Tooltip("Modo de movimiento: Ground (horizontal) o Flying (2D).")]
    [SerializeField] private PatrolMode _patrolMode = PatrolMode.Ground;
    [Tooltip("Velocidad de movimiento durante la patrulla.")]
    [SerializeField] private float _patrolSpeed = 2f;
    [Tooltip("Tiempo de espera al llegar a un punto de patrulla.")]
    [SerializeField] private float _waitTime = 1f;
    #endregion

    #region Propiedades Públicas
    /// <summary>
    /// El punto de patrulla actual hacia el que se dirige.
    /// </summary>
    public Transform CurrentTarget { get; private set; }

    /// <summary>
    /// Velocidad de patrulla configurada.
    /// </summary>
    public float PatrolSpeed => _patrolSpeed;

    /// <summary>
    /// Indica si el componente está correctamente configurado.
    /// </summary>
    public bool IsConfigured => _patrolPoints != null && _patrolPoints.Count >= 2 && _patrolPoints.TrueForAll(p => p != null);

    /// <summary>
    /// Indica si está esperando en un punto de patrulla.
    /// </summary>
    public bool IsWaiting => _waitTimer > 0;

    /// <summary>
    /// Indica si la patrulla está activa (inicializada).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Dirección horizontal hacia el objetivo actual (-1 izquierda, 1 derecha).
    /// </summary>
    public float DirectionX => CurrentTarget != null && _mover != null
        ? Mathf.Sign(CurrentTarget.position.x - _mover.Position.x)
        : 0f;

    /// <summary>
    /// Dirección 2D normalizada hacia el objetivo actual.
    /// </summary>
    public Vector2 Direction2D => CurrentTarget != null && _mover != null
        ? ((Vector2)CurrentTarget.position - _mover.Position).normalized
        : Vector2.zero;

    /// <summary>
    /// Posición del objetivo actual.
    /// </summary>
    public Vector2 TargetPosition => CurrentTarget != null
        ? (Vector2)CurrentTarget.position
        : (_mover != null ? _mover.Position : (Vector2)transform.position);
    #endregion

    #region Variables Privadas
    private float _waitTimer = 0f;
    private KinematicMover _mover;
    private ObstacleDetector _obstacleDetector;
    private int _currentPatrolIndex = 0;
    #endregion

    #region Ciclo de Vida de Unity

    private void Awake()
    {
        _mover = GetComponent<KinematicMover>();
        _obstacleDetector = GetComponent<ObstacleDetector>();
    }
    #endregion

    #region Métodos Públicos de Control

    /// <summary>
    /// Inicializa la patrulla. Debe llamarse en el Enter del estado de patrulla.
    /// </summary>
    public void Initialize()
    {
        if (!IsConfigured)
        {
            Debug.LogError($"PatrolBehaviour en {gameObject.name}: Los puntos de patrulla no están configurados.");
            return;
        }

        _waitTimer = 0f;
        IsActive = true;

        // Elige el punto más cercano como objetivo inicial
        ChooseClosestPoint();
    }

    /// <summary>
    /// Actualiza la patrulla. Debe llamarse en cada frame del estado de patrulla.
    /// Gestiona automáticamente el movimiento, detección de llegada y obstáculos.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!IsActive || !IsConfigured || _mover == null) return;

        // Actualiza el temporizador de espera
        if (_waitTimer > 0)
        {
            _waitTimer -= deltaTime;
            return;
        }

        // Detecta obstáculos según el modo
        if (DetectObstacle())
        {
            SwitchTargetAndWait();
            OnObstacleDetected?.Invoke();
            return;
        }

        // Ejecuta el movimiento según el modo
        if (_patrolMode == PatrolMode.Ground)
        {
            TickGroundPatrol(deltaTime);
        }
        else
        {
            TickFlyingPatrol(deltaTime);
        }
    }

    /// <summary>
    /// Detiene la patrulla y limpia el estado.
    /// Debe llamarse en el Exit del estado de patrulla.
    /// </summary>
    public void Stop()
    {
        IsActive = false;
        _waitTimer = 0f;
    }
    #endregion

    #region Métodos Privados de Movimiento

    /// <summary>
    /// Actualiza la patrulla para enemigos terrestres (movimiento horizontal).
    /// </summary>
    private void TickGroundPatrol(float deltaTime)
    {
        float directionX = DirectionX;
        float currentX = _mover.Position.x;
        float speed = _patrolSpeed;
        float nextX = currentX + directionX * speed * deltaTime;

        // Comprueba si llegará al objetivo
        if (WouldOvershootX(currentX, nextX))
        {
            // Aterriza exactamente en el punto
            Vector2 snapDelta = new Vector2(GetSnapDeltaX(currentX), 0);
            _mover.AddMovement(snapDelta);
            SwitchTargetAndWait();
            OnTargetReached?.Invoke();
        }
        else
        {
            // Movimiento normal
            _mover.AddMovement(new Vector2(directionX * speed * deltaTime, 0));
        }
    }

    /// <summary>
    /// Actualiza la patrulla para enemigos voladores (movimiento 2D).
    /// </summary>
    private void TickFlyingPatrol(float deltaTime)
    {
        Vector2 currentPos = _mover.Position;
        Vector2 direction = Direction2D;
        Vector2 nextPos = currentPos + direction * _patrolSpeed * deltaTime;

        // Comprueba si se pasará del objetivo
        if (WouldOvershoot2D(currentPos, nextPos))
        {
            // Snapea exactamente al punto
            Vector2 snapDelta = GetSnapDelta2D(currentPos);
            _mover.AddMovement(snapDelta);
            SwitchTargetAndWait();
            OnTargetReached?.Invoke();
        }
        else
        {
            // Movimiento normal
            _mover.AddMovement(direction * _patrolSpeed * deltaTime);
        }
    }

    /// <summary>
    /// Detecta obstáculos según el modo de patrulla usando ObstacleDetector.
    /// </summary>
    private bool DetectObstacle()
    {
        if (_obstacleDetector == null) return false;

        if (_patrolMode == PatrolMode.Ground)
        {
            return _obstacleDetector.IsBlockedHorizontal(DirectionX);
        }
        else
        {
            return _obstacleDetector.IsBlocked2D(Direction2D);
        }
    }
    #endregion

    #region Métodos Auxiliares

    /// <summary>
    /// Comprueba si se ha alcanzado el objetivo actual (para enemigos terrestres, solo en X).
    /// </summary>
    private bool WouldOvershootX(float currentX, float nextX)
    {
        if (CurrentTarget == null) return false;
        float targetX = CurrentTarget.position.x;
        return Mathf.Sign(targetX - currentX) != Mathf.Sign(targetX - nextX);
    }

    /// <summary>
    /// Comprueba si se pasará del objetivo actual (para enemigos voladores, distancia 2D).
    /// </summary>
    private bool WouldOvershoot2D(Vector2 currentPos, Vector2 nextPos)
    {
        if (CurrentTarget == null) return false;
        Vector2 targetPos = CurrentTarget.position;

        // Comprueba si el signo de la dirección cambia (overshoot)
        Vector2 directionBefore = targetPos - currentPos;
        Vector2 directionAfter = targetPos - nextPos;

        return Vector2.Dot(directionBefore, directionAfter) <= 0;
    }

    /// <summary>
    /// Calcula el delta de movimiento exacto para aterrizar en el objetivo (eje X).
    /// </summary>
    private float GetSnapDeltaX(float currentX)
    {
        if (CurrentTarget == null) return 0f;
        return CurrentTarget.position.x - currentX;
    }

    /// <summary>
    /// Calcula el delta de movimiento exacto para aterrizar en el objetivo (2D).
    /// </summary>
    private Vector2 GetSnapDelta2D(Vector2 currentPos)
    {
        if (CurrentTarget == null) return Vector2.zero;
        return (Vector2)CurrentTarget.position - currentPos;
    }

    /// <summary>
    /// Cambia al siguiente punto de patrulla y activa el temporizador de espera.
    /// </summary>
    private void SwitchTargetAndWait()
    {
        SwitchTarget();
        _waitTimer = _waitTime;
    }

    /// <summary>
    /// Cambia al siguiente punto de patrulla sin esperar.
    /// </summary>
    private void SwitchTarget()
    {
        if (!IsConfigured) return;

        // Avanza al siguiente índice de forma cíclica
        _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Count;
        CurrentTarget = _patrolPoints[_currentPatrolIndex];
    }

    /// <summary>
    /// Elige el punto de patrulla más cercano como objetivo actual.
    /// </summary>
    private void ChooseClosestPoint()
    {
        if (!IsConfigured || _mover == null) return;

        float minDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < _patrolPoints.Count; i++)
        {
            float distance = Vector2.Distance(_mover.Position, _patrolPoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        _currentPatrolIndex = closestIndex;
        CurrentTarget = _patrolPoints[_currentPatrolIndex];
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (_patrolPoints == null || _patrolPoints.Count < 2) return;

        Gizmos.color = Color.cyan;

        // Dibuja líneas entre puntos consecutivos
        for (int i = 0; i < _patrolPoints.Count; i++)
        {
            if (_patrolPoints[i] == null) continue;

            // Dibuja esfera en el punto
            Gizmos.DrawSphere(_patrolPoints[i].position, 0.15f);

            // Dibuja línea al siguiente punto (cíclico)
            int nextIndex = (i + 1) % _patrolPoints.Count;
            if (_patrolPoints[nextIndex] != null)
            {
                Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[nextIndex].position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (CurrentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);
        }
    }
    #endregion
}
