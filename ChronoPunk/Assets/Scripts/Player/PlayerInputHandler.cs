using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Cronopunk.Movement;
using CameraSystem;

using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona todas las entradas del jugador. Act�a como un intermediario entre
/// el componente PlayerInput y los sistemas de juego (PlayerMovement, Shoot).
/// Tambi�n se encarga de comunicarse con el RecordManager para grabar cada acci�n.
/// Cuando una acción de input es efectuada, llama al audioManager para sonorizar
/// </summary>
[RequireComponent(typeof(PlayerMovementKinematic))]
[RequireComponent(typeof(Shoot))]
[RequireComponent(typeof(PlayerAudio))]

public class PlayerInputHandler : PausableMonoBehaviour
{
    [Header("End Iteration Preview")]
    [SerializeField, Min(0f)] private float endIterationPreviewSeconds = 2f;
    [SerializeField, Min(0f)] private float endIterationToShadowMenuBlendSeconds = 2f;
    [SerializeField] private CameraBlendStyle endIterationToShadowMenuBlendStyle = CameraBlendStyle.EaseInOut;
    [SerializeField, Min(0f)] private float endIterationPostTransitionDelaySeconds = 0.2f;
    [SerializeField] private bool allowSkipEndIterationTransition = true;
    [SerializeField] private GameObject endIterationSkipHintUI;

    private PlayerMovementKinematic _playerMovementComponent;
    private Shoot _shootComponent;
    private BalloonUser _balloonUser;

    private RecordManager _recordManager;
    private RecordingSlotManager _slotManager;
    private PlayerAudio _playerAudio;
    private PlayerPerkController _playerPerkController;
    private PerkUI _perkUI;

    // Para evitar grabar inputs redundantes (especialmente con joystick)
    private Vector2 _lastMoveDirection = new Vector2(0f, 0f);

    private bool _isEndingIterationPreview = false;
    private Renderer[] _playerRenderers;
    private Collider2D[] _playerColliders;
    private HealthSystem _playerHealth;


    private void Awake()
    {
        _playerMovementComponent = GetComponent<PlayerMovementKinematic>();
        _shootComponent = GetComponent<Shoot>();
        _balloonUser = GetComponent<BalloonUser>();
        _playerAudio = GetComponent<PlayerAudio>();
        _playerPerkController = GetComponent<PlayerPerkController>();
        _perkUI = GetComponent<PerkUI>();

        _playerRenderers = GetComponentsInChildren<Renderer>(true);
        _playerColliders = GetComponentsInChildren<Collider2D>(true);
        _playerHealth = GetComponent<HealthSystem>();
    }

    protected void Start()
    {
        _recordManager = RecordManager.Instance;
        _slotManager = RecordingSlotManager.Instance;
        SetEndIterationSkipHintVisible(false);
    }

    private bool ShouldIgnoreInput()
    {
        return !isActiveAndEnabled || IsPaused || _isEndingIterationPreview;
    }

    /// <summary>
    /// Se ejecuta cuando se recibe un evento de movimiento.
    /// Procesa el vector de entrada para el apuntado, y lo convierte a una
    /// direcci�n cardinal para el movimiento del jugador y la grabaci�n.
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInput()) return;

        Vector2 moveDirection = context.ReadValue<Vector2>();

        // Grabar la direcci�n de MOVIMIENTO si ha cambiado.
        if (_slotManager.IsRecording && moveDirection != _lastMoveDirection)
        {
            _recordManager.RecordMove(_slotManager.CurrentRecordingSlot, moveDirection);
            _lastMoveDirection = moveDirection;
        }

        if (_balloonUser != null && _balloonUser.IsOnBalloon)
        {
            // Si estamos en un globo, la entrada horizontal controla el globo
            _balloonUser.CurrentBalloon.SetHorizontalInput(moveDirection.x);
        }
        else
        {
            // Delegar la acci�n de MOVIMIENTO al componente de movimiento.
            _playerMovementComponent.SetHorizontalMove(moveDirection.x);
        }

        // Igualamos teclado/mando para que el disparo use el mismo abanico de direcciones.
        Vector2 aimDirection = Shoot.QuantizeToEightDirections(moveDirection);
        _shootComponent.SetAim(aimDirection);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInput()) return;

        // Grabar la acci�n de salto o cortar salto
        if (_slotManager.IsRecording)
        {
            int activeSlot = _slotManager.CurrentRecordingSlot;
            if (context.started)
            {
                _recordManager.RecordJump(activeSlot, true);
            }
            else if (context.canceled)
            {
                _recordManager.RecordJump(activeSlot, false);
            }
        }

        if (_balloonUser != null && _balloonUser.IsOnBalloon)
        {
            if (context.started)
            {
                // Si estamos en un globo, el salto nos hace bajar
                _balloonUser.EjectFromBalloon(true);
            }
        }
        else
        {
            // Delegar la acci�n de salto o cortar salto al sistema de movimiento del jugador
            if (context.started)
            {
                _playerMovementComponent.TryJump();
            }
            else if (context.canceled)
            {
                _playerMovementComponent.ApplyJumpCut();
            }
        }

    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInput()) return;
        if (!context.started) return;

        // Grabar la acci�n de disparo
        if (_slotManager.IsRecording)
        {
            _recordManager.RecordShoot(_slotManager.CurrentRecordingSlot);
        }

        // Delegar la acci�n
        if (_shootComponent.TryShoot())
            _playerAudio.PlayShoot();
    }

    public void OnStopRecording(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreInput()) return;
        if (!context.started) return;
        if (!_slotManager.IsRecording) return;

        int previewSlot = _slotManager.CurrentRecordingSlot;

        // Grabar la acci�n de detener la grabaci�n
        _recordManager.RecordStopRecording(previewSlot);

        _slotManager.StopCurrentRecording();
        Telemetry.TelemetryDispatch.SendEndIteration((Vector2)GetCurrentPlayerPosition(), previewSlot);

        StartCoroutine(PlayEndIterationPreviewAndReload(previewSlot));
    }

    private IEnumerator PlayEndIterationPreviewAndReload(int previewSlot)
    {
        _isEndingIterationPreview = true;

        Vector3 previewSpawnPosition = GetCurrentPlayerPosition();
        ReplayManager.EndIterationPreviewSnapshot previewSnapshot = CaptureEndIterationPreviewSnapshot();

        FreezeCameraOnPosition(previewSpawnPosition);
        HidePlayerForPreview();

        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.StartEndIterationPreview(previewSlot, previewSpawnPosition, _playerPerkController, previewSnapshot);
        }

        yield return new WaitForSeconds(endIterationPreviewSeconds);

        bool transitionedToShadowMenuCamera = false;
        if (CameraSystemManager.Instance != null)
        {
            transitionedToShadowMenuCamera = CameraSystemManager.Instance.TransitionToShadowMenuCamera(
                endIterationToShadowMenuBlendSeconds,
                endIterationToShadowMenuBlendStyle
            );
        }

        if (transitionedToShadowMenuCamera && endIterationToShadowMenuBlendSeconds > 0f)
        {
            SetEndIterationSkipHintVisible(true);
            yield return WaitForEndIterationTransitionAndDelay(
                endIterationToShadowMenuBlendSeconds,
                endIterationPostTransitionDelaySeconds
            );
            SetEndIterationSkipHintVisible(false);
        }
        else if (endIterationPostTransitionDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(endIterationPostTransitionDelaySeconds);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private Vector3 GetCurrentPlayerPosition()
    {
        if (_playerMovementComponent != null)
        {
            KinematicMover mover = _playerMovementComponent.GetComponent<KinematicMover>();
            if (mover != null)
            {
                return mover.Position;
            }
        }

        return transform.position;
    }

    private ReplayManager.EndIterationPreviewSnapshot CaptureEndIterationPreviewSnapshot()
    {
        if (_playerMovementComponent == null)
        {
            return new ReplayManager.EndIterationPreviewSnapshot(false, 0f, null);
        }

        return new ReplayManager.EndIterationPreviewSnapshot(
            _playerMovementComponent.IsFacingLeft(),
            _playerMovementComponent.GetVerticalVelocity(),
            _playerMovementComponent.GetCurrentSprite()
        );
    }

    private void HidePlayerForPreview()
    {
        if (_playerMovementComponent != null)
        {
            _playerMovementComponent.ForceStopHorizontalImmediately();
            _playerMovementComponent.enabled = false;
        }

        if (_shootComponent != null)
        {
            _shootComponent.enabled = false;
        }

        if (_balloonUser != null)
        {
            _balloonUser.enabled = false;
        }

        if (_playerHealth != null)
        {
            _playerHealth.SetInvulnerable(true);
        }

        if (_perkUI != null)
        {
            _perkUI.HidePerkImage();
        }

        for (int i = 0; i < _playerRenderers.Length; i++)
        {
            if (_playerRenderers[i] != null)
            {
                _playerRenderers[i].enabled = false;
            }
        }

        for (int i = 0; i < _playerColliders.Length; i++)
        {
            if (_playerColliders[i] != null)
            {
                //_playerColliders[i].enabled = false;
            }
        }
    }

    private void FreezeCameraOnPosition(Vector3 fixedPosition)
    {
        if (CameraSystemManager.Instance == null) return;

        GameObject anchor = new GameObject("EndIterationPreviewCameraAnchor");
        anchor.transform.position = fixedPosition;
        CameraSystemManager.Instance.SetPlayerTarget(anchor.transform);
    }

    private IEnumerator WaitForEndIterationTransitionAndDelay(float transitionSeconds, float postDelaySeconds)
    {
        float remainingTransition = Mathf.Max(0f, transitionSeconds);
        float remainingPostDelay = Mathf.Max(0f, postDelaySeconds);

        while (remainingTransition > 0f)
        {
            if (allowSkipEndIterationTransition && WasAnySkipInputPressedThisFrame())
            {
                yield break;
            }

            remainingTransition -= Time.deltaTime;
            yield return null;
        }

        while (remainingPostDelay > 0f)
        {
            if (allowSkipEndIterationTransition && WasAnySkipInputPressedThisFrame())
            {
                yield break;
            }

            remainingPostDelay -= Time.deltaTime;
            yield return null;
        }
    }

    private bool WasAnySkipInputPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Gamepad.current == null)
        {
            return false;
        }

        foreach (InputControl control in Gamepad.current.allControls)
        {
            if (control is ButtonControl button && button.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private void SetEndIterationSkipHintVisible(bool isVisible)
    {
        if (endIterationSkipHintUI == null) return;
        endIterationSkipHintUI.SetActive(isVisible);
    }
}
