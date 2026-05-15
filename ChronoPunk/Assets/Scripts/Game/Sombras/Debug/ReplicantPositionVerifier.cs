using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente para los replicantes. Comprueba en cada FixedUpdate si su posici�n
/// coincide con la del rastro grabado por el DebugTraceManager.
/// Este sistema de verificaci�n es crucial para asegurar el determinismo
/// del sistema de replay.
/// </summary>
public class ReplicantPositionVerifier : PausableMonoBehaviour
{
    [Tooltip("La diferencia m�xima de posici�n permitida antes de lanzar un error.")]
    [SerializeField] private float tolerance = 0.001f;

    // La lista de posiciones "esperadas" que provienen del rastro del jugador original.
    private List<Vector2> _expectedTrace;
    // El �ndice del frame de FixedUpdate actual que estamos verificando.
    private int _frameIndex = 0;
    // El ID del slot de grabaci�n que estamos verificando.
    private int _slotId = -1;
    // Bandera para saber si el verificador ha sido inicializado y puede empezar a trabajar.
    private bool _isInitialized = false;
    // Evita enviar múltiples eventos por el mismo desvío sostenido.
    private bool _detFailureSent = false;

    /// <summary>
    /// Lo llama el ReplayManager para configurar el verificador.
    /// Es el �nico punto de entrada para inicializar este componente.
    /// </summary>
    /// <param name="slotId">El ID del slot de grabaci�n asociado a este replicante.</param>
    public void Initialize(int slotId)
    {
        _slotId = slotId;
        _detFailureSent = false;

        // Obtenemos el rastro de posiciones del Debug Manager.
        _expectedTrace = DebugTraceManager.Instance.GetTrace(_slotId);

        // Si el rastro no existe o est� vac�o, no hay nada que verificar.
        if (_expectedTrace == null || _expectedTrace.Count == 0)
        {
            return;
        }

        // Marcamos el componente como inicializado, permitiendo que la corrutina comience.
        _isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (this.IsPaused) return;

        if (!_isInitialized || _frameIndex >= _expectedTrace.Count)
        {
            return;
        }

        // Obtenemos las posiciones para la comparaci�n.
        Vector2 expectedPosition = _expectedTrace[_frameIndex];
        Vector2 actualPosition = GetComponent<Rigidbody2D>().position;

        // Calculamos la distancia entre ambas posiciones.
        float distance = Vector2.Distance(expectedPosition, actualPosition);

        // Si la distancia es mayor que la tolerancia, significa que hay una
        // desviaci�n y se ha roto el determinismo.
        if (distance > tolerance)
        {
            if (!_detFailureSent)
            {
                Telemetry.TelemetryDispatch.SendDetFailure(actualPosition, _slotId);
                _detFailureSent = true;
            }

            // Registramos un error detallado en la consola de Unity.
            Debug.LogError($"[DETERMINISM FAIL] Slot: {_slotId} | Frame: {_frameIndex} | " +
                           $"Pos Esperada: {expectedPosition:F4} | " +
                           $"Pos Actual: {actualPosition:F4} | " +
                           $"Error: {distance:F6}");

            // Pausamos el editor para inspeccionar el estado exacto en el momento del fallo.
            //Debug.Break();
        }
        else
        {
            // Si la desviaci�n est� dentro de la tolerancia, lo registramos como un �xito.
            Debug.Log($"[DETERMINISM] Slot: {_slotId} | Frame: {_frameIndex} | " +
                           $"Pos Esperada: {expectedPosition:F4} | " +
                           $"Pos Actual: {actualPosition:F4} | " +
                           $"Error: {distance:F6}");
        }

        // Incrementamos el �ndice del frame para la pr�xima verificaci�n.
        _frameIndex++;
    }
}
