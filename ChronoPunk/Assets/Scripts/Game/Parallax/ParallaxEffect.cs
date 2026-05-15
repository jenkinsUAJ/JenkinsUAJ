using UnityEngine;
using UnityEngine.UI;

public class ParallaxEffect : MonoBehaviour
{
    private Transform cameraTransform;
    
    private Vector3 lastCameraPosition;

    private float offsetX;
    private float offsetY;
    
    private void Start()
    {
        cameraTransform = Camera.main.transform;
        
        lastCameraPosition = cameraTransform.position;
        offsetX = 0;
        offsetY = 0;
    }

    private void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        offsetX += deltaMovement.x;

        if ((offsetY + deltaMovement.y) > 0)
            offsetY += deltaMovement.y;
        
        foreach (Image layer in GetComponentsInChildren<Image>())
        {
            layer.material.SetFloat("_OffsetX", offsetX);
            layer.material.SetFloat("_OffsetY", offsetY);
        }
        
        lastCameraPosition = cameraTransform.position;
    }
}