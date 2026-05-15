using UnityEngine;
using Cronopunk.Movement;

[RequireComponent(typeof(KinematicMover))]
public class PerkTwin : PausableMonoBehaviour
{
    [Header("Configuraci�n de Oscilaci�n")]
    [Tooltip("Altura del movimiento de oscilaci�n.")]
    [SerializeField] private float amplitude = 0.5f;
    [Tooltip("Velocidad del movimiento de oscilaci�n.")]
    [SerializeField] private float oscillationSpeed = 2f;

    private KinematicMover _kinematicMover;
    private float elapsedTime;

    private void Awake()
    {
        _kinematicMover = GetComponent<KinematicMover>();
    }

    void FixedUpdate()
    {
        // No moverse si el juego está pausado
        if (IsPaused) return;
        
        elapsedTime += Time.fixedDeltaTime;

        // 1. Calcula la velocidad vertical para este instante exacto.
        // Se usa Coseno, que representa la velocidad de la onda Seno.
        float verticalVelocity = amplitude * oscillationSpeed * Mathf.Cos(elapsedTime * oscillationSpeed);

        // 2. Convierte esa velocidad en un desplazamiento para este fotograma.
        float deltaY = verticalVelocity * Time.fixedDeltaTime;
        
        // 3. Aplica el movimiento directamente.
        _kinematicMover.AddMovement(new Vector2(0f, deltaY));
    }
}