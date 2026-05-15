using System.Security;
using UnityEngine;
/// <summary>
/// Elemento de la escena para asignar una pista de música
/// </summary>
public class MusicSelection : MonoBehaviour
{
    [Tooltip("La canción que sonará en el nivel actual")]
    [SerializeField] private AudioClip song;
    private AudioManager audioManager;

    void Start()
    {
        audioManager = AudioManager.Instance;
        if (audioManager == null)
            Debug.LogWarning("No se ha encontrado AudioManager");
        else if (song != null)
            audioManager.SetSong(song);
    }

}
