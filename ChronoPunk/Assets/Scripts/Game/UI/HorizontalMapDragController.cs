using UnityEngine;
using UnityEngine.EventSystems;

public class HorizontalMapDragController : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform map;
    [SerializeField] private RectTransform leftBound;
    [SerializeField] private RectTransform rightBound;

    [Header("Settings")]
    [SerializeField] private float dragSpeed = 1f;

    private Vector2 lastPointerPosition;

    private Canvas canvas;

    private void Awake()
    {
        canvas = map.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("No Canvas found in parent hierarchy.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        lastPointerPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentPointerPosition = eventData.position;
        Vector2 delta = currentPointerPosition - lastPointerPosition;

        lastPointerPosition = currentPointerPosition;

        float scaleFactor = canvas.scaleFactor;
        float deltaX = (delta.x / scaleFactor) * dragSpeed;

        Vector3 newPosition = map.localPosition;
        newPosition.x += deltaX;

        newPosition.x = ClampToBounds(newPosition.x);

        map.localPosition = newPosition;
    }

    private float ClampToBounds(float targetX)
    {
        float mapHalfWidth = map.rect.width * 0.5f;
        float leftLimit = leftBound.localPosition.x + mapHalfWidth;
        float rightLimit = rightBound.localPosition.x - mapHalfWidth;

        return Mathf.Clamp(targetX, leftLimit, rightLimit);
    }

    /*
    private float ClampToBounds(float targetX)
    {
        float mapWidth = map.rect.width;
        float mapPivotX = map.pivot.x;

        float mapLeftEdge = targetX - (mapWidth * mapPivotX);
        float mapRightEdge = mapLeftEdge + mapWidth;

        float leftBoundRightEdge =
            leftBound.localPosition.x +
            (leftBound.rect.width * (1f - leftBound.pivot.x));

        float rightBoundLeftEdge =
            rightBound.localPosition.x -
            (rightBound.rect.width * rightBound.pivot.x);

        if (mapLeftEdge > leftBoundRightEdge)
        {
            targetX = leftBoundRightEdge + (mapWidth * mapPivotX);
        }

        if (mapRightEdge < rightBoundLeftEdge)
        {
            targetX = rightBoundLeftEdge - (mapWidth * (1f - mapPivotX));
        }

        return targetX;
    }
    */

}
