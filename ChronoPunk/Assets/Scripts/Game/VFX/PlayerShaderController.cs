using UnityEngine;

/// <summary>
/// Clase para manejar los shaders del jugador
/// </summary>
public class PlayerShaderController : MonoBehaviour
{
    [SerializeField] protected ShadowsColor shadowsColorScriptableObject;
    [SerializeField] protected SpriteRenderer colorIndicator;
    [SerializeField] protected SpriteRenderer glowColorIndicator;

    private VFX_Restart vfxrestart;
    protected int shadowNum;
    private void Init(int shadowId)
    {
        vfxrestart.Init(shadowId);
        shadowNum = shadowId;
        ColorSprites();
    }

    protected void ColorSprites()
    {
        Color shadowColor = shadowsColorScriptableObject.GetColor(shadowNum);
        colorIndicator.color = shadowColor;
        float glowAlpha = glowColorIndicator.color.a;
        glowColorIndicator.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, glowAlpha);
    }

    protected virtual void OnEnableController()
    {
        if (!RecordingSlotManager.Instance) return;

        RecordingSlotManager.Instance.OnRecordingStarted -= Init;

    }
    protected virtual void OnDisableController()
    {
        if (!RecordingSlotManager.Instance) return;

        RecordingSlotManager.Instance.OnRecordingStarted -= Init;
    }

    protected virtual void OnStartController()
    {
        vfxrestart = GetComponent<VFX_Restart>();
        if (!RecordingSlotManager.Instance) return;

        RecordingSlotManager.Instance.OnRecordingStarted += Init;
    }

    private void OnEnable()
    {
        OnEnableController();
    }
    private void OnDisable()
    {
        OnDisableController();
    }

    private void Start()
    {
        OnStartController();
    }
}
