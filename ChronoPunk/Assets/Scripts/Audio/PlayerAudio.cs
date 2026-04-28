using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Audio de salto")]
    [SerializeField] private AudioSource audioSourceJump;
    [Header("Audio de disparo")]
    [SerializeField] private AudioSource audioSourceShoot;
    [Header("Audio de muerte")]
    [SerializeField] private AudioSource audioSourceDeath;
    [Header("Audio de andares")]
    [SerializeField] private AudioSource audioSourceWalk;
    [Header("Audio de aterrizaje")]
    [SerializeField] private AudioSource audioSourceLand;
    [Header("Audio de solidificacion")]
    [SerializeField] private AudioSource audioSourceHarden;

    public void PlayJump()
    {
        if (audioSourceJump == null) return;

        if (audioSourceJump.clip == null)
        {
            Debug.LogWarning("Clip de salto del jugador no asignado");
            return;
        }
        audioSourceJump.Play();
    }
    public void PlayDeath()
    {
        if (audioSourceDeath == null) return;
        if (audioSourceDeath.clip == null)
        {
            Debug.LogWarning("Clip de muerte del jugador no asignado");
            return;
        }
        audioSourceDeath.Play();
    }
    public void PlayShoot()
    {
        if (audioSourceShoot == null) return;

        if (audioSourceShoot.clip == null)
        {
            Debug.LogWarning("Clip de disparo del jugador no asignado");
            return;
        }
        audioSourceShoot.Play();
    }

    public void PlayWalk()
    {
        if (audioSourceWalk == null) return;

        if (audioSourceWalk.clip == null)
        {
            Debug.LogWarning("Clip de andares del jugador no asignado");
            return;
        }

        audioSourceWalk.Play();
    }

    public void PlayLand()
    {
        if (audioSourceLand == null) return;

        if (audioSourceLand.clip == null)
        {
            Debug.LogWarning("Clip de aterrizaje del jugador no asignado");
            return;
        }

        audioSourceLand.Play();
    }

    public void PlayHarden()
    {
        if (audioSourceHarden == null) return;

        if (audioSourceHarden.clip == null)
        {
            Debug.LogWarning("Clip de solidificar del jugador no asignado");
            return;
        }

        audioSourceHarden.Play();
    }
}
