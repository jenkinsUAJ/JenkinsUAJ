using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class RecordManager : PausableMonoBehaviour
{
    public static RecordManager Instance;

    // Usamos un diccionario para almacenar m�ltiples grabaciones por slot ID.
    public Dictionary<int, List<RecordedInput>> allRecordings = new Dictionary<int, List<RecordedInput>>();

    // Contador de Fixed Frames para una precisi�n determinista
    private int _fixedFrameCounter = 0;

    void Awake() {
        // Singleton
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void FixedUpdate() 
    {
        if (this.IsPaused) return;

        _fixedFrameCounter++;
    }

    public void StartRecording(int slotId) {
        // Si ya existe una grabaci�n en el slot, se sobrescribe.
        if (!allRecordings.ContainsKey(slotId)) {
            allRecordings.Add(slotId, new List<RecordedInput>());
        } else {
            allRecordings[slotId].Clear();
        }

        // Reiniciar el contador de frames para esta nueva grabaci�n.
        _fixedFrameCounter = 0;

        Debug.Log($"Recording started on Slot {slotId}.");
    }

    public void StopRecording(int slotId) {
        if (allRecordings.ContainsKey(slotId)) {
            Debug.Log($"Recording stopped on Slot {slotId}. Total inputs: {allRecordings[slotId].Count}");
        }
    }

    public void RecordMove(int slotId, Vector2 direction) {
        if (!allRecordings.ContainsKey(slotId)) return;

        allRecordings[slotId].Add(new MoveInput {
            fixedFrameStamp = _fixedFrameCounter,
            direction = direction
        });
    }

    public void RecordJump(int slotId, bool isPressed) {
        if (!allRecordings.ContainsKey(slotId)) return;

        allRecordings[slotId].Add(new JumpInput {
            fixedFrameStamp = _fixedFrameCounter,
            isPressed = isPressed
        });
    }

    public void RecordShoot(int slotId) {
        if (!allRecordings.ContainsKey(slotId)) return;

        allRecordings[slotId].Add(new ShootInput {
            fixedFrameStamp = _fixedFrameCounter
        });
    }

    public void RecordStopRecording(int slotId) {
        if (!allRecordings.ContainsKey(slotId)) return;

        allRecordings[slotId].Add(new StopRecordingInput {
            fixedFrameStamp = _fixedFrameCounter
        });
    }


    public bool IsSlotUsed(int slotId) { 
    
        return allRecordings.ContainsKey(slotId);
    }

    public bool DeleteRecording(int slotId)
    {
        return allRecordings.Remove(slotId);
    }


    public void ResetAllRecordings()
    {
        allRecordings.Clear();
    }

}