using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Clase para manejar el VFX del restart
/// </summary>
public class VFX_Restart : MonoBehaviour
{
    public GameObject[] prefabShadows;

    private int _shadowNum = 0;

    private VisualEffect _vfxInstance;
    private AudioSource _audioSource;


    public void Init(int shadowNum)
    {
        _shadowNum = shadowNum;

        GameObject vfxGameObject = Instantiate(prefabShadows[_shadowNum]);

        if (vfxGameObject)
        {
            _vfxInstance = vfxGameObject.GetComponent<VisualEffect>();

            _vfxInstance.transform.position = transform.position;

            _vfxInstance.enabled = false;

            _audioSource = _vfxInstance.GetComponent<AudioSource>();
        }
    }
    public void Spawn()
    {
        if (!_vfxInstance)
        {
            _vfxInstance = Instantiate(prefabShadows[_shadowNum]).GetComponent<VisualEffect>();
            _vfxInstance.transform.position = transform.position;
            _audioSource = _vfxInstance.GetComponent<AudioSource>();
        }

        _vfxInstance.transform.position = transform.position;
        _vfxInstance.enabled = true;
        _vfxInstance.Play();

    }
}
