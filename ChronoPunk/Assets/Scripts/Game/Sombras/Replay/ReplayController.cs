using UnityEngine;
using System.Collections.Generic;
using Cronopunk.Movement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ReplayController : PausableMonoBehaviour
{
    private List<RecordedInput> _inputsToReplay;
    private int _currentInputIndex = 0;
    private float _replayFixedFrame = 0;

    private PlayerMovementKinematic _playerMovementComponent;
    private Shoot _shootComponent;
    private BalloonUser _balloonUser;

    private Solidify _solidifyComponent;
    private PlayerPerkController _playerPerkController;
    private AmmoUI _ammoUI;
    private bool _isInitialized = false;

    private int _slotId = -1;
    public int SlotId => _slotId;

    /// <summary>
    /// El ReplayManager llama a este m�todo para entregar los inputs.
    /// </summary>
    public void Initialize(int slotId, List<RecordedInput> inputs)
    {
        this._slotId = slotId;
        _inputsToReplay = inputs;
        CacheReferences();
        _isInitialized = true;

        // initialize shader
        ShadowShaderController shader = GetComponent<ShadowShaderController>();
        if (shader != null)
        {
            int recordingLength = inputs != null && inputs.Count > 0 ? inputs[inputs.Count - 1].fixedFrameStamp : 1;
            shader.Init(slotId, recordingLength);
        }
    }

    public void PlayEndIterationPreview(int slotId, ReplayManager.EndIterationPreviewSnapshot previewSnapshot)
    {
        _slotId = slotId;
        CacheReferences();

        if (_playerMovementComponent != null)
        {
            _playerMovementComponent.ApplyEndIterationPreviewState(
                previewSnapshot.facingLeft,
                previewSnapshot.verticalVelocity,
                previewSnapshot.sprite
            );
        }

        ShadowShaderController shader = GetComponent<ShadowShaderController>();
        if (shader != null)
        {
            shader.Init(slotId, 1);
        }

        ApplyStopRecordingEffects();

        if (gameObject != null)
        {
            IgnoreInitialTriggerOverlapsForPreview();
        }

        this.enabled = false;

        if (_ammoUI != null)
        {
            _ammoUI.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (this.IsPaused) return;

        if (!_isInitialized || _inputsToReplay == null || _currentInputIndex >= _inputsToReplay.Count)
        {
            // Detiene la reproducci�n si no hay nada que hacer.
            if (_isInitialized) 
            {
                this.enabled = false;
                if(_ammoUI != null)
                {
                    _ammoUI.gameObject.SetActive(false); // Desactiva la UI de munición
                }
            } 
            return;
        }

        _replayFixedFrame++;

        // Bucle por si varios inputs ocurren en el mismo frame de f�sica
        while (_currentInputIndex < _inputsToReplay.Count &&
               _replayFixedFrame >= _inputsToReplay[_currentInputIndex].fixedFrameStamp)
        {
            ExecuteInput(_inputsToReplay[_currentInputIndex]);
            _currentInputIndex++;
        }
    }

    private void ExecuteInput(RecordedInput input)
    {
        if (input is MoveInput move)
        {
            if (_balloonUser != null && _balloonUser.IsOnBalloon)
            {
                _balloonUser.CurrentBalloon.SetHorizontalInput(move.direction.x);
            }
            else
            {
                _playerMovementComponent.SetHorizontalMove(move.direction.x);
            }
            Vector2 aimDirection = Shoot.QuantizeToEightDirections(move.direction);
            _shootComponent.SetAim(aimDirection);
        }
        else if (input is JumpInput jump)
        {
            if (_balloonUser != null && _balloonUser.IsOnBalloon)
            {
                if (jump.isPressed)
                {
                    _balloonUser.EjectFromBalloon(true);
                }
            }
            else
            {
                if (jump.isPressed)
                    _playerMovementComponent.TryJump();
                else
                    _playerMovementComponent.ApplyJumpCut();
            }
        }
        else if (input is ShootInput)
        {
            _shootComponent.TryShoot();
        }
        else if (input is StopRecordingInput)
        {
            ApplyStopRecordingEffects();
        }
    }

    private void CacheReferences()
    {
        _playerMovementComponent = GetComponent<PlayerMovementKinematic>();
        _shootComponent = GetComponent<Shoot>();
        _balloonUser = GetComponent<BalloonUser>();

        _solidifyComponent = GetComponent<Solidify>();
        _playerPerkController = GetComponent<PlayerPerkController>();
        _ammoUI = GetComponentInChildren<AmmoUI>();
    }

    private void ApplyStopRecordingEffects()
    {
        if (_playerMovementComponent != null)
        {
            _playerMovementComponent.ForceStopHorizontalImmediately();
        }

        if (_solidifyComponent != null)
        {
            _solidifyComponent.ActivateSolidification();
        }

        if (_playerPerkController != null)
        {
            _playerPerkController.UsePerk();
        }
    }

    private void IgnoreInitialTriggerOverlapsForPreview()
    {
        Collider2D[] ownColliders = GetComponentsInChildren<Collider2D>(true);
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = false;
        filter.useTriggers = true;

        // Buffer local para evitar allocs excesivas; suficiente para el caso de preview.
        Collider2D[] overlaps = new Collider2D[32];

        for (int i = 0; i < ownColliders.Length; i++)
        {
            Collider2D own = ownColliders[i];
            if (own == null || !own.enabled) continue;

            int overlapCount = own.Overlap(filter, overlaps);
            for (int j = 0; j < overlapCount; j++)
            {
                Collider2D other = overlaps[j];
                if (other == null) continue;
                if (!other.isTrigger) continue;
                if (other.transform.root == transform.root) continue;

                // Ignoramos solo triggers ya solapados al instanciar el preview.
                Physics2D.IgnoreCollision(own, other, true);
            }
        }
    }
}
