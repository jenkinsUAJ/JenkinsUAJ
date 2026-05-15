using UnityEngine;

public class ActivatorAudio : MonoBehaviour
{

    [SerializeField] private AudioSource audioSourceActivate;
    [SerializeField] private AudioSource audioSourceDeactivate;

    public void PlayActivate()
    {
        if (audioSourceActivate.clip == null)
        {
            Debug.LogWarning("Clip de desactivación del objeto " + gameObject.name + " no asignado");
            return;
        }
        audioSourceActivate.Play();
    }
    public void PlayDeactivate()
    {
        if (audioSourceDeactivate.clip == null)
        {
            Debug.LogWarning("Clip de desactivación del objeto " + gameObject.name + " no asignado");
            return;
        }
        audioSourceDeactivate.Play();
    }

}
