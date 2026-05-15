using UnityEngine;
using UnityEngine.Audio;

public enum SoundSFX { MECANISMO_ACTIVAR, MECANISMO_DESACTIVAR, JUMP, PLAYER_DEATH, PLAYER_RESTART, SHOOT, MELEE_WALK, ENEMY_HURT, PASAR_NIVEL }

public class AudioManager : MonoBehaviour
{   
    public SaveDataManager.AudioSettings audioSettings;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource audioSource;
    public static AudioManager Instance = null;

    private void LoadAudioSettings()
    {
        audioSettings = SaveDataManager.LoadAudioSettings();
    }

    public void SaveAudioSettings()
    {
        SaveDataManager.PersistAudioSettings(audioSettings);
    }

    // Actualiza el mixer de musica y sonido con el volumen
    public void updateVolume()
    {
        audioMixer.SetFloat("MusicVolume", GetDecibelsFromLinear(audioSettings.musicVolume));
        audioMixer.SetFloat("SFXVolume", GetDecibelsFromLinear(audioSettings.sfxVolume));
    }
    /// <summary>
    /// Devuelve un valor de volumen natural aplicando una curva logarítimica al valor de entrada
    /// </summary>
    /// <param name="value">Numero del 0 (sin volumen) al 1 (máximo)</param>
    /// <returns></returns>
    private float GetDecibelsFromLinear(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        return Mathf.Lerp(-80f, 20f, Mathf.Pow(value, 0.25f));
    }

    public void StopSong()
    {
        musicSource.Pause();
    }
    public void PlaySong()
    {
        musicSource.Stop();
        musicSource.PlayScheduled(AudioSettings.dspTime);
    }

    /// <summary>
    /// "Pone" una canción en el nivel.
    /// Si no hay ninguna puesta o es distinta a la que había, la pone y comienza a reproducir.
    /// </summary>
    /// <param name="song">El temardo a poner</param>
    public void SetSong(AudioClip song)
    {
        if (musicSource.clip != song)
        {
            musicSource.clip = song;
            PlaySong();
        } else if(musicSource.isPlaying == false)
        {
            PlaySong();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //para conservar entre escenas
            gameObject.transform.parent = null;
            LoadAudioSettings();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }
    private void Start()
    {
        updateVolume();
    }
    public void PlayVictory()
    {  
        if (audioSource.clip == null)
        {
            Debug.LogWarning("Clip de victoria no asignado");
            return;
        }
        audioSource.Play();
    }
}