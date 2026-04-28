using UnityEngine;
using Cronopunk.Movement;

/// <summary>
/// Componente marcador para identificar a entidades que pueden usar globos.
/// Gestiona el estado y la comunicaci�n entre el usuario y el globo.
/// </summary>
public class BalloonUser : MonoBehaviour
{
    // Referencia al globo que el usuario est� ocupando actualmente.
    public HotAirBalloon CurrentBalloon { get; private set; }

    // Componentes del propio usuario que se desactivar�n.
    private PlayerMovementKinematic _playerMovement;

    public bool IsOnBalloon => CurrentBalloon != null;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovementKinematic>();
    }

    /// <summary>
    /// Se llama cuando el usuario se sube a un globo.
    /// </summary>
    /// <param name="balloon">El globo al que se sube.</param>
    public void BoardBalloon(HotAirBalloon balloon)
    {
        if (IsOnBalloon) return; // Ya est� en un globo

        CurrentBalloon = balloon;

        // Desactivamos el movimiento normal del jugador
        if (_playerMovement != null)
        {
            _playerMovement.enabled = false;
        }
    }

    /// <summary>
    /// Se llama cuando el usuario se baja o es expulsado de un globo.
    /// </summary>
    public void ExitBalloon()
    {
        if (!IsOnBalloon) return;

        // Reactivamos el movimiento del jugador
        if (_playerMovement != null)
        {
            _playerMovement.enabled = true;
        }

        CurrentBalloon = null;
    }

    /// <summary>
    /// Expulsa al usuario del globo.
    /// </summary>
    /// <param name="withJump">Si true, aplica un salto al salir. Si false, simplemente cae.</param>
    public void EjectFromBalloon(bool withJump = true)
    {
        if (!IsOnBalloon) return;

        // TODO: Aquí se podría añadir un sonido de expulsión/caída del globo

        // El globo nos libera
        CurrentBalloon.EjectUser();

        // Si se solicita, aplicamos impulso de salto
        if (withJump && _playerMovement != null)
        {
            _playerMovement.ForceJump();
        }
    }
}
