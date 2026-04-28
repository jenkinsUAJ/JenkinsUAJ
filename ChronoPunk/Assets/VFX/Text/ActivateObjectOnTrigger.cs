using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class ActivateObjectOnTrigger : MonoBehaviour
{
    [Tooltip("Tag exacto del objeto que debe entrar en el trigger")]
    [SerializeField] private string targetTag;

    [Tooltip("Objetos que se activarán cuando entre el target")]
    [SerializeField] private List<GameObject> objectsToActivate = new List<GameObject>();

    [Tooltip("Objetos que se desactivarán cuando entre el target")]
    [SerializeField] private List<GameObject> objectsToDeactivate = new List<GameObject>();

    [Tooltip("Si está activo, solo se ejecuta una vez")]
    [SerializeField] private bool oneShot = true;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered && oneShot) return;
        if (!collision.CompareTag(targetTag)) return;

        for (int i = 0; i < objectsToActivate.Count; i++)
        {
            GameObject go = objectsToActivate[i];
            if (go != null)
            {
                go.SetActive(true);
            }
        }

        for (int i = 0; i < objectsToDeactivate.Count; i++)
        {
            GameObject go = objectsToDeactivate[i];
            if (go != null)
            {
                go.SetActive(false);
            }
        }

        hasTriggered = true;
    }
}
