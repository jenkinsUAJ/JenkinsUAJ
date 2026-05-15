using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    // Singleton
    public static TransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [Tooltip("Imagen UI de pantalla completa usada para el fade (debe ser negra)")]
    [SerializeField] private Image fadeImage;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            // Se mantiene entre escenas
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Llama a este método para cargar una nueva escena con transición.
    /// </summary>
    /// <param name="sceneName">Nombre de la escena a cargar (debe estar en Build Settings)</param>
    /// <param name="fadeTime">Tiempo (en segundos) de fade in/out</param>
    /// <param name="blackTime">Tiempo (en segundos) en que se mantiene la pantalla en negro</param>
    public void LoadScene(string sceneName, float fadeTime, float blackTime) {
        StartCoroutine(Transition(sceneName, fadeTime, blackTime));
    }

    private IEnumerator Transition(string sceneName, float fadeTime, float blackTime) {
        // Fade out: de transparente a opaco
        yield return StartCoroutine(Fade(0f, 1f, fadeTime));

        // Mantiene la pantalla en negro durante blackTime segundos
        yield return new WaitForSeconds(blackTime);

        // Carga la nueva escena
        SceneManager.LoadScene(sceneName);

        // Fade in: de opaco a transparente
        yield return StartCoroutine(Fade(1f, 0f, fadeTime));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration) {
        float timer = 0f;
        Color currentColor = fadeImage.color;
        // Aseguramos el valor inicial
        currentColor.a = startAlpha;
        fadeImage.color = currentColor;

        while (timer < duration) {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            fadeImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            yield return null;
        }
        // Forzamos el valor final
        fadeImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, endAlpha);
    }
}
