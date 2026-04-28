using GameFlow;
using TMPro;
using UnityEngine;

public class CurrentLevelHUD : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LevelManager lm = LevelManager.Instance;
        TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();
        text.SetText((lm.GetCurrentLevel() + 1).ToString());
    }
}
