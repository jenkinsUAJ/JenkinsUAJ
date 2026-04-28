using UnityEngine;

public class ButtonController : Activador
{
    [SerializeField] private Animator animator;
    [SerializeField] private int telemetryId = -1;
    private int _nUsers = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Rock") || other.CompareTag("Shadow"))
        {
            _nUsers++;
            if (_nUsers == 1)
            {

                if (alwaysSwicthActivableState)
                {
                    SwitchActivableState();
                }
                else
                {
                    SendToActivables(true);
                }
                Telemetry.TelemetryDispatch.SendButtonPress(telemetryId);
                GetComponent<ActivatorAudio>().PlayActivate();
                ChangeVFX(true);
                animator.Play("Button_activate");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<Cronopunk.Movement.PlayerMovementKinematic>() != null)
        {
            _nUsers--;
            if (_nUsers == 0)
            {
                if (alwaysSwicthActivableState)
                {
                    SwitchActivableState();
                }
                else
                {
                    SendToActivables(false);
                }

                GetComponent<ActivatorAudio>().PlayDeactivate();
                ChangeVFX(false);
                animator.Play("Button_deactivate");
            }
        }
    }
}
