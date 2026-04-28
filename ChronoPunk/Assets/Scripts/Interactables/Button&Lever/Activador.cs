using UnityEngine;

public abstract class Activador : MonoBehaviour
{
    /// <summary>
    /// Los objetos activables a ser activados por este activador. (xd)
    /// </summary>
    [SerializeField] private MonoBehaviour[] activables;

    [SerializeField] private VFXLighningHandler[] vfxHandlers;

    public bool alwaysSendTrueToActivators = false;

    //de momento se prioriza esto a alwaysSendTrueToActivators, asi que si ambos estan marcados, solo se hace caso a este
    public bool alwaysSwicthActivableState = false;

    /// <summary>
    /// Variable que guarda el último estado del Activador
    /// </summary>
    public bool isPressed = false;

    /// <summary>
    /// El método para enviar señal de activarse a todos los activables asociados a este activador.
    /// </summary>
    /// <param name="state">El estado de señal enviada. Ej. true -> abrir puerta, false -> cerrar puerta</param>
    /// 

    // para el sfx
    [Header("SFX")]
    [SerializeField] private AudioClip activateClip; 
    [SerializeField] private AudioClip deactivateClip;

    private AudioSource audioSource;
    protected void SendToActivables(bool state)
    {
        foreach (var act in activables) {
            if (act is IActivable activable) {
                activable.Activar(state);
            }
        }

        
    }

    protected void ChangeVFX(bool on)
    {
        foreach (VFXLighningHandler vfx in vfxHandlers)
        {
            if (on)
                vfx.Activate();
            else
                vfx.Deactivate();
        }
    }


    protected void SwitchActivableState()
    {
        foreach (var act in activables)
        {
            if (act is IActivable activable)
            {
                activable.SwicthActivableState();
            }
        }
    }


    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// El método para cambiar automáticamente el estado del Activador a su opuesto.
    /// </summary>
    protected void Switch()
    {        
        isPressed = !isPressed;


        if (alwaysSwicthActivableState)
        {
            SwitchActivableState();
        }
        else if (alwaysSendTrueToActivators)
        {
            SendToActivables(true);
        }
        else
            SendToActivables(!isPressed);
    }

    protected void PlayAudioSFX()
    {
        audioSource.clip = isPressed ? activateClip : deactivateClip;
        audioSource.Play();
    }
}
