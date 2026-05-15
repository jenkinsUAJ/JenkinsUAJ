using UnityEngine;

public class ActivateSecondShadow : MonoBehaviour
{
    void Start()
    {
        RecordingSlotManager.Instance.OnRecordingStarted += handleActivation;
    }

    private void OnDestroy()
    {
        RecordingSlotManager.Instance.OnRecordingStarted -= handleActivation;
    }


    void handleActivation(int n)
    {

        if (RecordingSlotManager.Instance.CurrentRecordingSlot != RecordingSlotManager.Instance.LastRecordedSlot &&
            RecordingSlotManager.Instance.LastRecordedSlot != -1)
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
  
}
