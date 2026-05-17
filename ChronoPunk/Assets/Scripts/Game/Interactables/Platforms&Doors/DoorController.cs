using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour, IActivable
{
    [Header("Referencias de Renderers")]
    [SerializeField] private SpriteRenderer firstHalf;
    [SerializeField] private SpriteRenderer secondHalf;

    [Header("Sprites Puerta Abierta")]
    [SerializeField] private Sprite openSpriteFirstHalf;
    [SerializeField] private Sprite openSpriteSecondHalf;

    [Header("Sprites Puerta Cerrada")]
    [SerializeField] private Sprite closeSpriteFirstHalf;
    [SerializeField] private Sprite closeSpriteSecondHalf;

    [Header("Configuraci�n Inicial")]
    [SerializeField] private bool startClosed = true;

    private Collider2D _coll;
    [SerializeField] private bool isClosed;

    public bool IsClosed => isClosed;

    private void Awake() {
        _coll = GetComponent<Collider2D>();
    }

    private void Start() {
        isClosed = startClosed;
        _coll.enabled = isClosed;
        firstHalf.sprite = isClosed ? closeSpriteFirstHalf : openSpriteFirstHalf;
        secondHalf.sprite = isClosed ? closeSpriteSecondHalf : openSpriteSecondHalf;
    }

    public void Activar(bool state) {
        isClosed = !state;
        UpdateDoorState();
    }

    public void SwicthActivableState()
    {
        isClosed = !isClosed;
        UpdateDoorState();
    }


    private void UpdateDoorState() {
        if (!isActiveAndEnabled || !gameObject.scene.isLoaded)
        {
            return;
        }

        if (_coll != null && _coll.gameObject != null)
        {
            _coll.enabled = isClosed;
        }
        if (firstHalf != null)
        {
            firstHalf.sprite = isClosed ? closeSpriteFirstHalf : openSpriteFirstHalf;
        }

        if (secondHalf != null)
        {
            secondHalf.sprite = isClosed ? closeSpriteSecondHalf : openSpriteSecondHalf;
        }

        if (isClosed)
        {
            ActivatorAudio activatorAudio = GetComponent<ActivatorAudio>();
            if (activatorAudio != null)
            {
                activatorAudio.PlayDeactivate();
            }
        }
        else
        {
            ActivatorAudio activatorAudio = GetComponent<ActivatorAudio>();
            if (activatorAudio != null)
            {
                activatorAudio.PlayActivate();
            }
        }
    }

    


}
