using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;

public class VFXLighningHandler : MonoBehaviour
{
    [Tooltip("Posicion final en caso de ser un gameobject móvil, si no es movil dejarlo vacio")]
    public Transform finalPosition = null;
    public bool startActive = false;
    public SplineContainer spline;
    public VisualEffect vfx;
    public int pointsAmmount = 50;

    private GraphicsBuffer _graphicsBuffer;

    [SerializeField] private Vector3[] _bakedPositions;


    private readonly int _graphicsBufferID = Shader.PropertyToID("PositionsBuffer");

    private bool active = false;

    public void Activate()
    {
        if (active)
            Deactivate();
        else
        {
            vfx.SendEvent("On");
            active = true;
        }
    }

    public void Deactivate()
    {
        if (!active)
            Activate();
        else
        {
            vfx.SendEvent("Off");
            active = false;
        }
    }
    public void BakePoints()
    {
        if (spline == null || vfx == null)
        {
            Debug.LogWarning("ERROR falta asignar spline o vfx.");
            return;
        }

        ReleaseBuffer();

        _bakedPositions = new Vector3[pointsAmmount];

        for (int i = 0; i < pointsAmmount; i++)
        {
            float t = i / (float)(pointsAmmount - 1);
            _bakedPositions[i] = spline.EvaluatePosition(t);
        }

        CreateAndPopulateBuffer();
    }

    private void CreateAndPopulateBuffer()
    {
        if (_bakedPositions == null || _bakedPositions.Length == 0) return;

        int count = _bakedPositions.Length;
        int stride = Marshal.SizeOf(typeof(Vector3)); // 12 bytes

        _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, stride);
        _graphicsBuffer.SetData(_bakedPositions);

        vfx.SetGraphicsBuffer(_graphicsBufferID, _graphicsBuffer);
    }

    private void ReleaseBuffer()
    {
        if (_graphicsBuffer != null)
        {
            _graphicsBuffer.Release();
            _graphicsBuffer = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (_bakedPositions == null || _bakedPositions.Length == 0 || spline == null)
        {
            return;
        }

        Gizmos.color = Color.green;

        for (int i = 0; i < _bakedPositions.Length; i++)
        {

            Gizmos.DrawSphere(_bakedPositions[i], 0.2f);

            if (i > 0)
            {
                Vector3 previousWorldPos = _bakedPositions[i - 1];
                Gizmos.DrawLine(previousWorldPos, _bakedPositions[i]);
            }
        }
    }

    private void OnDestroy()
    {
        ReleaseBuffer();
    }
    void Start()
    {
        CreateAndPopulateBuffer();
        if (_bakedPositions == null)
        {
            Debug.LogError("WARNING, puntos no bakeados en el editor, bakeando en runtime");
            BakePoints();
        }

        if (startActive)
            Activate();
    }

    void Update()
    {

    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(VFXLighningHandler))]
public class VFXLighningHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VFXLighningHandler script = (VFXLighningHandler)target;

        GUILayout.Space(15);

        if (GUILayout.Button("Bakear puntos en spline", GUILayout.Height(40)))
        {
            script.BakePoints();

            EditorUtility.SetDirty(script);
        }
    }
}
#endif