using GameFlow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    Level _level;

    //El nivel a cargar de la lista de LevelManger.
    private int _levelToLoad;

    LevelPopUp _levelPopUp;

    public GameObject _completed;
    public GameObject _unlocked;
    public GameObject _blocked;

    public void SetLevelInfo(Level level,int levelToLoad,LevelPopUp levelPopUp)
    {
        _level = level;
        _levelToLoad = levelToLoad; 
        _levelPopUp = levelPopUp;   

        GetComponent<Button>().onClick.AddListener(ConfigPopUp);


        //settear sprite 
        switch(level.state)
        {
            case Level.LevelProgressState.LOCKED:
                _completed.SetActive(false);
                _unlocked.SetActive(false);
                _blocked.SetActive(true);
                GetComponent<Button>().targetGraphic = _blocked.GetComponent<Graphic>();
            break;
            case Level.LevelProgressState.UNLOCKED:
                _completed.SetActive(false);
                _unlocked.SetActive(true);
                _blocked.SetActive(false);
                GetComponent<Button>().targetGraphic = _unlocked.GetComponent<Graphic>();
                break;
            case Level.LevelProgressState.COMPLETED:
                _completed.SetActive(true);
                _unlocked.SetActive(false);
                _blocked.SetActive(false);
                GetComponent<Button>().targetGraphic = _completed.GetComponent<Graphic>();
            break;
        }
            
    }


    private void ConfigPopUp()
    {
        if (_level.state == Level.LevelProgressState.LOCKED) return;

        //activar el panel
        _levelPopUp.SetOpenerButton(gameObject);
        _levelPopUp.gameObject.SetActive(true);


        //configurarlo
        _levelPopUp.playButton.SetLevelToLoad(_levelToLoad);
        _levelPopUp.levelName.text = _level.name;

        if (_level.state == Level.LevelProgressState.COMPLETED)
        {
            _levelPopUp.levelPreview.sprite = _level.levelPreview;
            _levelPopUp.textoCompletar.gameObject.SetActive(false);

        }
        else
        {
            _levelPopUp.levelPreview.sprite = null;
            _levelPopUp.textoCompletar.gameObject.SetActive(true);
        }

        if (EventSystem.current != null && _levelPopUp.playButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_levelPopUp.playButton.gameObject);
        }

    }
}