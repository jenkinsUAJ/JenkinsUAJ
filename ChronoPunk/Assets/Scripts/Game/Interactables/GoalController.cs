using GameFlow;
using UnityEngine;

/// <summary>
/// Script para el objeto al que hay que llegar para pasar al siguiente nivel
/// </summary>
public class GoalController : MonoBehaviour
{
    /// <summary>
    /// Quien debe alcanzar la meta para superar el nivel.
    /// </summary>
    [SerializeField] GameObject goalTarget;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsGoalReachedByWinner(other))
        {
            //Sacar un mensaje de que has ganado
            //Fade out y cambiar de escena
            Rigidbody2D rigidbody2D = other.gameObject.GetComponent<Rigidbody2D>();
            if (rigidbody2D != null)
            {
                rigidbody2D.linearVelocity = new Vector2(0, 0);
            }
            Cronopunk.Movement.PlayerMovementKinematic movement = other.gameObject.GetComponent<Cronopunk.Movement.PlayerMovementKinematic>();
            if (movement != null)
            {
                movement.enabled = false;
            }
            //AudioManager.Instance.PlayVictory();

            if (TestRecordManager.Instance != null && TestRecordManager.Instance.TryFinalizeRecording())
            {
                return;
            }

            Telemetry.TelemetryDispatch.SendLevelEnd();

            LevelManager.Instance.NextLevel();
        }
        else
        {
            Destroy(other.gameObject);
        }
    }

    private bool IsGoalReachedByWinner(Collider2D other)
    {
        if (other.gameObject.name == goalTarget.name)
        {
            return true;
        }

        // En tests de replay (modo solo lectura), permitimos que una sombra valide el nivel.
        if (TestRecordManager.ForceReadOnlyMode && other.CompareTag("Shadow"))
        {
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (goalTarget == null)
        {
            Debug.LogWarning("No se han seleccionado goalTargets (el player)");
        }
    }
#endif
}
