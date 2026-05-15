using GameFlow;
using UnityEngine;

public class MainMenuButtonsManager : MonoBehaviour
{
    public void LoadLevelSelection()
    {
        LevelManager.Instance.LoadLevelSelection();
    }

    public void SelectLevel(int i)
    {
        LevelManager.Instance.SelectLevel(i);
    }

    public void PlayButton()
    {
        LevelManager.Instance.SelectLevel(LevelManager.Instance._currentUnlockedLevel);
    }

        public void ExitGame()
    {
        LevelManager.Instance.ExitGame();
    }
}
