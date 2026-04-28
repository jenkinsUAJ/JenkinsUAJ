using GameFlow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [SerializeField] GameObject optionsUI;
    [SerializeField] GameObject volumeUI;
    [SerializeField] GameObject controlsUI;
    [SerializeField] private Button backButton;
    [SerializeField] private SelectShadow selectShadow;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider musicSlider;

    private void OnEnable()
    {
        SelectBackButton();
    }

    private void Start()
    {
        if (selectShadow == null)
        {
            selectShadow = FindAnyObjectByType<SelectShadow>(FindObjectsInactive.Include);
        }
        sfxSlider.value = AudioManager.Instance.audioSettings.sfxVolume;
        musicSlider.value = AudioManager.Instance.audioSettings.musicVolume;
    }

    public void ToggleOptionsMenu(InputAction.CallbackContext callback)
    {
        if (!callback.started) return;

        if (!optionsUI.activeInHierarchy)
        {
            Open();
        }
        else
        {
            Return();
        }
    }

    private void HideAll()
    {
        optionsUI.SetActive(false);
        volumeUI.SetActive(false);
        controlsUI.SetActive(false);
    }

    public void ShowControls(bool b)
    {
        controlsUI.SetActive(b);
    }

    public void ShowVolume(bool b)
    {
        volumeUI.SetActive(b);
    }

    public void UpdateSfxVolume(float volume)
    {
        AudioManager.Instance.audioSettings.sfxVolume = volume;
        AudioManager.Instance.updateVolume();
        AudioManager.Instance.SaveAudioSettings();
    }

    public void UpdateMusicVolume(float volume)
    {
        AudioManager.Instance.audioSettings.musicVolume = volume;
        AudioManager.Instance.updateVolume();
        AudioManager.Instance.SaveAudioSettings();
    }

    /// <summary>
    /// El método de regreso al juego
    /// </summary>
    public void Return()
    {
        HideAll();
        if (selectShadow != null)
        {
            selectShadow.optionsMenuOpen = false;
            if (!selectShadow.gameObject.activeSelf)
            {
                PauseManager.Instance.SetPause(false);
            }
        }
        else
        {
            PauseManager.Instance.SetPause(false);
        }
    }

    public void Open()
    {
        optionsUI.SetActive(true);
        PauseManager.Instance.SetPause(true);
        if (selectShadow != null)
            selectShadow.optionsMenuOpen = true;

        SelectBackButton();
    }

    private void SelectBackButton()
    {
        if (EventSystem.current == null || backButton == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(backButton.gameObject);
    }

    public void ExitLevel()
    {
        PlayerPrefs.SetInt("LAST_LEVEL", SceneManager.GetActiveScene().buildIndex);

        PauseManager.Instance.SetPause(false);
        Telemetry.TelemetryDispatch.SendLeftLevel();
        LevelManager.Instance.LoadMainMenu();
    }
}
