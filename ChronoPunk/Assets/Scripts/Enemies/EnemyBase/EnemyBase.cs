using UnityEngine;
using Cronopunk.Movement;

/// <summary>
/// Clase base abstracta para todos los enemigos del juego.
/// Proporciona funcionalidad común como
/// pausa, movimiento kinematico y orientación visual.
/// Las clases hijas pueden usar los helpers que necesiten sin estar obligadas a usarlos todos.
/// </summary>
[RequireComponent(typeof(KinematicMover))]
[RequireComponent(typeof(Collider2D))]
public abstract class EnemyBase : PausableMonoBehaviour
{
    #region Componentes Comunes
    /// <summary>
    /// Referencia al componente KinematicMover para gestionar el movimiento físico.
    /// </summary>
    protected KinematicMover Mover { get; private set; }

    /// <summary>
    /// Referencia al componente PlayerDetector para gestionar la detección del jugador.
    /// Puede ser null si el enemigo no utiliza detección de jugador.
    /// </summary>
    protected PlayerDetector Detector { get; private set; }
    #endregion

    #region Campos de Orientación Visual
    /// <summary>
    /// Indica si el enemigo está mirando hacia la derecha.
    /// </summary>
    protected bool _isFacingRight = true;
    #endregion

    #region Ciclo de Vida de Unity

    /// <summary>
    /// Inicializa los componentes comunes. Las clases hijas deben llamar a base.Awake().
    /// </summary>
    protected virtual void Awake()
    {
        Mover = GetComponent<KinematicMover>();
        Detector = GetComponent<PlayerDetector>();

        _isFacingRight = transform.localScale.x >= 0f;
    }

    /// <summary>
    /// Validación inicial. Las clases hijas deben llamar a base.Start() si sobrescriben.
    /// </summary>
    protected virtual void Start()
    {
        // Solo inicializa el detector si existe
        if (Detector != null)
        {
            Detector.Initialize(_isFacingRight);
        }
    }

    /// <summary>
    /// Bucle de física. Gestiona la pausa y actualiza el detector.
    /// Las clases hijas deben llamar a base.FixedUpdate() al inicio de su override.
    /// </summary>
    protected virtual void FixedUpdate()
    {
        // Si está pausado, no ejecutar ninguna lógica.
        if (IsPaused) return;

        // Actualiza el detector de jugador.
        Detector?.Tick(Time.fixedDeltaTime);

        // Ejecuta el comportamiento específico del enemigo.
        ExecuteBehaviour();
    }

    #endregion

    #region Método Abstracto Principal

    /// <summary>
    /// Método abstracto que contiene la lógica de comportamiento específica de cada enemigo.
    /// Se llama automáticamente en FixedUpdate si el juego no está pausado.
    /// </summary>
    protected abstract void ExecuteBehaviour();
    #endregion

    #region Helpers de Detección del Jugador

    /// <summary>
    /// Indica si hay un jugador detectado actualmente.
    /// </summary>
    protected bool HasDetectedPlayer => Detector?.HasDetectedPlayer ?? false;

    /// <summary>
    /// Transform del jugador detectado. Null si no hay jugador.
    /// </summary>
    protected Transform DetectedPlayer => Detector?.DetectedPlayer;
    #endregion

    #region Helpers de Orientación Visual

    /// <summary>
    /// Orienta al enemigo en la dirección horizontal indicada.
    /// </summary>
    /// <param name="directionX">La dirección horizontal (-1 para izquierda, 1 para derecha).</param>
    protected virtual void FaceDirection(float directionX)
    {
        if (directionX > 0 && !_isFacingRight)
        {
            Flip();
        }
        else if (directionX < 0 && _isFacingRight)
        {
            Flip();
        }
    }

    /// <summary>
    /// Orienta al enemigo hacia una posición objetivo.
    /// </summary>
    /// <param name="targetPosition">La posición hacia la que mirar.</param>
    protected virtual void FaceTowards(Vector2 targetPosition)
    {
        float directionX = Mathf.Sign(targetPosition.x - Mover.Position.x);
        FaceDirection(directionX);
    }

    /// <summary>
    /// Invierte la escala horizontal del transform para voltear el sprite.
    /// </summary>
    protected virtual void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;

        // Notifica al detector del cambio de orientación
        Detector?.UpdateFacing(_isFacingRight);
    } 
    #endregion

    #region Helpers de Movimiento

    /// <summary>
    /// Aplica movimiento horizontal al enemigo.
    /// </summary>
    /// <param name="speed">Velocidad de movimiento.</param>
    /// <param name="directionX">Dirección horizontal (-1, 0, 1).</param>
    protected virtual void MoveHorizontally(float speed, float directionX)
    {
        Vector2 moveDelta = new Vector2(directionX * speed * Time.fixedDeltaTime, 0);
        Mover.AddMovement(moveDelta);
    }

    /// <summary>
    /// Aplica movimiento en una dirección 2D al enemigo.
    /// </summary>
    /// <param name="speed">Velocidad de movimiento.</param>
    /// <param name="direction">Dirección normalizada del movimiento.</param>
    protected virtual void MoveInDirection(float speed, Vector2 direction)
    {
        Vector2 moveDelta = direction.normalized * speed * Time.fixedDeltaTime;
        Mover.AddMovement(moveDelta);
    }

    /// <summary>
    /// Mueve al enemigo hacia una posición objetivo.
    /// </summary>
    /// <param name="speed">Velocidad de movimiento.</param>
    /// <param name="targetPosition">Posición objetivo.</param>
    protected virtual void MoveTowards(float speed, Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - Mover.Position).normalized;
        MoveInDirection(speed, direction);
    }
    #endregion
}
