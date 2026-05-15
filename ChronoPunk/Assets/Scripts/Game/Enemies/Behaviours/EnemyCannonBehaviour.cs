using UnityEngine;

/// <summary>
/// Gestiona el comportamiento de un cañón estático.
/// No se mueve, solo detecta al jugador en un cono y dispara en una dirección fija.
/// </summary>
[RequireComponent(typeof(Shoot))]
[RequireComponent(typeof(PlayerDetector))]
public class EnemyCannonBehaviour : EnemyBase
{
    #region Enums
    /// <summary>
    /// Estados posibles del cañón.
    /// </summary>
    private enum CannonState
    {
        Idle,       // Esperando, sin jugador detectado
        Shooting    // Disparando al jugador
    }
    #endregion

    #region Campos Serializados
    [Header("Shooting")]
    [SerializeField] private Transform shootingPoint;
    
    [Header("Debug")]
    [SerializeField] private CannonState _currentState;
    #endregion

    #region Componentes
    private Shoot _shoot;
    #endregion

    #region Ciclo de Vida

    protected override void Awake()
    {
        base.Awake();
        _shoot = GetComponent<Shoot>();
    }

    protected override void Start()
    {
        base.Start();

        // Configura la dirección de disparo fija basada en el shootingPoint
        // Si no hay shootingPoint asignado, usa el tranform del enemigo por defecto
        Vector2 aimDirection = shootingPoint != null ? shootingPoint.right : transform.right;
        _shoot.SetAim(aimDirection);

        // Estado inicial: Idle
        TransitionTo(CannonState.Idle);
    }
    #endregion

    #region Comportamiento Principal

    protected override void ExecuteBehaviour()
    {
        switch (_currentState)
        {
            case CannonState.Idle:
                UpdateIdle();
                break;
            case CannonState.Shooting:
                UpdateShooting();
                break;
        }
    }

    private void TransitionTo(CannonState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
    }
    #endregion

    #region Estados - Idle

    private void UpdateIdle()
    {
        if (HasDetectedPlayer)
        {
            TransitionTo(CannonState.Shooting);
        }
    }
    #endregion

    #region Estados - Shooting

    private void UpdateShooting()
    {
        // Si perdió al jugador, vuelve a Idle
        if (!HasDetectedPlayer)
        {
            TransitionTo(CannonState.Idle);
            return;
        }

        // Dispara en la dirección fija
        _shoot.TryShoot();
    }
    #endregion
}
