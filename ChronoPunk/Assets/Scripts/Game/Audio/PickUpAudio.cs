using UnityEngine;

public class PickUpAudio : MonoBehaviour
{

    [SerializeField] private AudioSource audioSourcePickUp;

    public void PlayPickUp()
    {
        if (audioSourcePickUp.clip == null)
        {
            Debug.LogWarning("Clip de obtencion de pick-up " + gameObject.name + " no asignado");
            return;
        }

        AudioSource.PlayClipAtPoint(audioSourcePickUp.clip, transform.position);
    }

}
