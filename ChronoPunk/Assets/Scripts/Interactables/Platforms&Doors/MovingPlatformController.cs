using UnityEngine;
using System;
using Cronopunk.Movement;

/// <summary>
/// Controlador de plataformas móviles en 2D.
/// Permite definir un conjunto de puntos de patrulla por los que la plataforma se moverá.
/// Soporta movimiento automático en bucle o activación manual mediante <see cref="Activar"/>.
/// El movimiento se gestiona con un perfil trapezoidal de velocidad (suavizado de aceleración y frenado).
/// </summary>
[RequireComponent(typeof(KinematicMover))]
public class MovingPlatformController : PausableMonoBehaviour, IActivable
{
    [Header("Path Settings")]
    [Tooltip("Puntos por los que pasará la plataforma, en bucle.")]
    [SerializeField] private Transform[] patrolPoints;

    [Tooltip("Índice del punto de patrulla al que se dirigirá primero (0-based).")]
    [SerializeField] private int firstDestinationIndex = 1;

    [Tooltip("OPCIONAL (puede ser null): Posición inicial de la plataforma. Si no se asigna, usa la posición actual en la escena.")]
    [SerializeField] private Transform initialPositionOptional = null;

    [Header("Movement Settings")]
    [Tooltip("Velocidad máxima (unidades/segundo).")]
    [SerializeField] private float speed = 3f;

    [Tooltip("Aceleración máxima (unidades/segundo^2).")]
    [SerializeField] private float acceleration = 5f;

    [Tooltip("Si es verdadero, se mueve automáticamente en bucle.")]
    [SerializeField] private bool automaticMovement = true;

    [Tooltip("Si es verdadero y automaticMovement tambien, se mueve automáticamente desde el inicio ")]
    [SerializeField] private bool startMoving = true;


    private KinematicMover _km;
    private int destinationPointIndex = 0;
    private Vector2 currentVelocity = Vector2.zero;


    public event Action<Vector2> OnPlatformMoved; // Evento que notifica el delta de movimiento

    // === Helpers ===
    private bool HasValidPath => patrolPoints != null && patrolPoints.Length >= 2;
    private bool TargetIndexValid => HasValidPath && destinationPointIndex >= 0 && destinationPointIndex < patrolPoints.Length;
    private Vector2 TargetPos => (Vector2)patrolPoints[destinationPointIndex].position;

    private void Awake() {
        _km = GetComponent<KinematicMover>();
    }

    private void Start() {
        if (!HasValidPath) {
            enabled = false;
            Debug.LogWarning("[MovingPlatform] Se requieren al menos 2 puntos en 'patrolPoints'. Script desactivado.", this);
            return;
        }

        // Validar y ajustar el índice del primer destino
        if (firstDestinationIndex < 0 || firstDestinationIndex >= patrolPoints.Length) {
            Debug.LogWarning($"[MovingPlatform] firstDestinationIndex ({firstDestinationIndex}) fuera de rango. Ajustado a 1.", this);
            firstDestinationIndex = Mathf.Clamp(firstDestinationIndex, 0, patrolPoints.Length - 1);
        }

        // Establecer posición inicial
        if (initialPositionOptional != null) {
            _km.SetPosition(initialPositionOptional.position);
        }
        // Si initialPosition es null, la plataforma mantiene su posición actual en la escena

        // Configurar el primer destino
        if (startMoving) {
            destinationPointIndex = automaticMovement ? firstDestinationIndex : 0;
        }
        else {
            destinationPointIndex = 0;
        }
    }

    private void FixedUpdate() 
    {
        if (this.IsPaused) return;

        if (!HasValidPath || !TargetIndexValid) return;

        Vector2 toTarget = TargetPos - _km.Position;
        float dist = toTarget.magnitude;

        // Salimos si ya estamos en el destino
        if (dist == 0) return;

        // Dirección normalizada hacia el destino
        Vector2 dir = toTarget / dist;

        // Seguridad: nunca permitir velocidad o aceleración negativas
        float vMax = Mathf.Max(0f, speed);
        float aMax = Mathf.Max(0.0001f, acceleration);

        // PERFIL TRAPEZOIDAL:
        // Limitamos la velocidad deseada en función de la distancia restante,
        // para que pueda frenar suavemente al llegar (v = sqrt(2 * a * d)).
        float vDesiredMag = Mathf.Min(vMax, Mathf.Sqrt(2f * aMax * dist));
        Vector2 desiredVel = dir * vDesiredMag;

        // Limitamos el cambio de velocidad por aceleración máxima (dv ≤ a * dt)
        currentVelocity = Vector2.MoveTowards(currentVelocity, desiredVel, aMax * Time.fixedDeltaTime);

        // Paso de integración
        Vector2 step = currentVelocity * Time.fixedDeltaTime;

        // Control de overshoot
        if (step.sqrMagnitude > toTarget.sqrMagnitude) {
            step = toTarget;
            ArriveAtTarget();
        }

        // Aplicamos el movimiento a la pplataforma por medio del KinematicMover
        _km.AddMovement(step);

        // Evento: notificamos el delta de movimiento a los listeners
        OnPlatformMoved?.Invoke(step);
    }

    /// <summary>
    /// Llamado cuando la plataforma alcanza un punto de patrulla.
    /// </summary>
    private void ArriveAtTarget() {
        currentVelocity = Vector2.zero;

        if (automaticMovement) {
            destinationPointIndex = (destinationPointIndex + 1) % patrolPoints.Length;
        }
    }

    /// <summary>
    /// Método público de activación (sobrescrito de <see cref="Activable"/>).
    /// Avanza manualmente al siguiente punto de patrulla.
    /// </summary>
    /// <param name="state">Estado de activación (true/false).</param>
    public void Activar(bool state) {
        if (!HasValidPath) return;
        destinationPointIndex = (destinationPointIndex + 1) % patrolPoints.Length;
    }

    public void SwicthActivableState()
    {
        //TODO : implementar logica SwicthActivableState en plataformas
        automaticMovement = !automaticMovement;
        if(automaticMovement)
        {
            Activar(true); 
        }
    }



    /// <summary>
    /// Dibuja en la vista de escena los puntos de patrulla, conexiones y destino actual.
    /// Facilita la depuración visual del recorrido de la plataforma.
    /// </summary>
    private void OnDrawGizmos() {
        if (!HasValidPath) return;

        for (int i = 0; i < patrolPoints.Length; i++) {
            if (patrolPoints[i] == null) continue;

            // Puntos de patrulla: verde = normales, rojo = destino actual
            if (i == destinationPointIndex) Gizmos.color = Color.red;
            else Gizmos.color = Color.green;

            Gizmos.DrawSphere(patrolPoints[i].position, 0.15f);

            // Líneas de conexión entre puntos
            if (patrolPoints[(i + 1) % patrolPoints.Length] != null) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[(i + 1) % patrolPoints.Length].position);
            }
        }

        // Línea especial desde la plataforma al destino actual
        if (Application.isPlaying && destinationPointIndex < patrolPoints.Length) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_km.Position, patrolPoints[destinationPointIndex].position);
        }
    }


#if UNITY_EDITOR
    /// <summary>
    /// Valida y corrige valores en el inspector.
    /// Previene errores comunes como parámetros negativos o puntos faltantes.
    /// </summary>
    private void OnValidate() {
        if (speed < 0f) speed = 0f;
        if (acceleration < 0f) acceleration = 0f;

        if (patrolPoints == null || patrolPoints.Length < 2) {
            Debug.LogWarning($"[{nameof(MovingPlatformController)}] Asigna al menos 2 puntos en 'patrolPoints'.", this);
            return;
        }

        // Validar firstDestinationIndex
        if (firstDestinationIndex < 0 || firstDestinationIndex >= patrolPoints.Length) {
            Debug.LogWarning($"[{nameof(MovingPlatformController)}] 'firstDestinationIndex' ({firstDestinationIndex}) debe estar entre 0 y {patrolPoints.Length - 1}.", this);
        }

        // Avisar si algún punto está sin asignar
        for (int i = 0; i < patrolPoints.Length; i++) {
            if (patrolPoints[i] == null) {
                Debug.LogWarning($"[{nameof(MovingPlatformController)}] 'patrolPoints[{i}]' es null.", this);
            }
        }
    }
#endif
}
