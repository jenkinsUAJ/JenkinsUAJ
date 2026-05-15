using GameFlow;
using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    //El nivel a cargar de la lista de LevelManger.

    [SerializeField]
    private int _levelToLoad;

    public void SetLevelToLoad(int lvl)
    {
        _levelToLoad = lvl;
    }

    public void LoadLevel()
    {
        LevelManager.Instance.SelectLevel(_levelToLoad);
    }
}
