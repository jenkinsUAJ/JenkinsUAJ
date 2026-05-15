using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum TipoPerk { }
public class GameUI : MonoBehaviour
{
    [SerializeField] private SpriteRenderer perkImageHolder;
    [Header("Setting shadows UI")]
    [SerializeField] private Color currentShadowColor;
    [SerializeField] private Color inactiveShadowColor;
    [SerializeField] private Color activeShadowColor;
    [SerializeField] private GameObject shadowHolder;
    [SerializeField] private GameObject shadowUIPrefab;

    private List<Image> shadowsUI;

    private int numShadowsTotal;
    private int numShadowsActive;

    public static GameUI Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void StartUI(int numShadowsTotal, int numShadowsActive)
    {
        shadowsUI = new List<Image>();

        for (int i = 0; i < numShadowsTotal; i++)
        {
            GameObject shadowUI = Instantiate(shadowUIPrefab, shadowHolder.transform);

            Image shadowUIImage = shadowUI.GetComponent<Image>();
            if (i < numShadowsActive)
                shadowUIImage.color = activeShadowColor;
            else if (i > numShadowsActive)
                shadowUIImage.color = inactiveShadowColor;
            else
                shadowUIImage.color = currentShadowColor;

        }
    }
}
