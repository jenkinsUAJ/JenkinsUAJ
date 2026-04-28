using UnityEngine;
using UnityEngine.VFX;


[RequireComponent(typeof(VFX_Restart))]
public class ShadowShaderController : PlayerShaderController
{
    [SerializeField] private Renderer _spriteRenderer;
    [SerializeField] private VisualEffect _solidificationVFX;
    [SerializeField] private Animator solidifactionShinyAnimator;
    [SerializeField] private AnimationCurve progressCurve;

    private int _frameIndx;
    private int _lastFrameIndx;


    private float progress;
    private float _time;
    private float _minTimeMultiplier;
    private float _maxTimeMultiplier;

    private bool _solidificated = false;

    protected override void OnEnableController()
    {
    }

    protected override void OnDisableController()
    {
    }

    protected override void OnStartController()
    {
    }

    public void Init(int shadowNum, int recordingLength)
    {
        this.shadowNum = shadowNum;
        _spriteRenderer.material.SetColor("_ShadowColor", shadowsColorScriptableObject.GetColor(shadowNum));
        _spriteRenderer.material.SetColor("_OutlineColor", shadowsColorScriptableObject.GetColor(shadowNum));

        _frameIndx = 1;
        _lastFrameIndx = recordingLength;

        GetComponent<VFX_Restart>().Init(shadowNum);

        ColorSprites();
    }

    private void FixedUpdate()
    {
        _time += Mathf.Lerp(_minTimeMultiplier, _maxTimeMultiplier, progress);
        _spriteRenderer.material.SetFloat("_time", _time);

        if (_frameIndx < _lastFrameIndx)
        {
            _frameIndx++;

            progress = Mathf.Clamp01((float)_frameIndx / (float)_lastFrameIndx);
            _spriteRenderer.material.SetFloat("_Progress", progressCurve.Evaluate(progress));
        }
        else if (!_solidificated)
        {
            _solidificationVFX.SetVector4("ParticlesColor", shadowsColorScriptableObject.GetColor(shadowNum));
            _solidificationVFX.SendEvent("Spawn");

            solidifactionShinyAnimator.SetTrigger("Spawn");

            _spriteRenderer.material.SetFloat("_Progress", 1f);
            _solidificated = true;
        }
    }
}
