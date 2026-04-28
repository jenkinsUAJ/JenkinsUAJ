using System;
using System.IO;
using GameFlow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RecordingSlotManager : MonoBehaviour
{
    public static RecordingSlotManager Instance;

    [Tooltip("N�mero m�ximo de slots de grabaci�n disponibles.")]
    [SerializeField] private int maxSlots = 5;

    [Tooltip("Slot que se est� grabando actualmente.")]
    [SerializeField][ReadOnly] private int currentRecordingSlot = -1; // -1 significa que ninguno est� activo

    [Tooltip("�ltimo slot en el que se guard� una grabaci�n.")]
    [SerializeField][ReadOnly] private int lastRecordedSlot = -1;


    [SerializeField] private LevelsMaxSlots levelsMaxSlots;


    public int MaxSlots => maxSlots;
    public int CurrentRecordingSlot => currentRecordingSlot;
    public int LastRecordedSlot => lastRecordedSlot;
    public bool IsRecording => currentRecordingSlot != -1;

    /// <summary>
    /// Se dispara cuando comienza una grabaci�n en un slot.
    /// Par�metro: slotId
    /// </summary>
    public event Action<int> OnRecordingStarted;

    /// <summary>
    /// Se dispara cuando se detiene la grabaci�n.
    /// Par�metro: slotId (el que acaba de detenerse)
    /// </summary>
    public event Action<int> OnRecordingStopped;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- M�TODOS PARA SER USADOS POR LA UI Y EL JUEGO ---

    /// <summary>
    /// Inicia la grabaci�n en un slot espec�fico. Si ya se est� grabando, primero detiene la anterior.
    /// </summary>
    public void SelectAndStartRecording(int slotId)
    {
        if (slotId < 0 || slotId >= maxSlots)
        {
            Debug.LogError($"Slot ID {slotId} is out of range.");
            return;
        }

        if (IsRecording)
        {
            StopCurrentRecording();
        }

        currentRecordingSlot = slotId;
        RecordManager.Instance.StartRecording(currentRecordingSlot);
        Telemetry.TelemetryDispatch.SendShadowSelect(currentRecordingSlot);

        // Notificar que se ha iniciado la grabaci�n.
        OnRecordingStarted?.Invoke(currentRecordingSlot);
    }

    /// <summary>
    /// Detiene la grabaci�n que est� activa actualmente.
    /// </summary>
    public void StopCurrentRecording()
    {
        if (!IsRecording) return;

        int stopped = currentRecordingSlot;
        RecordManager.Instance.StopRecording(currentRecordingSlot);
        lastRecordedSlot = stopped;
        currentRecordingSlot = -1;

        // Notificar que se ha detenido la grabaci�n.
        OnRecordingStopped?.Invoke(stopped);
    }




    private void OnEnable()
    {
        // Nos suscribimos al evento de cambio de escena
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDisable()
    {
        // Siempre es buena pr�ctica desuscribirse
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        SetMaxSlots(newScene);
    }

    private void SetMaxSlots(Scene scene)
    {
        if (levelsMaxSlots == null)
        {
            Debug.LogWarning("LevelsMaxSlots no est� asignado.");
            return;
        }

        string scenePath = scene.path;
        string sceneName = scene.name;

        foreach (var level in levelsMaxSlots.levels)
        {
            if (level.scenePath == scenePath)
            {
                maxSlots = level.maxSlots;
                Debug.Log($"Set maxSlots = {maxSlots} para la escena {sceneName}");
                return;
            }

            // Fallback para datos antiguos: si no hay ruta guardada, comparamos por nombre.
            if (string.IsNullOrEmpty(level.scenePath))
            {
                continue;
            }

            string configuredName = Path.GetFileNameWithoutExtension(level.scenePath);
            if (configuredName == sceneName)
            {
                maxSlots = level.maxSlots;
                Debug.Log($"Set maxSlots = {maxSlots} para la escena {sceneName}");
                return;
            }
        }

        Debug.LogWarning($"No se encontr� configuraci�n para la escena {sceneName}");
    }



    public bool IsSlotUsed(int slotId)
    {
        return RecordManager.Instance.IsSlotUsed(slotId);
    }

    public bool DeleteSlotRecording(int slotId)
    {
        if (slotId < 0 || slotId >= maxSlots)
        {
            return false;
        }

        if (currentRecordingSlot == slotId)
        {
            StopCurrentRecording();
        }

        bool deleted = RecordManager.Instance.DeleteRecording(slotId);
        if (deleted && lastRecordedSlot == slotId)
        {
            lastRecordedSlot = -1;
        }

        return deleted;
    }

    public void ResetSlots()
    {
        StopCurrentRecording();

        lastRecordedSlot = -1;

        RecordManager.Instance.ResetAllRecordings();
    }
}

// Atributo para hacer un campo de solo lectura en el inspector.
public class ReadOnlyAttribute : PropertyAttribute { }
