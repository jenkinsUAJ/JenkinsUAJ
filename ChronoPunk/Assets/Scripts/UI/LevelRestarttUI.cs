using UnityEngine;
using UnityEngine.InputSystem;

public class LevelResetUI : PausableMonoBehaviour
{
    [SerializeField] public LoadingBarController bar;
    public void OnResetLevel()
    {        
        GameFlow.LevelManager.Instance.RestartLevel();
    }

    public void OnRestartLevelInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            bar.SetPressed(true);
        }
        else if (context.canceled)
        {
            bar.SetPressed(false);
        }
    }
}