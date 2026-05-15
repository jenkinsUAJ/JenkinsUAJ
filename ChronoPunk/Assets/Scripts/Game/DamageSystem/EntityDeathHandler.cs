
using Cronopunk.Movement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(HealthSystem))]
public class EntityDeathHandler : MonoBehaviour
{
    [Header("Acci�n al morir (comportamiento por defecto)")]
    [Tooltip("Si true: destruye el GameObject inmediatamente al morir.")]
    [SerializeField] private bool _destroyOnDeath = true;

    [Tooltip("Si true: desactiva coliders y destruye con delay para permitir reproducir sonidos/anim.")]
    [SerializeField] private bool _gracefulDestroy = false;

    [Tooltip("Si gracefulDestroy, tiempo hasta destruir.")]
    [SerializeField] private float _destroyDelay = 1f;

    private HealthSystem _health;
    private bool _hasHandledDeath;

    private void Awake()
    {
        _health = GetComponent<HealthSystem>();
        _health.OnDeath.AddListener(HandleDeath);
    }

    private void playerChangeScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    void HandleDeath()
    {
        if (_hasHandledDeath)
        {
            return;
        }

        _hasHandledDeath = true;

        if (TryGetComponent<PlayerMovementKinematic>(out var playerMovement))
        {
            playerMovement.enabled = false;
        }

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (TryGetComponent<Collider2D>(out var collider))
        {
            collider.enabled = false;
        }

        // Solo encontramos el PlayerInputHandler en el player
        if (TryGetComponent<PlayerInputHandler>(out var inputHandler))
        {
            inputHandler.enabled = false;

            // Desactivamos el PlayerInput para no leer más inputs una vez el jugador ha muerto
            var playerInput = FindAnyObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.DeactivateInput();
            }
        }

        if (tag == "Player")
        {
            Telemetry.TelemetryDispatch.SendDeath(Telemetry.TelemetryDispatch.ResolvePosition(transform));
            RecordingSlotManager.Instance.StopCurrentRecording();
            GetComponent<PlayerAudio>().PlayDeath();
            Invoke("playerChangeScene", _destroyDelay);
            return;
        }

        if (_gracefulDestroy)
        {
            foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
            Destroy(gameObject, _destroyDelay);
            return;
        }

        if (_destroyOnDeath)
        {
            Destroy(gameObject);
            return;
        }

        enabled = false;
    }

    private void OnDestroy()
    {
        if (_health != null) _health.OnDeath.RemoveListener(HandleDeath);
    }
}
