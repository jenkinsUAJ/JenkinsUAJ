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
        if (other.gameObject.name == goalTarget.name)
        {
            //Sacar un mensaje de que has ganado
            //Fade out y cambiar de escena
            other.gameObject.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0, 0);
            other.gameObject.GetComponent<Cronopunk.Movement.PlayerMovementKinematic>().enabled = false;
            Telemetry.TelemetryDispatch.SendLevelEnd();
            //AudioManager.Instance.PlayVictory();

            LevelManager.Instance.NextLevel();
        }
        else
        {
            Destroy(other.gameObject);
        }
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
