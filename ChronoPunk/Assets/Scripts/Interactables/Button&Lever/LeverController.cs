using UnityEngine;
using System.Collections.Generic;

public class LeverController : Activador
{
    [Tooltip("Animación de la palanca")]
    [SerializeField] private Animator animator;
    [SerializeField] private int telemetryId = -1;
    [Header("Hitbox")]
    [SerializeField] private Transform colliderOffPosition;
    [SerializeField] private Transform colliderOnPosition;

    private List<Transform> playersOnLever = new List<Transform>();
    private float leverWidth;
    private bool _useSounds;


    void Start()
    {
        _useSounds = false;
        ChangeAspect();
        leverWidth = Mathf.Abs(colliderOffPosition.localPosition.x - colliderOnPosition.localPosition.x);
        _useSounds = true;
    }

    /// <summary>
    /// Activa la animación de la palanca y cambia la hitbox en uso (activar/desactivar)
    /// </summary>
    private void ChangeAspect()
    {
        if (isPressed)
        { 
            animator.Play("Palanca_activate");

            if (_useSounds)
            {
                GetComponent<ActivatorAudio>().PlayActivate();
            }
        }
        else
        {
            animator.Play("Palanca_deactivate");

            if (_useSounds)
            {
                GetComponent<ActivatorAudio>().PlayDeactivate();
            }
        }

        GetComponent<BoxCollider2D>().offset = isPressed ? colliderOnPosition.localPosition : colliderOffPosition.localPosition;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<Cronopunk.Movement.PlayerMovementKinematic>() != null &&
            !playersOnLever.Contains(collision.gameObject.transform))
        {
            playersOnLever.Add(collision.gameObject.transform);
            Switch();
            Telemetry.TelemetryDispatch.SendLeverAction(telemetryId);
            ChangeAspect();
            PlayAudioSFX();

            ChangeVFX(isPressed);
        }
    }

    void Update()
    {
        List<Transform> playersToRemove = new List<Transform>();

        foreach (Transform player in playersOnLever)
        {
            if (player != null)
            {
                if (Vector2.Distance(player.position, transform.position) > leverWidth)
                {
                    playersToRemove.Add(player);
                }
            }
        }

        foreach (Transform player in playersToRemove)
        {
            playersOnLever.Remove(player);
        }
    }
}
