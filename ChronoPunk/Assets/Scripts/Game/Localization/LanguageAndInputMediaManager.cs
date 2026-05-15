using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public interface ILanguageAndInputMediaListener
{
    void OnLanguageAndInputConfigChanged(LanguageAndInputMediaManager.MediaConfig newConfig);
}

public class LanguageAndInputMediaManager : MonoBehaviour
{
    private const string LanguagePrefKey = "LanguageAndInputMediaManager.Language";
    private const string InputTypePrefKey = "LanguageAndInputMediaManager.InputType";
    private static readonly KeyCode[] JoystickButtons = BuildJoystickButtonCache();
    private static readonly KeyCode[] KeyboardKeys = BuildKeyboardKeyCache();

    public enum Language
    {
        Spanish = 0,
        English = 1
    }

    public enum InputType
    {
        Keyboard = 0,
        XboxGamepad = 1
    }

    public enum AutoInputUpdateCriteria
    {
        LastUsed = 0,
        Connection = 1
    }

    [Serializable]
    public struct MediaConfig
    {
        public Language language;
        public InputType inputType;

        public MediaConfig(Language language, InputType inputType)
        {
            this.language = language;
            this.inputType = inputType;
        }
    }

    public static LanguageAndInputMediaManager Instance { get; private set; }

    [Header("Current Global Config")]
    [SerializeField] private Language currentLanguage = Language.Spanish;
    [SerializeField] private InputType currentInputType = InputType.Keyboard;

    [Header("Automatic Input Detection")]
    [Tooltip("Si esta activo, el manager cambia automaticamente entre teclado y mando al detectar conexion/desconexion.")]
    [SerializeField] private bool autoManageInputType = true;
    [Tooltip("Criterio para actualizar automaticamente el tipo de input.")]
    [SerializeField] private AutoInputUpdateCriteria autoInputUpdateCriteria = AutoInputUpdateCriteria.LastUsed;
    [Tooltip("Intervalo en segundos para comprobar conexion de mandos.")]
    [Min(0.05f)]
    [SerializeField] private float inputDetectionIntervalSeconds = 0.25f;

    private readonly List<ILanguageAndInputMediaListener> listeners = new List<ILanguageAndInputMediaListener>();
    private bool hasUnsavedChanges;
    private bool hasValidationSnapshot;
    private Language lastValidatedLanguage;
    private InputType lastValidatedInputType;
    private bool wasGamepadConnected;
    private bool wasAutoManageInputTypeEnabled;
    private float nextInputDetectionTime;

    public Language CurrentLanguage => currentLanguage;
    public InputType CurrentInputType => currentInputType;
    public MediaConfig CurrentConfig => new MediaConfig(currentLanguage, currentInputType);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadPersistedConfig();
            UpdateValidationSnapshot();
            InitializeAutoInputDetectionState();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        if (Instance == this)
        {
            PersistCurrentConfig();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            PersistCurrentConfig();
            Instance = null;
        }
    }

    private void Update()
    {
        if (!autoManageInputType)
        {
            wasAutoManageInputTypeEnabled = false;
            return;
        }

        if (autoInputUpdateCriteria == AutoInputUpdateCriteria.LastUsed)
        {
            UpdateInputTypeByLastUsed();
            wasAutoManageInputTypeEnabled = true;
            return;
        }

        if (!wasAutoManageInputTypeEnabled)
        {
            wasAutoManageInputTypeEnabled = true;
            wasGamepadConnected = IsAnyGamepadConnected();

            ApplyAutoDetectedInputType(wasGamepadConnected);

            nextInputDetectionTime = Time.unscaledTime + inputDetectionIntervalSeconds;
            return;
        }

        if (Time.unscaledTime < nextInputDetectionTime)
        {
            return;
        }

        nextInputDetectionTime = Time.unscaledTime + inputDetectionIntervalSeconds;
        UpdateInputTypeByConnection();
    }

    private void UpdateInputTypeByConnection()
    {
        bool isGamepadConnected = IsAnyGamepadConnected();
        if (isGamepadConnected == wasGamepadConnected)
        {
            return;
        }

        wasGamepadConnected = isGamepadConnected;
        ApplyAutoDetectedInputType(isGamepadConnected);
    }

    private void UpdateInputTypeByLastUsed()
    {
        if (!TryGetLastUsedInputType(out InputType detectedInputType))
        {
            return;
        }

        SetInputType(detectedInputType);
    }

    private void OnValidate()
    {
        if (!hasValidationSnapshot)
        {
            UpdateValidationSnapshot();
            return;
        }

        bool changed = currentLanguage != lastValidatedLanguage || currentInputType != lastValidatedInputType;
        if (!changed)
        {
            return;
        }

        hasUnsavedChanges = true;
        UpdateValidationSnapshot();

        if (Instance == this)
        {
            NotifyListeners();
        }
    }

    public void SetLanguage(Language language)
    {
        SetCurrentConfig(language, currentInputType);
    }

    public void SetInputType(InputType inputType)
    {
        SetCurrentConfig(currentLanguage, inputType);
    }

    public void SetCurrentConfig(MediaConfig config)
    {
        SetCurrentConfig(config.language, config.inputType);
    }

    public void SetCurrentConfig(Language language, InputType inputType)
    {
        bool changed = currentLanguage != language || currentInputType != inputType;
        if (!changed)
        {
            return;
        }

        currentLanguage = language;
        currentInputType = inputType;
        hasUnsavedChanges = true;
        UpdateValidationSnapshot();
        NotifyListeners();
    }

    public void ForceSaveCurrentConfig()
    {
        PersistCurrentConfig();
    }

    private void LoadPersistedConfig()
    {
        int languageValue = PlayerPrefs.GetInt(LanguagePrefKey, (int)currentLanguage);
        int inputTypeValue = PlayerPrefs.GetInt(InputTypePrefKey, (int)currentInputType);

        if (Enum.IsDefined(typeof(Language), languageValue))
        {
            currentLanguage = (Language)languageValue;
        }

        if (Enum.IsDefined(typeof(InputType), inputTypeValue))
        {
            currentInputType = (InputType)inputTypeValue;
        }

        hasUnsavedChanges = false;
        UpdateValidationSnapshot();
    }

    private void PersistCurrentConfig()
    {
        if (!hasUnsavedChanges)
        {
            return;
        }

        PlayerPrefs.SetInt(LanguagePrefKey, (int)currentLanguage);
        PlayerPrefs.SetInt(InputTypePrefKey, (int)currentInputType);
        PlayerPrefs.Save();
        hasUnsavedChanges = false;
    }

    private void UpdateValidationSnapshot()
    {
        lastValidatedLanguage = currentLanguage;
        lastValidatedInputType = currentInputType;
        hasValidationSnapshot = true;
    }

    private void InitializeAutoInputDetectionState()
    {
        wasAutoManageInputTypeEnabled = autoManageInputType;
        wasGamepadConnected = IsAnyGamepadConnected();
        nextInputDetectionTime = Time.unscaledTime + inputDetectionIntervalSeconds;

        if (autoManageInputType && autoInputUpdateCriteria == AutoInputUpdateCriteria.Connection)
        {
            ApplyAutoDetectedInputType(wasGamepadConnected);
        }
    }

    private void ApplyAutoDetectedInputType(bool isGamepadConnected)
    {
        InputType targetInputType = isGamepadConnected ? InputType.XboxGamepad : InputType.Keyboard;
        SetInputType(targetInputType);
    }

    private static bool IsAnyGamepadConnected()
    {
        string[] joystickNames = Input.GetJoystickNames();
        if (joystickNames == null)
        {
            return false;
        }

        for (int i = 0; i < joystickNames.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(joystickNames[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetLastUsedInputType(out InputType inputType)
    {
        if (WasGamepadUsedThisFrame())
        {
            inputType = InputType.XboxGamepad;
            return true;
        }

        if (WasKeyboardUsedThisFrame())
        {
            inputType = InputType.Keyboard;
            return true;
        }

        inputType = InputType.Keyboard;
        return false;
    }

    private static bool WasGamepadUsedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame ||
                gamepad.buttonNorth.wasPressedThisFrame ||
                gamepad.buttonEast.wasPressedThisFrame ||
                gamepad.buttonWest.wasPressedThisFrame ||
                gamepad.startButton.wasPressedThisFrame ||
                gamepad.selectButton.wasPressedThisFrame ||
                gamepad.leftShoulder.wasPressedThisFrame ||
                gamepad.rightShoulder.wasPressedThisFrame ||
                gamepad.leftStickButton.wasPressedThisFrame ||
                gamepad.rightStickButton.wasPressedThisFrame ||
                gamepad.dpad.up.wasPressedThisFrame ||
                gamepad.dpad.down.wasPressedThisFrame ||
                gamepad.dpad.left.wasPressedThisFrame ||
                gamepad.dpad.right.wasPressedThisFrame)
            {
                return true;
            }

            // Detecta navegacion analogica aunque no haya "button down".
            if (gamepad.leftStick.ReadValue().sqrMagnitude > 0.0225f ||
                gamepad.rightStick.ReadValue().sqrMagnitude > 0.0225f ||
                gamepad.leftTrigger.ReadValue() > 0.15f ||
                gamepad.rightTrigger.ReadValue() > 0.15f)
            {
                return true;
            }
        }
#endif

        for (int i = 0; i < JoystickButtons.Length; i++)
        {
            if (Input.GetKeyDown(JoystickButtons[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool WasKeyboardUsedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

        for (int i = 0; i < KeyboardKeys.Length; i++)
        {
            if (Input.GetKeyDown(KeyboardKeys[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static KeyCode[] BuildJoystickButtonCache()
    {
        List<KeyCode> keys = new List<KeyCode>(8 * 20);

        for (int joystick = 1; joystick <= 8; joystick++)
        {
            for (int button = 0; button <= 19; button++)
            {
                string keyName = $"Joystick{joystick}Button{button}";
                if (Enum.TryParse(keyName, out KeyCode keyCode))
                {
                    keys.Add(keyCode);
                }
            }
        }

        return keys.ToArray();
    }

    private static KeyCode[] BuildKeyboardKeyCache()
    {
        Array keyValues = Enum.GetValues(typeof(KeyCode));
        List<KeyCode> keys = new List<KeyCode>();

        for (int i = 0; i < keyValues.Length; i++)
        {
            KeyCode keyCode = (KeyCode)keyValues.GetValue(i);
            string keyName = keyCode.ToString();

            if (keyCode == KeyCode.None)
            {
                continue;
            }

            if (keyName.StartsWith("Mouse", StringComparison.Ordinal) ||
                keyName.StartsWith("Joystick", StringComparison.Ordinal))
            {
                continue;
            }

            keys.Add(keyCode);
        }

        return keys.ToArray();
    }

    public void RegisterListener(ILanguageAndInputMediaListener listener)
    {
        if (listener == null || listeners.Contains(listener))
        {
            return;
        }

        listeners.Add(listener);
    }

    public void UnregisterListener(ILanguageAndInputMediaListener listener)
    {
        if (listener == null)
        {
            return;
        }

        listeners.Remove(listener);
    }

    private void NotifyListeners()
    {
        MediaConfig config = CurrentConfig;

        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            ILanguageAndInputMediaListener listener = listeners[i];
            UnityEngine.Object unityObject = listener as UnityEngine.Object;

            if (unityObject == null)
            {
                listeners.RemoveAt(i);
                continue;
            }

            listener.OnLanguageAndInputConfigChanged(config);
        }
    }
}
