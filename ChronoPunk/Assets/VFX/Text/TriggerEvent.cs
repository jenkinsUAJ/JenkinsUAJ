using System;
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(Collider2D))]
public class TriggerEvent : MonoBehaviour
{
    public string targetName;
    public UnityEvent<GameObject> onEnterCallback;
    public UnityEvent<GameObject> onExitCallback;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.name == targetName)
        {
            onEnterCallback?.Invoke(collision.gameObject);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.name == targetName)
        {
            onExitCallback?.Invoke(collision.gameObject);
        }
    }
}
