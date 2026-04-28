using System;
using UnityEngine;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
    public struct EndIterationPreviewSnapshot
    {
        public readonly bool facingLeft;
        public readonly float verticalVelocity;
        public readonly Sprite sprite;

        public EndIterationPreviewSnapshot(bool facingLeft, float verticalVelocity, Sprite sprite)
        {
            this.facingLeft = facingLeft;
            this.verticalVelocity = verticalVelocity;
            this.sprite = sprite;
        }
    }

    public static ReplayManager Instance;

    [Tooltip("El Prefab del personaje que se usar� para la reproducci�n. Debe tener el script ReplayController.")]
    [SerializeField] private GameObject replicantPrefab;

    private List<GameObject> activeReplicants = new List<GameObject>();

    /// <summary>
    /// Se dispara cuando comienza la reproducci�n de un replicante.
    /// Par�metro: slotId del replicante.
    /// </summary>
    public event Action<int> OnReplicantReplayStarted;

    void Awake()
    {
        // Singleton
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

    /// <summary>
    /// Inicia la reproducci�n de TODOS los slots grabados.
    /// </summary>
    public void StartFullReplay()
    {
        if (replicantPrefab == null)
        {
            Debug.LogError("Replicant Prefab no est� asignado en el ReplayManager.");
            return;
        }

        Vector3 playerSpawnPosition = transform.position;
        var playerMovement = FindAnyObjectByType<Cronopunk.Movement.PlayerMovementKinematic>();
        if (playerMovement != null)
        {
            // Usar KinematicMover.Position para mantener determinismo en el replay
            var mover = playerMovement.GetComponent<Cronopunk.Movement.KinematicMover>();
            playerSpawnPosition = (Vector3)mover.Position;
        }

        ClearExistingReplicants();

        var allRecordings = RecordManager.Instance.allRecordings;

        foreach (var recordingEntry in allRecordings)
        {
            int slotId = recordingEntry.Key;
            List<RecordedInput> inputsToReplay = recordingEntry.Value;

            if (inputsToReplay.Count > 0)
            {
                // Instanciamos el replicante.
                GameObject replicant = Instantiate(replicantPrefab, playerSpawnPosition, Quaternion.identity);
                activeReplicants.Add(replicant);

                // Obtenemos su ReplayController y le pasamos los datos que debe reproducir.
                var replayController = replicant.GetComponent<ReplayController>();
                if (replayController != null)
                {
                    replayController.Initialize(slotId, inputsToReplay);
                    // Disparamos el evento de inicio de reproducci�n.
                    OnReplicantReplayStarted?.Invoke(slotId);
                }
                else
                {
                    Debug.LogWarning($"El prefab del replicante no tiene el componente ReplayController en el slot {slotId}.");
                }

                // --- INICIALIZAR EL SISTEMA DE DEBUG ---
                var verifier = replicant.GetComponent<ReplicantPositionVerifier>();
                if (verifier != null)
                {
                    verifier.Initialize(slotId);
                }
            }
        }

        Debug.Log($"Replay iniciado. {activeReplicants.Count} replicantes creados.");
    }

    /// <summary>
    /// Instancia un replicante en la posición indicada y aplica directamente
    /// el resultado de fin de iteración (solidificación o uso de perk).
    /// </summary>
    public void StartEndIterationPreview(int slotId, Vector3 spawnPosition, PlayerPerkController sourcePerkController, EndIterationPreviewSnapshot previewSnapshot)
    {
        if (replicantPrefab == null)
        {
            Debug.LogError("Replicant Prefab no está asignado en el ReplayManager.");
            return;
        }

        GameObject replicant = Instantiate(replicantPrefab, spawnPosition, Quaternion.identity);
        activeReplicants.Add(replicant);

        PlayerPerkController previewPerkController = replicant.GetComponent<PlayerPerkController>();
        if (sourcePerkController != null && sourcePerkController.HasPerk() && previewPerkController != null)
        {
            sourcePerkController.TryCopyPerkTo(previewPerkController);
        }

        var replayController = replicant.GetComponent<ReplayController>();
        if (replayController != null)
        {
            replayController.PlayEndIterationPreview(slotId, previewSnapshot);
            OnReplicantReplayStarted?.Invoke(slotId);
        }
        else
        {
            Debug.LogWarning("El prefab del replicante no tiene ReplayController para la previsualización de fin de iteración.");
        }
    }

    /// <summary>
    /// Destruye todos los replicantes activos en la escena.
    /// </summary>
    public void ClearExistingReplicants()
    {
        foreach (var replicant in activeReplicants)
        {
            Destroy(replicant);
        }
        activeReplicants.Clear();
    }
}
