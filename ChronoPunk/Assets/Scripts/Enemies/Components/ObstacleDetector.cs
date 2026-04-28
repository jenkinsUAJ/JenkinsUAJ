using UnityEngine;
using Cronopunk.Movement;

/// <summary>
/// Componente que detecta obstáculos (paredes y vacíos) en el entorno usando CapsuleCast/BoxCast.
/// Proporciona una API simple para que otros componentes consulten el estado del terreno.
/// Se adapta automáticamente al tipo de collider del enemigo (CapsuleCollider2D o BoxCollider2D).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObstacleDetector : MonoBehaviour
{
    #region Enums
    public enum DetectionMode
    {
        Ground,  // Detecta paredes y vacíos (enemigos terrestres)
        Flying   // Solo detecta paredes (enemigos voladores)
    }

    private enum ColliderType
    {
        Capsule,
        Box
    }
    #endregion

    #region Variables Privadas
    private KinematicMover _mover;
    private Collider2D _collider;
    private ColliderType _colliderType;
    private Vector2 _colliderSize;
    private Vector2 _colliderOffset;

    private Vector2 CurrentPosition => _mover != null ? _mover.Position : (Vector2)transform.position;
    #endregion

    #region Campos Serializados
    [Header("Configuración")]
    [Tooltip("Modo de detección: Ground (paredes + vacíos) o Flying (solo paredes).")]
    [SerializeField] private DetectionMode _detectionMode = DetectionMode.Ground;

    [Header("Distancias de Detección")]
    [Tooltip("Distancia para detectar paredes.")]
    [SerializeField] private float _wallCheckDistance = 0.2f;

    [Tooltip("Distancia para detectar el suelo (solo modo Ground).")]
    [SerializeField] private float _groundCheckDistance = 0.2f;

    [Tooltip("Offset horizontal para la detección de suelo.")]
    [SerializeField] private float _groundCheckOffsetX = 1f;

    [Header("Capas")]
    [Tooltip("La capa (Layer) que representa el suelo y los obstáculos.")]
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Debug")]
    [Tooltip("Mostrar gizmos de debug siempre.")]
    [SerializeField] private bool _alwaysShowGizmos = true;
    #endregion

    #region Propiedades Públicas
    public DetectionMode Mode => _detectionMode;
    #endregion

    #region Ciclo de Vida
    private void Awake()
    {
        _mover = GetComponent<KinematicMover>();
        _collider = GetComponent<Collider2D>();
        
        InitializeColliderInfo();
    }

    private void InitializeColliderInfo()
    {
        if (_collider is CapsuleCollider2D capsule)
        {
            _colliderType = ColliderType.Capsule;
            _colliderSize = capsule.size;
            _colliderOffset = capsule.offset;
        }
        else if (_collider is BoxCollider2D box)
        {
            _colliderType = ColliderType.Box;
            _colliderSize = box.size;
            _colliderOffset = box.offset;
        }
        else
        {
            Debug.LogWarning($"[ObstacleDetector] Collider no soportado en {gameObject.name}. Se requiere CapsuleCollider2D o BoxCollider2D.", this);
            _colliderType = ColliderType.Box;
            _colliderSize = Vector2.one;
            _colliderOffset = Vector2.zero;
        }
    }
    #endregion

    #region API Pública - Detección de Obstáculos

    /// <summary>
    /// Comprueba si hay una pared en la dirección horizontal indicada.
    /// </summary>
    public bool IsWallAhead(float directionX)
    {
        float dirX = Mathf.Sign(directionX);
        return PerformWallCast(new Vector2(dirX, 0));
    }

    /// <summary>
    /// Comprueba si hay una pared en la dirección 2D indicada.
    /// </summary>
    public bool IsWallAhead2D(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return false;
        return PerformWallCast(direction.normalized);
    }

    /// <summary>
    /// Comprueba si hay suelo en la dirección horizontal indicada.
    /// </summary>
    public bool IsGroundAhead(float directionX)
    {
        float dirX = Mathf.Sign(directionX);
        Vector2 checkPosition = CurrentPosition + new Vector2(_groundCheckOffsetX * dirX, 0);
        RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.down, _groundCheckDistance, _obstacleLayer);
        return hit.collider != null;
    }

    /// <summary>
    /// Comprueba si el movimiento horizontal está bloqueado (pared o vacío).
    /// </summary>
    public bool IsBlockedHorizontal(float directionX)
    {
        if (_detectionMode == DetectionMode.Flying)
        {
            return IsWallAhead(directionX);
        }
        return IsWallAhead(directionX) || !IsGroundAhead(directionX);
    }

    /// <summary>
    /// Comprueba si el movimiento 2D está bloqueado.
    /// </summary>
    public bool IsBlocked2D(Vector2 direction)
    {
        return IsWallAhead2D(direction);
    }

    #endregion

    #region Métodos Privados de Detección

    private bool PerformWallCast(Vector2 direction)
    {
        // Tamaño del cast: width reducido a 10%, height igual
        Vector2 castSize = new Vector2(_colliderSize.x * 0.1f, _colliderSize.y);
        
        // Calcular el offset desde el borde del collider (no desde el centro)
        float halfWidth = _colliderSize.x * 0.5f;
        Vector2 edgeOffset = direction * halfWidth;
        
        // Posición del cast: desde el borde del collider
        Vector2 castOrigin = CurrentPosition + _colliderOffset + edgeOffset;
        
        RaycastHit2D hit;
        
        if (_colliderType == ColliderType.Capsule)
        {
            hit = Physics2D.CapsuleCast(
                castOrigin,
                castSize,
                CapsuleDirection2D.Vertical,
                0f, // angle
                direction,
                _wallCheckDistance,
                _obstacleLayer
            );
        }
        else // Box
        {
            hit = Physics2D.BoxCast(
                castOrigin,
                castSize,
                0f, // angle
                direction,
                _wallCheckDistance,
                _obstacleLayer
            );
        }

        return hit.collider != null;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (_alwaysShowGizmos)
        {
            float facing = Mathf.Sign(transform.localScale.x);
            DrawDetectionGizmos(facing);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!_alwaysShowGizmos)
        {
            float facing = Mathf.Sign(transform.localScale.x);
            DrawDetectionGizmos(facing);
            DrawDetectionGizmos(-facing);
        }
    }

    private void DrawDetectionGizmos(float directionX)
    {
        float dirX = Mathf.Sign(directionX);
        Vector3 position = transform.position;
        
        // Tamaño del cast: width reducido a 10%, height igual
        Vector2 castSize = new Vector2(_colliderSize.x * 0.1f, _colliderSize.y);
        
        // Offset desde el borde del collider
        float halfWidth = _colliderSize.x * 0.5f;
        Vector3 direction = new Vector3(dirX, 0, 0);
        Vector3 edgeOffset = direction * halfWidth;
        
        // Posición del cast: desde el borde
        Vector3 castOrigin = position + (Vector3)_colliderOffset + edgeOffset;
        Vector3 endPosition = castOrigin + direction * _wallCheckDistance;

        // Wall cast visualization
        Gizmos.color = Color.red;
        
        if (_colliderType == ColliderType.Capsule)
        {
            DrawWireCapsule(castOrigin, castSize, Color.red);
            DrawWireCapsule(endPosition, castSize, new Color(1f, 0f, 0f, 0.5f));
        }
        else
        {
            DrawWireBox(castOrigin, castSize, Color.red);
            DrawWireBox(endPosition, castSize, new Color(1f, 0f, 0f, 0.5f));
        }

        // Connection line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(castOrigin, endPosition);

        // Ground check (solo en modo Ground)
        if (_detectionMode == DetectionMode.Ground)
        {
            Gizmos.color = Color.blue;
            Vector3 groundCheckPos = position + new Vector3(_groundCheckOffsetX * dirX, 0, 0);
            Gizmos.DrawLine(groundCheckPos, groundCheckPos + Vector3.down * _groundCheckDistance);
            Gizmos.DrawWireSphere(groundCheckPos, 0.05f);
        }
    }

    private void DrawWireCapsule(Vector3 position, Vector2 size, Color color)
    {
        Gizmos.color = color;
        float radius = size.x * 0.5f;
        float height = size.y;
        
        Vector3 top = position + Vector3.up * (height * 0.5f - radius);
        Vector3 bottom = position + Vector3.down * (height * 0.5f - radius);
        
        // Círculos superior e inferior
        DrawWireCircle(top, radius);
        DrawWireCircle(bottom, radius);
        
        // Líneas laterales
        Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
        Gizmos.DrawLine(top + Vector3.left * radius, bottom + Vector3.left * radius);
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments = 16)
    {
        Vector3 prevPoint = center + Vector3.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private void DrawWireBox(Vector3 position, Vector2 size, Color color)
    {
        Gizmos.color = color;
        Vector3 halfSize = size * 0.5f;
        
        Vector3 topLeft = position + new Vector3(-halfSize.x, halfSize.y, 0);
        Vector3 topRight = position + new Vector3(halfSize.x, halfSize.y, 0);
        Vector3 bottomLeft = position + new Vector3(-halfSize.x, -halfSize.y, 0);
        Vector3 bottomRight = position + new Vector3(halfSize.x, -halfSize.y, 0);
        
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }

    #endregion
}
