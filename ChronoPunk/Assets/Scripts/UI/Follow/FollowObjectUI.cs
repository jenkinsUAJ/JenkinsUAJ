using UnityEngine;

[RequireComponent(typeof(RectTransform))]
// Elemento de UI que acompaña a un objeto ej: la UI de munición del jugador
public class FollowObjectUI : MonoBehaviour
{
    [Tooltip("El objeto a seguir")]
    [SerializeField] public Transform target;
    [Tooltip(" El desplazamiento desde el transform del objeto seguido")]
    [SerializeField] public Vector3 displacement;
    private RectTransform rectTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if(rectTransform == null)
        {
            Debug.LogWarning(name + "no tiene un rect transform asociado");
        }
        if(target == null)
        {
            Debug.LogWarning(name + "no tiene un objetivo a seguir");
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(target != null)
        {
            Vector2 point = Camera.main.WorldToScreenPoint(target.position + displacement);
            rectTransform.position = point;
        }
    }

    public void SetFollowTarget(Transform target)
    {
        this.target = target;
    }
}
