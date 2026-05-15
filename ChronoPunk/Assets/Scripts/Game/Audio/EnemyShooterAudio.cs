using UnityEngine;

public class EnemyShooterAudio : MonoBehaviour
{

    [SerializeField] private AudioSource audioSourceShoot;

    public void PlayShoot()
    {
        if (audioSourceShoot.clip == null)
        {
            Debug.LogWarning("Clip de obtencion de pick-up " + gameObject.name + " no asignado");
            return;
        }

        audioSourceShoot.Play();
    }

}
