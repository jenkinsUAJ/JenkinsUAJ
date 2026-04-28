using UnityEngine;
using System.Collections;

/// <summary>
/// Componente para el jugador original. Se suscribe a eventos del RecordingSlotManager
/// para saber cuándo empezar y parar de grabar el rastro de posiciones en el DebugTraceManager.
/// </summary>
public class PlayerTraceRecorder : PausableMonoBehaviour
{
    [Tooltip("Activa o desactiva la grabación del rastro de posición.")]
    [SerializeField] private bool _enableTrace = true;

    // Referencias a los Singletons de los managers. Se buscan automáticamente en Start().
    private RecordingSlotManager _slotManager;
    private DebugTraceManager _traceManager;
    // Bandera para controlar si la corrutina de grabación está activa.
    private bool _isTracing = false;

    /// <summary>
    /// Se llama una vez al inicio del ciclo de vida del componente.
    /// Aquí se obtienen las referencias a los managers y se suscribe a sus eventos.
    /// </summary>
    void Start() {
        if (!_enableTrace) {
            this.enabled = false;
            return;
        }

        _slotManager = RecordingSlotManager.Instance;
        _traceManager = DebugTraceManager.Instance;

        if (_slotManager == null || _traceManager == null) {
            Debug.LogError("[PlayerTraceRecorder] ERROR: Uno de los managers es nulo. " +
                           "Asegúrate de que los Singletons existen en la escena.");
            this.enabled = false;
            return;
        }

        _slotManager.OnRecordingStarted += HandleRecordingStarted;
        _slotManager.OnRecordingStopped += HandleRecordingStopped;
    }

    /// <summary>
    /// Se llama cuando el objeto es destruido. Es vital para desuscribirse de los eventos
    /// y evitar referencias nulas y errores.
    /// </summary>
    protected override void OnDestroy() {
        base.OnDestroy();
        if (_slotManager != null) {
            _slotManager.OnRecordingStarted -= HandleRecordingStarted;
            _slotManager.OnRecordingStopped -= HandleRecordingStopped;
        }
    }

    /// <summary>
    /// Maneja el evento OnRecordingStarted. Inicia la grabación de la traza de posición.
    /// </summary>
    private void HandleRecordingStarted(int slotId) {
        // Llama al manager de trazas para preparar un nuevo rastro.
        _traceManager.StartTrace(slotId);
        _isTracing = true;
    }

    /// <summary>
    /// Maneja el evento OnRecordingStopped. Detiene la grabación de la traza.
    /// </summary>
    private void HandleRecordingStopped(int slotId) {
        _isTracing = false;
    }

    private void FixedUpdate() 
    {
        if (this.IsPaused) return;

        if (_isTracing) {
            // Registramos la posición actual.
            _traceManager.RecordPosition(_slotManager.CurrentRecordingSlot, GetComponent<Rigidbody2D>().position);
        }
    }
}