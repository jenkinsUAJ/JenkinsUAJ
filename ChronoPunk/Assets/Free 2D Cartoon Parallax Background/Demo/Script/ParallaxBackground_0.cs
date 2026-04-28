using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground_0 : MonoBehaviour
{
    public bool Camera_Move;
    public float Camera_MoveSpeed = 1.5f;

    [Header("Layer Setting")]
    public float[] Layer_Speed = new float[7];
    public GameObject[] Layer_Objects = new GameObject[7];

    [Header("Zoom Setting")]
    public float MaxZoomOrthoSize = 20f;

    private Transform _camera;
    private float[] startPos = new float[7];
    private float _cameraStartX;
    private Camera _cameraData;
    private float[] boundSizeX = new float[7];
    private Vector3[] baseScale = new Vector3[7];
    private float[] fitScaleAtMaxZoom = new float[7];

    void Start()
    {
        if (Camera.main == null)
        {
            enabled = false;
            return;
        }

        _camera = Camera.main.transform;
        _cameraData = Camera.main;
        _cameraStartX = _camera.position.x;

        for (int i = 0; i < 5; i++)
        {
            if (Layer_Objects[i] == null)
            {
                startPos[i] = 0f;
                continue;
            }

            Vector3 localPos = Layer_Objects[i].transform.localPosition;
            Layer_Objects[i].transform.localPosition = new Vector3(0f, localPos.y, localPos.z);
            startPos[i] = 0f;

            baseScale[i] = Layer_Objects[i].transform.localScale;
            SpriteRenderer renderer = Layer_Objects[i].GetComponent<SpriteRenderer>();
            boundSizeX[i] = renderer != null && renderer.sprite != null ? renderer.sprite.bounds.size.x : 1f;
            fitScaleAtMaxZoom[i] = GetFitScaleForMaxZoom(renderer, baseScale[i]);
        }
    }

    void Update()
    {
        //Moving camera
        if (Camera_Move)
        {
            _camera.position += Vector3.right * Time.deltaTime * Camera_MoveSpeed;
        }
        for (int i = 0; i < 5; i++)
        {
            if (Layer_Objects[i] == null)
            {
                continue;
            }

            float zoomScale = GetLayerZoomScale(i);
            Layer_Objects[i].transform.localScale = baseScale[i] * fitScaleAtMaxZoom[i] * zoomScale;

            float cameraDeltaX = _camera.position.x - _cameraStartX;
            float temp = (cameraDeltaX * (1 - Layer_Speed[i]));
            float localParallaxX = -temp;
            float currentLayerWidth = boundSizeX[i] * Mathf.Abs(Layer_Objects[i].transform.localScale.x);

            if (temp > startPos[i] + currentLayerWidth)
            {
                startPos[i] += currentLayerWidth;
            }
            else if (temp < startPos[i] - currentLayerWidth)
            {
                startPos[i] -= currentLayerWidth;
            }

            Vector3 localPos = Layer_Objects[i].transform.localPosition;
            Layer_Objects[i].transform.localPosition = new Vector3(startPos[i] + localParallaxX, localPos.y, localPos.z);

        }
    }

    private float GetLayerZoomScale(int layerIndex)
    {
        if (_cameraData == null || !_cameraData.orthographic || MaxZoomOrthoSize <= 0f)
        {
            return 1f;
        }

        float effectiveOrtho = Mathf.Min(_cameraData.orthographicSize, MaxZoomOrthoSize);
        float fullFollowScale = effectiveOrtho / Mathf.Max(0.0001f, MaxZoomOrthoSize);
        float zoomWeight = Mathf.Clamp01(Layer_Speed[layerIndex]);

        return Mathf.Lerp(1f, fullFollowScale, zoomWeight);
    }

    private float GetFitScaleForMaxZoom(SpriteRenderer renderer, Vector3 currentBaseScale)
    {
        if (_cameraData == null || !_cameraData.orthographic || MaxZoomOrthoSize <= 0f)
        {
            return 1f;
        }

        float cameraHeightAtMax = 2f * MaxZoomOrthoSize;
        float cameraWidthAtMax = cameraHeightAtMax * _cameraData.aspect;

        float spriteWidth = 1f;
        float spriteHeight = 1f;
        if (renderer != null && renderer.sprite != null)
        {
            spriteWidth = renderer.sprite.bounds.size.x;
            spriteHeight = renderer.sprite.bounds.size.y;
        }

        float worldWidth = spriteWidth * Mathf.Abs(currentBaseScale.x);
        float worldHeight = spriteHeight * Mathf.Abs(currentBaseScale.y);
        float fitX = cameraWidthAtMax / Mathf.Max(0.0001f, worldWidth);
        float fitY = cameraHeightAtMax / Mathf.Max(0.0001f, worldHeight);

        return Mathf.Max(fitX, fitY);
    }
}
