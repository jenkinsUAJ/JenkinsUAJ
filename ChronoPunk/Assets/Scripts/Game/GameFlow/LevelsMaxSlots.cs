using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class LevelInfo
{
    [HideInInspector]
    public string scenePath;

#if UNITY_EDITOR
    [SerializeField]
    private SceneAsset scene;

    public void SyncScenePath()
    {
        scenePath = scene != null
            ? AssetDatabase.GetAssetPath(scene)
            : string.Empty;
    }
#endif

    public int maxSlots;
}

[CreateAssetMenu(
    fileName = "LevelsMaxSlots",
    menuName = "Game Data/Levels Max Slots",
    order = 0)]
public class LevelsMaxSlots : ScriptableObject
{
    public List<LevelInfo> levels = new List<LevelInfo>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null)
            {
                levels[i].SyncScenePath();
            }
        }
    }
#endif
}




#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(LevelInfo))]
public class LevelInfoDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // ======= CONFIGURACIÓN =======
        const bool SHOW_INDEX = true;
        // ==============================

        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.IndentedRect(position);

        float spacing = 5f;
        float indexWidth = SHOW_INDEX ? 30f : 0f;
        float labelWidth = 70f;

        float contentX = position.x + indexWidth + (SHOW_INDEX ? spacing : 0f);
        float contentWidth = position.width - indexWidth - (SHOW_INDEX ? spacing : 0f);

        float fieldWidth =
            (contentWidth - (labelWidth * 2) - 20f) / 2f;

        // Obtener propiedades
        SerializedProperty sceneProp =
            property.FindPropertyRelative("scene");

        SerializedProperty scenePathProp =
            property.FindPropertyRelative("scenePath");

        SerializedProperty slotsProp =
            property.FindPropertyRelative("maxSlots");

        // ======= ÍNDICE =======
        if (SHOW_INDEX)
        {
            int index =
                GetIndexFromPropertyPath(property.propertyPath);

            Rect indexRect =
                new Rect(position.x,
                         position.y,
                         indexWidth,
                         position.height);

            EditorGUI.LabelField(indexRect,
                                 index.ToString("D2"));
        }

        // ======= Scene =======
        Rect sceneLabelRect =
            new Rect(contentX,
                     position.y,
                     labelWidth,
                     position.height);

        Rect sceneFieldRect =
            new Rect(sceneLabelRect.xMax + 2,
                     position.y,
                     fieldWidth,
                     position.height);

        EditorGUI.LabelField(sceneLabelRect,
                             "Scene");

        sceneProp.objectReferenceValue =
            EditorGUI.ObjectField(
                sceneFieldRect,
                sceneProp.objectReferenceValue,
                typeof(SceneAsset),
                false
            );

        SceneAsset selectedScene =
            sceneProp.objectReferenceValue as SceneAsset;

        scenePathProp.stringValue =
            selectedScene != null
                ? AssetDatabase.GetAssetPath(selectedScene)
                : string.Empty;

        // ======= MaxSlots =======
        Rect slotsLabelRect =
            new Rect(sceneFieldRect.xMax + 10,
                     position.y,
                     labelWidth,
                     position.height);

        Rect slotsFieldRect =
            new Rect(slotsLabelRect.xMax + 2,
                     position.y,
                     fieldWidth,
                     position.height);

        EditorGUI.LabelField(slotsLabelRect,
                             "MaxSlots");

        slotsProp.intValue =
            EditorGUI.IntField(
                slotsFieldRect,
                slotsProp.intValue
            );

        EditorGUI.EndProperty();
    }

    private int GetIndexFromPropertyPath(string path)
    {
        int start = path.IndexOf('[') + 1;
        int end = path.IndexOf(']');
        string number = path.Substring(start,
                                        end - start);
        return int.Parse(number);
    }
}
#endif