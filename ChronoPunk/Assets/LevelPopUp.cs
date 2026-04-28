using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelPopUp : MonoBehaviour
{
    public Text levelName;
    public PlayButton playButton;
    public Image levelPreview;
    public GameObject textoCompletar;

    private GameObject _openerButton;

    public void SetOpenerButton(GameObject openerButton)
    {
        _openerButton = openerButton;
    }

    private void OnDisable()
    {
        if (EventSystem.current != null && _openerButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_openerButton);
        }
    }
}
