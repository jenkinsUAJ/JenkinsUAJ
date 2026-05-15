using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ReplicantIndicator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private ReplayController replayController;

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        replayController = GetComponentInParent<ReplayController>();
    }

    void Start() 
    {
        int slotId = RecordingSlotManager.Instance.CurrentRecordingSlot;
        
        if (replayController != null) {
            slotId = replayController.SlotId;
        }

        spriteRenderer.color = GetColorForSlot(slotId);
    }

    private Color GetColorForSlot(int slotId) {
        // Ejemplo sencillo de paleta:
        switch (slotId) {
            case 0: return Color.blue;
            case 1: return Color.red;
            case 2: return Color.green;
            case 3: return Color.yellow;
            case 4: return Color.cyan;
            case 5: return Color.magenta;
            default: return Color.black;
        }
    }
}
