using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Se encarga de detallar los niveles que existen, su orden y cuales tiene el jugador desbloqueado.
/// Lo pueden utilizar y modificar los distintos elementos del juego para cambiar entre niveles.
/// </summary>

[Serializable]
public class Level
{
    public enum LevelProgressState
    {
        LOCKED,
        UNLOCKED,
        COMPLETED
    }

    [SerializeField]
    public LevelProgressState state;

    [Tooltip("El nombre que aparecerá en el menú")]
    public string name;
    [HideInInspector]
    public string sceneName;
#if UNITY_EDITOR
    [Tooltip("La escena del nivel")]
    public SceneAsset sceneAsset;
#endif
    public Sprite levelPreview;
}

[CreateAssetMenu(fileName = "LevelsData", menuName = "Game Data/LevelsData")]
public class LevelsData : ScriptableObject
{
    /// <summary>
    /// Los niveles del juego, en orden.
    /// </summary>

    public Level[] levels;

#if UNITY_EDITOR
    private void OnValidate()
    {
        int i = 0;
        foreach (Level l in levels)
        {
            if (l.sceneAsset != null)
            {
                l.sceneName = l.sceneAsset.name;

                //Comprueba si la escena está en los build settings.
                string scenePath = AssetDatabase.GetAssetPath(l.sceneAsset);
                bool found = false;
                foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                {
                    if (s.path == scenePath)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Debug.LogWarning("La escena del nivel " + i + " no se encuentra en los Build Settings. Añádela." + " (" + l.sceneName + ")", this);
                }
            }
            else
            {
                Debug.LogWarning("El nivel " + i + " no tiene una escena asociada desde el editor", this);
            }
            i++;
        }
    }
#endif
}

#if UNITY_EDITOR


[CustomPropertyDrawer(typeof(Level))]
public class LevelDrawer : PropertyDrawer
{
    // ===== CONFIGURACIÓN =====
    private const bool SHOW_INDEX = true;
    private const float INDEX_WIDTH = 30f;
    private const float SEPARATOR_HEIGHT = 2f;
    private const float HORIZONTAL_SPACING = 8f;
    // =========================

    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        int index = GetIndexFromPropertyPath(property.propertyPath);

        // ===== Fondo alternado =====
        Color bgColor = (index % 2 == 0)
            ? new Color(0.35f, 0.35f, 0.35f)
            : new Color(0.35f, 0.35f, 0.35f);

        EditorGUI.DrawRect(position, bgColor);

        float y = position.y + spacing;
        float contentWidth = position.width;

        // Ajuste si mostramos índice
        float startX = position.x;
        if (SHOW_INDEX)
        {
            Rect indexRect = new Rect(
                position.x,
                y,
                INDEX_WIDTH,
                lineHeight
            );

            EditorGUI.LabelField(
                indexRect,
                index.ToString("D2"),
                EditorStyles.boldLabel
            );

            startX += INDEX_WIDTH + HORIZONTAL_SPACING;
            contentWidth -= INDEX_WIDTH + HORIZONTAL_SPACING;
        }

        // ===== Ancho columnas (más espacio a campos) =====
        float columnWidth =
            (contentWidth - HORIZONTAL_SPACING) / 2f;

        float labelWidthBackup = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 85f; // menos label, más campo

        // ===== FILA 1 =====
#if UNITY_EDITOR
        SerializedProperty sceneProp =
            property.FindPropertyRelative("sceneAsset");
#endif
        SerializedProperty stateProp =
            property.FindPropertyRelative("state");

        Rect sceneRect = new Rect(
            startX,
            y,
            columnWidth,
            lineHeight
        );

        Rect stateRect = new Rect(
            startX + columnWidth + HORIZONTAL_SPACING,
            y,
            columnWidth,
            lineHeight
        );

#if UNITY_EDITOR
        EditorGUI.PropertyField(sceneRect, sceneProp);
#endif
        EditorGUI.PropertyField(stateRect, stateProp);

        y += lineHeight + spacing;

        // ===== FILA 2 =====
        SerializedProperty nameProp =
            property.FindPropertyRelative("name");

        SerializedProperty previewProp =
            property.FindPropertyRelative("levelPreview");

        Rect nameRect = new Rect(
            startX,
            y,
            columnWidth,
            lineHeight
        );

        Rect previewRect = new Rect(
            startX + columnWidth + HORIZONTAL_SPACING,
            y,
            columnWidth,
            lineHeight
        );

        EditorGUI.PropertyField(nameRect, nameProp);
        EditorGUI.PropertyField(previewRect, previewProp);

        y += lineHeight + spacing;

        // ===== Separador =====
        Rect separator = new Rect(
            position.x,
            y,
            position.width,
            SEPARATOR_HEIGHT
        );

        EditorGUI.DrawRect(separator, Color.black);

        EditorGUIUtility.labelWidth = labelWidthBackup;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        int lines = 2;

        return (lines * lineHeight)
               + (lines * spacing)
               + SEPARATOR_HEIGHT
               + spacing;
    }

    private int GetIndexFromPropertyPath(string path)
    {
        int start = path.IndexOf('[') + 1;
        int end = path.IndexOf(']');
        string number =
            path.Substring(start, end - start);

        return int.Parse(number);
    }
}
#endif