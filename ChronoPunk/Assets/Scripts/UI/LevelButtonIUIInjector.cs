using GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Se encarga de crear dentro de un canvas los botones de cada nivel de la selecci�n de niveles asignada en LevelManager.
/// 
/// </summary>
public class LevelButtonIUIInjector : MonoBehaviour
{
    private LevelManager _lm;

    public LevelPopUp levelPopUp;

    public GameObject buttonsContainer;

    private void Start()
    {
        int i = 0;
        _lm = LevelManager.Instance;

        foreach (Level l in _lm.GetLevelsData().levels)
        {
            if(i >= buttonsContainer.transform.childCount) break;
            GameObject buttonObject = buttonsContainer.transform.GetChild(i).gameObject;
            buttonObject.GetComponent<LevelButton>().SetLevelInfo(l, i,levelPopUp);

            i++;
        }
    }

}
