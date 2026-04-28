using UnityEngine;

public class CursorStartupLock : MonoBehaviour
{
    private void Awake()
    {
        ForceCursorOff();
    }

    private void OnEnable()
    {
        ForceCursorOff();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ForceCursorOff();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            ForceCursorOff();
        }
    }

    private static void ForceCursorOff()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }
}
