using UnityEngine;
using Cronopunk.Movement;

/// <summary>
/// Componente autónomo que gestiona la detección del jugador.
/// Soporta detección por cono con ángulo configurable (360° = círculo completo),
/// línea de visión opcional, y rangos separados para detección y pérdida.
/// Es completamente autogestionado: solo requiere Initialize() + Tick().
/// </summary>
public class PlayerDetector : MonoBehaviour
{
    #region Eventos
    /// <summary>
    /// Se invoca cuando el jugador es detectado.
    /// </summary>
    public event System.Action<Transform> OnPlayerDetected;

    /// <summary>
    /// Se invoca cuando el jugador se pierde (sale de rango o pierde línea de visión).
    /// </summary>
    public event System.Action OnPlayerLost;

    #endregion

    #region Campos Serializados
    [Header("Rango de Detección")]
    [Tooltip("Radio de detección del jugador.")]
    [SerializeField] private float _detectionRange = 8f;
    [Tooltip("Radio para considerar al jugador fuera de rango (debe ser >= detectionRange para histéresis).")]
    [SerializeField] private float _outOfRangeDistance = 10f;

    [Header("Cono de Detección")]
    [Tooltip("Ángulo del cono de detección en grados (360 = detección circular completa).")]
    [Range(0f, 360f)]
    [SerializeField] private float _detectionAngle = 360f;
    [Tooltip("Offset del ángulo de dirección del cono en grados (0 = adelante según la orientación del transform).")]
    [SerializeField] private float _coneDirectionOffset = 0f;

    [Header("Línea de Visión")]
    [Tooltip("Si es true, requiere línea de visión directa para detectar al jugador.")]
    [SerializeField] private bool _requireLineOfSight = false;
    [Tooltip("Radio del CircleCast para verificar línea de visión (0 = Raycast simple).")]
    [SerializeField] private float _lineOfSightRadius = 0.2f;
    [Tooltip("Capas que bloquean la línea de visión.")]
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Configuración")]
    [Tooltip("Offset relativo que representa la posición de los ojos del enemigo (punto de origen para detecciones).")]
    [SerializeField] private Vector2 _eyesOffset = Vector2.zero;
    [Tooltip("La capa (Layer) en la que se encuentra el jugador.")]
    [SerializeField] private LayerMask _playerLayer;
    [Tooltip("Tiempo de cooldown tras perder al jugador antes de poder detectarlo de nuevo.")]
    [SerializeField] private float _detectionCooldown = 2f;

    [Header("Debug")]
    [Tooltip("Mostrar gizmos de debug siempre (no solo cuando está seleccionado).")]
    [SerializeField] private bool _alwaysShowGizmos = true;
    #endregion

    #region Propiedades Públicas
    /// <summary>
    /// Transform del jugador detectado. Null si no hay jugador detectado.
    /// </summary>
    public Transform DetectedPlayer { get; private set; }

    /// <summary>
    /// Indica si hay un jugador detectado actualmente.
    /// </summary>
    public bool HasDetectedPlayer => DetectedPlayer != null;

    /// <summary>
    /// Indica si el detector está en cooldown.
    /// </summary>
    public bool IsInCooldown => _cooldownTimer > 0f;

    /// <summary>
    /// Indica si el detector está activo.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Dirección hacia el jugador detectado (normalizada). Zero si no hay jugador.
    /// </summary>
    public Vector2 DirectionToPlayer => HasDetectedPlayer
        ? ((Vector2)(DetectedPlayer.position - (Vector3)EyesPosition)).normalized
        : Vector2.zero;

    /// <summary>
    /// Distancia al jugador detectado. -1 si no hay jugador.
    /// </summary>
    public float DistanceToPlayer => HasDetectedPlayer
        ? Vector2.Distance(EyesPosition, DetectedPlayer.position)
        : -1f;

    /// <summary>
    /// Posición de los ojos del enemigo (origen de las detecciones).
    /// Usa KinematicMover si existe para mantener determinismo.
    /// </summary>
    private Vector2 EyesPosition => (_mover != null ? _mover.Position : (Vector2)transform.position) + _eyesOffset;
    #endregion

    #region Variables Privadas
    private KinematicMover _mover;
    private float _cooldownTimer = 0f;
    private bool _isFacingRight = true;
    #endregion

    #region Métodos Públicos de Control

    /// <summary>
    /// Inicializa el detector. Debe llamarse antes de usar Tick().
    /// </summary>
    /// <param name="isFacingRight">Orientación inicial del enemigo.</param>
    public void Initialize(bool isFacingRight = true)
    {
        _mover = GetComponent<KinematicMover>();
        _isFacingRight = isFacingRight;
        UpdateFacing(_isFacingRight);
        _cooldownTimer = 0f;
        DetectedPlayer = null;
        IsActive = true;
    }

    /// <summary>
    /// Actualiza el detector. Debe llamarse en cada frame.
    /// Gestiona automáticamente la detección, pérdida y cooldown.
    /// </summary>
    /// <param name="deltaTime">Tiempo desde el último frame.</param>
    public void Tick(float deltaTime)
    {
        if (!IsActive) return;

        // Actualiza el cooldown
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= deltaTime;
        }

        // Si ya tiene un jugador detectado, verifica si lo pierde
        if (HasDetectedPlayer)
        {
            if (ShouldLosePlayer())
            {
                LosePlayer();
            }
        }
        // Si no tiene jugador y no está en cooldown, intenta detectar
        else if (!IsInCooldown)
        {
            TryDetectPlayer();
        }
    }

    /// <summary>
    /// Actualiza la orientación del enemigo (afecta la dirección del cono).
    /// </summary>
    /// <param name="isFacingRight">True si el enemigo mira a la derecha.</param>
    public void UpdateFacing(bool isFacingRight)
    {
        _isFacingRight = isFacingRight;
    }

    /// <summary>
    /// Fuerza la pérdida del jugador detectado y activa el cooldown.
    /// </summary>
    public void ForceLosePlayer()
    {
        if (HasDetectedPlayer)
        {
            LosePlayer();
        }
    }

    /// <summary>
    /// Detiene el detector y limpia el estado.
    /// </summary>
    public void Stop()
    {
        IsActive = false;
        DetectedPlayer = null;
        _cooldownTimer = 0f;
    }
    #endregion

    #region Métodos Privados de Detección

    /// <summary>
    /// Intenta detectar al jugador dentro del rango y cono de detección.
    /// </summary>
    private void TryDetectPlayer()
    {
        // Busca jugadores en el rango de detección desde los ojos
        Collider2D playerCollider = Physics2D.OverlapCircle(EyesPosition, _detectionRange, _playerLayer);

        if (playerCollider == null) return;
        if (!IsValidDetectableTarget(playerCollider.transform, playerCollider)) return;

        // Obtener posición del jugador usando KinematicMover si existe
        KinematicMover playerMover = playerCollider.GetComponent<KinematicMover>();
        Vector2 playerPosition = playerMover != null ? playerMover.Position : (Vector2)playerCollider.transform.position;

        // Verifica si está dentro del cono de detección
        if (!IsInDetectionCone(playerPosition)) return;

        // Verifica línea de visión si es requerida
        if (_requireLineOfSight && !HasLineOfSight(playerPosition)) return;

        // ¡Jugador detectado!
        DetectedPlayer = playerCollider.transform;
        OnPlayerDetected?.Invoke(DetectedPlayer);
    }

    /// <summary>
    /// Determina si el jugador debe perderse.
    /// </summary>
    private bool ShouldLosePlayer()
    {
        if (DetectedPlayer == null) return true;
        if (!IsValidDetectableTarget(DetectedPlayer)) return true;

        Vector2 playerPosition = DetectedPlayer.position;
        float distance = Vector2.Distance(EyesPosition, playerPosition);

        // Fuera del rango extendido
        if (distance > _outOfRangeDistance) return true;

        // Fuera del cono de detección
        if (!IsInDetectionCone(playerPosition)) return true;

        // Si requiere línea de visión, verifica que aún la tenga
        if (_requireLineOfSight && !HasLineOfSight(playerPosition)) return true;

        return false;
    }

    private bool IsValidDetectableTarget(Transform target, Collider2D knownCollider = null)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        Collider2D targetCollider = knownCollider != null ? knownCollider : target.GetComponent<Collider2D>();
        if (targetCollider == null || !targetCollider.enabled) return false;

        if (target.TryGetComponent<HealthSystem>(out var health) && !health.IsAlive)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Pierde al jugador y activa el cooldown.
    /// </summary>
    private void LosePlayer()
    {
        DetectedPlayer = null;
        _cooldownTimer = _detectionCooldown;
        OnPlayerLost?.Invoke();
    }

    /// <summary>
    /// Verifica si una posición está dentro del cono de detección.
    /// </summary>
    private bool IsInDetectionCone(Vector2 targetPosition)
    {
        // Si el ángulo es 360°, siempre está en el cono (detección circular)
        if (_detectionAngle >= 360f) return true;

        // Calcula la dirección hacia el objetivo desde los ojos
        Vector2 directionToTarget = (targetPosition - EyesPosition).normalized;

        // Calcula la dirección del cono (basada en la orientación del enemigo)
        float facingAngle = _isFacingRight ? 0f : 180f;
        float coneAngle = facingAngle + _coneDirectionOffset;
        Vector2 coneDirection = AngleToDirection(coneAngle);

        // Calcula el ángulo entre la dirección del cono y la dirección al objetivo
        float angleToTarget = Vector2.Angle(coneDirection, directionToTarget);

        // Verifica si está dentro del cono (el ángulo es la mitad del cono total)
        return angleToTarget <= _detectionAngle * 0.5f;
    }

    /// <summary>
    /// Verifica si hay línea de visión hacia una posición.
    /// </summary>
    private bool HasLineOfSight(Vector2 targetPosition)
    {
        Vector2 direction = targetPosition - EyesPosition;
        float distance = direction.magnitude;

        RaycastHit2D hit;

        if (_lineOfSightRadius > 0f)
        {
            // CircleCast para colisionadores más gruesos
            hit = Physics2D.CircleCast(
                EyesPosition,
                _lineOfSightRadius,
                direction.normalized,
                distance,
                _obstacleLayer
            );
        }
        else
        {
            // Raycast simple
            hit = Physics2D.Raycast(
                EyesPosition,
                direction.normalized,
                distance,
                _obstacleLayer
            );
        }

        return hit.collider == null;
    }

    /// <summary>
    /// Convierte un ángulo en grados a una dirección Vector2.
    /// </summary>
    private Vector2 AngleToDirection(float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
    }
    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (_alwaysShowGizmos)
        {
            DrawDetectionGizmos();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!_alwaysShowGizmos)
        {
            DrawDetectionGizmos();
        }
    }

    private void DrawDetectionGizmos()
    {
        Vector3 position = EyesPosition;

        // Dibuja el indicador de los ojos
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(position, 0.1f);
        // Línea desde el transform al punto de los ojos
        if (_eyesOffset.sqrMagnitude > 0.001f)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawLine(transform.position, position);
        }

        // Color basado en el estado
        Color detectionColor = IsInCooldown ? Color.cyan : (HasDetectedPlayer ? Color.red : Color.yellow);
        Color outOfRangeColor = new Color(1f, 0.5f, 0f, 0.3f); // Naranja transparente

        // Dibuja el rango de detección
        if (_detectionAngle >= 360f)
        {
            // Círculo completo
            Gizmos.color = detectionColor;
            DrawWireCircle(position, _detectionRange, 32);
        }
        else
        {
            // Cono de detección
            DrawDetectionCone(position, detectionColor);
        }

        // Dibuja el rango de "out of range"
        if (_outOfRangeDistance > _detectionRange)
        {
            Gizmos.color = outOfRangeColor;
            DrawWireCircle(position, _outOfRangeDistance, 32);
        }

        // Si hay jugador detectado, dibuja la línea hacia él
        if (HasDetectedPlayer)
        {
            Vector2 directionToPlayer = (Vector2)(DetectedPlayer.position - (Vector3)position);
            float distanceToPlayer = directionToPlayer.magnitude;

            // Dibuja la línea de visión
            Gizmos.color = Color.red;
            Gizmos.DrawLine(position, DetectedPlayer.position);

            // Visualiza el grosor del CircleCast si está habilitado
            if (_requireLineOfSight && _lineOfSightRadius > 0f)
            {
                // Dibuja círculos a lo largo del trayecto para mostrar el grosor
                Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
                int steps = Mathf.Max(5, Mathf.CeilToInt(distanceToPlayer / 1f));
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector3 pointAlongRay = Vector3.Lerp(position, DetectedPlayer.position, t);
                    DrawWireCircle(pointAlongRay, _lineOfSightRadius, 12);
                }

                // Círculo en la posición del jugador
                Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
                DrawWireCircle(DetectedPlayer.position, _lineOfSightRadius, 16);
            }
        }
    }

    private void DrawDetectionCone(Vector3 eyesPosition, Color color)
    {
        Gizmos.color = color;

        // Calcula la dirección central del cono
        float facingAngle = _isFacingRight ? 0f : 180f;
        float coneAngle = facingAngle + _coneDirectionOffset;

        float halfAngle = _detectionAngle * 0.5f;
        float leftAngle = coneAngle + halfAngle;
        float rightAngle = coneAngle - halfAngle;

        Vector2 leftDirection = AngleToDirection(leftAngle);
        Vector2 rightDirection = AngleToDirection(rightAngle);

        // Dibuja los bordes del cono
        Vector3 leftEnd = eyesPosition + (Vector3)(leftDirection * _detectionRange);
        Vector3 rightEnd = eyesPosition + (Vector3)(rightDirection * _detectionRange);

        Gizmos.DrawLine(eyesPosition, leftEnd);
        Gizmos.DrawLine(eyesPosition, rightEnd);

        // Dibuja el arco del cono
        int segments = Mathf.Max(8, Mathf.CeilToInt(_detectionAngle / 10f));
        float angleStep = _detectionAngle / segments;

        Vector3 previousPoint = rightEnd;
        for (int i = 1; i <= segments; i++)
        {
            float angle = rightAngle + angleStep * i;
            Vector2 direction = AngleToDirection(angle);
            Vector3 point = eyesPosition + (Vector3)(direction * _detectionRange);
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Dibuja la dirección central (más sutil)
        Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
        Vector2 centerDirection = AngleToDirection(coneAngle);
        Gizmos.DrawLine(eyesPosition, eyesPosition + (Vector3)(centerDirection * _detectionRange * 0.7f));
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
    }
    #endregion
}
