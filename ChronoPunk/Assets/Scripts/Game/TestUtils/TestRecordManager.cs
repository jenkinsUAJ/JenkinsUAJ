using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor runtime de la herramienta de test recording.
/// Captura la grabación activa desde RecordManager, la guarda a disco y expone la carga para los tests.
/// </summary>
public class TestRecordManager : MonoBehaviour
{
    public const string EditorPrefsEnabledKey = "ChronoPunk.TestRecordManager.Enabled";
    public static bool ForceReadOnlyMode { get; private set; }

    public static TestRecordManager Instance { get; private set; }

    public bool IsRecordingSession { get; private set; }
    public string ActiveSceneName { get; private set; }
    public string CurrentFilePath { get; private set; }
    public TestRecordingFileData LastSavedRecording { get; private set; }
    public TestRecordingFileData LastLoadedRecording { get; private set; }

    private bool _hasFinalizedRecording;

    //se llama automaticamente al cargar una escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapIfRequested()
    {
        //si no estamos en modo record salimos, si estamos, marcamos el editor prefs a false y hacemos lo que toque 


        if (ForceReadOnlyMode)
        {
            return;
        }

#if UNITY_EDITOR
        if (!UnityEditor.EditorPrefs.GetBool(EditorPrefsEnabledKey, false))
        {
            return;
        }

        //poner el bool a false para que no afecte a la siguiente iteracion
        UnityEditor.EditorPrefs.SetBool(EditorPrefsEnabledKey, false);
#else
        return;
#endif

        EnsureInstance();
        Instance.BeginRecordingSession(SceneManager.GetActiveScene().name);
    }

    //crea artificialmente una instancia del manager
    public static TestRecordManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject host = new GameObject(nameof(TestRecordManager));
        return host.AddComponent<TestRecordManager>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void BeginRecordingSession(string sceneName)
    {
        if (ForceReadOnlyMode)
        {
            Debug.Log("[TestRecordManager] Read-only mode activo: se ignora el inicio de grabación.");
            return;
        }

        ActiveSceneName = sceneName;
        CurrentFilePath = TestLevelCatalog.GetRecordingFilePath(sceneName);
        IsRecordingSession = true;
        _hasFinalizedRecording = false;
        Debug.Log($"[TestRecordManager] Recording enabled for {sceneName}.");

        StartCoroutine(EnsureBaselinePlayerRecording());
    }

    private IEnumerator EnsureBaselinePlayerRecording()
    {
        float remainingSeconds = 5f;

        while (remainingSeconds > 0f && (RecordingSlotManager.Instance == null || RecordManager.Instance == null))
        {
            remainingSeconds -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!IsRecordingSession)
        {
            yield break;
        }

        RecordingSlotManager slotManager = RecordingSlotManager.Instance;
        if (slotManager == null)
        {
            Debug.LogWarning("[TestRecordManager] No se pudo iniciar grabación base: RecordingSlotManager no disponible.");
            yield break;
        }

        if (slotManager.IsRecording)
        {
            yield break;
        }

        if (slotManager.MaxSlots <= 0)
        {
            Debug.LogWarning("[TestRecordManager] No se pudo iniciar grabación base: maxSlots <= 0.");
            yield break;
        }

        slotManager.SelectAndStartRecording(0);
        Debug.Log("[TestRecordManager] Grabación base iniciada en slot 0.");
    }

    public bool TryFinalizeRecording()
    {
        // Protección doble: nunca guardar si estamos en modo de solo lectura
        if (ForceReadOnlyMode)
        {
            return false;
        }

        if (!IsRecordingSession || _hasFinalizedRecording)
        {
            return false;
        }

        if (RecordingSlotManager.Instance != null && RecordingSlotManager.Instance.IsRecording)
        {
            RecordingSlotManager.Instance.StopCurrentRecording();
        }

        // Verificación final: si de alguna forma llegamos aquí con ForceReadOnlyMode true, no guardamos
        if (!ForceReadOnlyMode)
        {
            TestRecordingFileData recording = CaptureCurrentRecording();
            SaveRecording(recording);
            LastSavedRecording = recording;
            Debug.Log($"[TestRecordManager] Recording saved to {CurrentFilePath}.");
        }

        IsRecordingSession = false;
        _hasFinalizedRecording = true;

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
    #endif

        return true;
    }

    public static void EnableReadOnlyModeForTests()
    {
        ForceReadOnlyMode = true;

#if UNITY_EDITOR
        // Limpiar EditorPrefs para evitar que BootstrapIfRequested cree instancia
        UnityEditor.EditorPrefs.SetBool(EditorPrefsEnabledKey, false);
#endif

        if (Instance != null)
        {
            Instance.CancelRecordingSession();
        }
    }

    public static void DisableReadOnlyModeForTests()
    {
        ForceReadOnlyMode = false;
    }

    public static void ResetForTests()
    {
        ForceReadOnlyMode = false;

#if UNITY_EDITOR
        UnityEditor.EditorPrefs.SetBool(EditorPrefsEnabledKey, false);
#endif

        if (Instance != null)
        {
            Instance.CancelRecordingSession();
            Object.Destroy(Instance.gameObject);
        }
    }

    private void CancelRecordingSession()
    {
        IsRecordingSession = false;
        _hasFinalizedRecording = false;

        if (RecordingSlotManager.Instance != null && RecordingSlotManager.Instance.IsRecording)
        {
            RecordingSlotManager.Instance.StopCurrentRecording();
        }
    }

    public TestRecordingFileData CaptureCurrentRecording()
    {
        TestRecordingFileData recording = new TestRecordingFileData
        {
            sceneName = ActiveSceneName,
            createdAtUtc = DateTime.UtcNow.ToString("o")
        };

        Dictionary<int, List<RecordedInput>> allRecordings = RecordManager.Instance != null
            ? RecordManager.Instance.allRecordings
            : null;

        if (allRecordings == null)
        {
            return recording;
        }

        foreach (KeyValuePair<int, List<RecordedInput>> entry in allRecordings)
        {
            TestRecordingSlotData slotData = new TestRecordingSlotData
            {
                slotId = entry.Key,
                inputs = new List<TestRecordedInputData>()
            };

            List<RecordedInput> inputs = entry.Value;
            if (inputs != null)
            {
                for (int i = 0; i < inputs.Count; i++)
                {
                    TestRecordedInputData inputData = TestRecordedInputData.FromRecordedInput(inputs[i]);
                    if (inputData != null)
                    {
                        slotData.inputs.Add(inputData);
                    }
                }
            }

            recording.slots.Add(slotData);
        }

        return recording;
    }

    public void SaveRecording(TestRecordingFileData recording)
    {
        // Protección: no guardar nunca si estamos en modo de solo lectura (tests)
        if (ForceReadOnlyMode)
        {
            Debug.LogWarning("[TestRecordManager] Intento de guardar en modo solo lectura. Se ignora el guardado.");
            return;
        }

        if (recording == null)
        {
            return;
        }

        string directory = TestLevelCatalog.GetRecordingDirectory();
        Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(recording, true);
        File.WriteAllText(CurrentFilePath, json);
    }

    public static TestRecordingFileData LoadRecordingFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<TestRecordingFileData>(json);
    }

    public static bool TryLoadRecordingForScene(string sceneName, out TestRecordingFileData recording)
    {
        string filePath = TestLevelCatalog.GetRecordingFilePath(sceneName);
        recording = LoadRecordingFromFile(filePath);
        return recording != null;
    }

    public static void PreparePlayback(TestRecordingFileData recording)
    {
        TestRecordManager manager = EnsureInstance();
        manager.LastLoadedRecording = recording;
    }
}