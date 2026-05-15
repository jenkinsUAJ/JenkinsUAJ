using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int currentRecording = -1;

    void Awake() {
        // Singleton
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Suscribirse al evento cuando se carga una escena
            SceneManager.sceneLoaded += OnSceneLoaded;
        } else {
            Destroy(gameObject);
        }
    }

    void OnDestroy() {
        // Evitar referencias huérfanas si el objeto se destruye
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Esto se llama cada vez que se carga una escena (incluso al reiniciar)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //StartCoroutine(StartRecordingCoroutine());

    }

    private IEnumerator StartRecordingCoroutine() {
        yield return new WaitForFixedUpdate();

        currentRecording++;
        RecordingSlotManager.Instance.SelectAndStartRecording(currentRecording);
        
        ReplayManager.Instance.StartFullReplay();
    }
}
