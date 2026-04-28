using UnityEngine;
using Cronopunk.Movement;
using UnityEngine.EventSystems;
using UnityEditor;

[RequireComponent(typeof(KinematicMover), typeof(Rigidbody2D), typeof(Collider2D))]
public class HotAirBalloon : PausableMonoBehaviour
{
    [Header("Configuraci�n de Movimiento")]
    [SerializeField] private float initialAscentSpeed = 0.5f;
    [SerializeField] private float ascentAcceleration = 0.4f;
    [SerializeField] private float maxAscentSpeed = 3f;

    [Header("Movimiento Horizontal (Control)")]
    [SerializeField] private float maxHorizontalSpeed = 3f;
    [SerializeField] private float horizontalAccelerationTime = 0.6f;
    [SerializeField] private float horizontalDecelerationTime = 0.6f;

    [Header("Configuracion del Pasajero")]
    [Tooltip("Posicion relativa al centro del globo donde se sentara el jugador.")]
    [SerializeField] private Vector2 seatOffset = Vector2.zero;

    [Header("Deteccion")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float skinWidth = 0.1f; // Margen de seguridad para evitar penetraciones mínimas

    private KinematicMover _kinematicMover;

    // --- ESTADO DEL PASAJERO ---
    private BalloonUser _currentUser = null;
    private KinematicMover _currentUserMover = null;

    // --- ESTADO DEL MOVIMIENTO ---
    private bool _isAscending = false;
    private float _horizontalInput = 0f;
    private float _currentHorizontalSpeed = 0f;
    private float _currentAscentSpeed = 0f;

    // Variable para SmoothDamp (similar al PlayerMovement)
    private float _velocitySmoothing;

    private bool _invulnerability = true;
    private int _penetrationIterations = 5; // Contador de iteraciones de penetracion

    private void Awake()
    {
        _kinematicMover = GetComponent<KinematicMover>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    /// <summary>
    /// Resuelve penetraciones del globo al instanciarse, basado en el sistema del PlayerMovementKinematic
    /// </summary>
    private void ResolvePenetrations()
    {
        Collider2D balloonCollider = GetComponent<Collider2D>();
        Vector2 resolution = Vector2.zero;

        // Detectar todos los colliders que intersectan con el globo
        Collider2D[] overlapping = Physics2D.OverlapBoxAll(
            balloonCollider.bounds.center,
            balloonCollider.bounds.size,
            0f,
            groundLayer
        );

        // Filtrar para excluir el propio globo y contar penetraciones reales
        int realPenetrations = 0;
        foreach (Collider2D hit in overlapping)
        {
            if (hit != null && hit != balloonCollider)
            {
                ColliderDistance2D distance = Physics2D.Distance(balloonCollider, hit);
                if (distance.isOverlapped)
                {
                    realPenetrations++;
                }
            }
        }

        print($"Total penetraciones reales: {realPenetrations}, Iteraciones restantes: {_penetrationIterations}");

        // Si no hay penetraciones reales, salir exitosamente
        if (realPenetrations == 0)
        {
            _invulnerability = false;
            print("Penetraciones resueltas exitosamente");
            return;
        }

        // Si se agotaron las iteraciones, destruir el globo
        if (_penetrationIterations <= 0)
        {
            print("ADVERTENCIA: No se pudieron resolver las penetraciones. Destruyendo globo.");
            Pop();
            return;
        }

        // Decrementar contador solo cuando hay penetraciones
        _penetrationIterations--;

        foreach (Collider2D hit in overlapping)
        {
            if (hit == null || hit == balloonCollider) continue;

            ColliderDistance2D distance = Physics2D.Distance(balloonCollider, hit);
            if (distance.isOverlapped)
            {
                // Invertir el normal para alejar del obstáculo y usar valor absoluto con skinWidth
                resolution += (Vector2)(-distance.normal * (Mathf.Abs(distance.distance) + skinWidth));
            }
        }

        // Aplicar la resolución si hay penetraciones
        if (resolution.magnitude > 0.001f)
        {
            // Usar KinematicMover para mantener determinismo
            _kinematicMover.SetPosition(_kinematicMover.Position + resolution);
        }
    }

    private void FixedUpdate()
    {
        if (IsPaused) return;

        if (_invulnerability)
        {
            ResolvePenetrations();
        }

        if (!_isAscending) return;

        // Cálculo de velocidad vertical (ascenso)
        _currentAscentSpeed += ascentAcceleration * Time.fixedDeltaTime;
        _currentAscentSpeed = Mathf.Min(_currentAscentSpeed, maxAscentSpeed);

        // Cálculo de velocidad horizontal
        float targetHorizontalSpeed = _horizontalInput * maxHorizontalSpeed;

        // Suavizado de aceleración/deceleración usando SmoothDamp
        _currentHorizontalSpeed = Mathf.SmoothDamp(
            _currentHorizontalSpeed,
            targetHorizontalSpeed,
            ref _velocitySmoothing,
            _horizontalInput != 0f
                ? horizontalAccelerationTime
                : horizontalDecelerationTime
        );

        // Aplicación del movimiento
        Vector2 movementDelta = new Vector2(
            _currentHorizontalSpeed * Time.fixedDeltaTime,
            _currentAscentSpeed * Time.fixedDeltaTime
        );

        // 1. Aplicamos el movimiento al propio globo.
        _kinematicMover.AddMovement(movementDelta);

        // 2. Si tenemos un pasajero, le aplicamos el MISMO movimiento.
        if (_currentUserMover != null)
        {
            _currentUserMover.AddMovement(movementDelta);
        }
    }

    public void EjectUser()
    {
        if (_currentUser != null)
        {
            // Antes de liberar al usuario, transferimos el input horizontal actual
            // al PlayerMovementKinematic para mantener la continuidad del movimiento
            PlayerMovementKinematic playerMovement = _currentUser.GetComponent<PlayerMovementKinematic>();
            if (playerMovement != null)
            {
                playerMovement.SetHorizontalMove(_horizontalInput);
            }

            _currentUser.ExitBalloon();
            _currentUser = null;
            _currentUserMover = null;
            _horizontalInput = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((groundLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            if (!_invulnerability)
            {
                Pop();
            }
            return;
        }

        if (_currentUser == null)
        {
            BalloonUser user = other.GetComponent<BalloonUser>();
            if (user != null && !user.IsOnBalloon)
            {
                _currentUser = user;

                // Obtener el input horizontal actual del usuario antes de subir al globo
                PlayerMovementKinematic playerMovement = user.GetComponent<PlayerMovementKinematic>();
                float currentPlayerInput = 0f;
                if (playerMovement != null)
                {
                    currentPlayerInput = playerMovement.GetCurrentMoveDirection();
                }

                user.BoardBalloon(this);

                _currentUserMover = user.GetComponent<KinematicMover>();
                if (_currentUserMover != null)
                {
                    _currentUserMover.SetPosition(_kinematicMover.Position + seatOffset);
                }

                // Transferir el input del jugador al globo para mantener continuidad
                _horizontalInput = currentPlayerInput;

                // Solo inicializamos el movimiento si es la primera vez que alguien se sube
                if (!_isAscending)
                {
                    _isAscending = true;
                    _currentAscentSpeed = initialAscentSpeed;
                    _currentHorizontalSpeed = 0f;
                }
            }
        }
    }

    private void Pop()
    {
        if (_currentUser != null)
        {
            _currentUser.EjectFromBalloon(true);
        }
        Destroy(gameObject);
    }

    public void SetHorizontalInput(float input)
    {
        if (IsPaused) return;
        if (input > 0)
            _horizontalInput = 1;
        else if (input < 0)
            _horizontalInput = -1;
        else
            _horizontalInput = 0;
    }
}
