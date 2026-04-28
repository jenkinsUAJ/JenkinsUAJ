using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LanguageAndInputMediaComponent))]
public class LanguageAndInputMediaComponentEditor : Editor
{
    private SerializedProperty objectsProperty;
    private int languageFilterMask;
    private int inputFilterMask;

    private void OnEnable()
    {
        objectsProperty = serializedObject.FindProperty("objects");
        languageFilterMask = GetAllMask(typeof(LanguageAndInputMediaManager.Language));
        inputFilterMask = GetAllMask(typeof(LanguageAndInputMediaManager.InputType));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        DrawSyncButton();
        EditorGUILayout.Space();
        DrawObjectsList();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSyncButton()
    {
        if (GUILayout.Button("Sync Variants With Enums"))
        {
            serializedObject.ApplyModifiedProperties();

            LanguageAndInputMediaComponent component = (LanguageAndInputMediaComponent)target;
            Undo.RecordObject(component, "Sync language/input variants");
            component.SyncAllObjectsWithEnums();
            EditorUtility.SetDirty(component);

            serializedObject.Update();
        }
    }

    private void DrawObjectsList()
    {
        if (objectsProperty == null)
        {
            EditorGUILayout.HelpBox("No se encontro la lista 'objects'.", MessageType.Error);
            return;
        }

        for (int i = 0; i < objectsProperty.arraySize; i++)
        {
            SerializedProperty objectElement = objectsProperty.GetArrayElementAtIndex(i);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool isExpanded = DrawObjectHeader(objectElement, i);
                if (isExpanded)
                {
                    DrawObjectBody(objectElement);
                    DrawObjectActions(i);
                }
            }

            EditorGUILayout.Space(4);
        }

        if (GUILayout.Button("Add Object"))
        {
            objectsProperty.InsertArrayElementAtIndex(objectsProperty.arraySize);
        }
    }

    private static bool DrawObjectHeader(SerializedProperty objectElement, int index)
    {
        SerializedProperty debugName = objectElement.FindPropertyRelative("debugName");

        string blockName = string.IsNullOrWhiteSpace(debugName.stringValue)
            ? "Object " + index
            : debugName.stringValue;

        objectElement.isExpanded = EditorGUILayout.Foldout(
            objectElement.isExpanded,
            blockName,
            true,
            EditorStyles.foldoutHeader);

        if (!objectElement.isExpanded)
        {
            return false;
        }

        EditorGUILayout.PropertyField(debugName);
        EditorGUILayout.PropertyField(objectElement.FindPropertyRelative("objectType"));
        return true;
    }

    private void DrawObjectBody(SerializedProperty objectElement)
    {
        SerializedProperty objectTypeProperty = objectElement.FindPropertyRelative("objectType");
        LanguageAndInputMediaComponent.LanguageAndInputObject.ObjectType objectType =
            (LanguageAndInputMediaComponent.LanguageAndInputObject.ObjectType)objectTypeProperty.enumValueIndex;

        switch (objectType)
        {
            case LanguageAndInputMediaComponent.LanguageAndInputObject.ObjectType.Text:
                EditorGUILayout.PropertyField(objectElement.FindPropertyRelative("textTarget"));
                break;

            case LanguageAndInputMediaComponent.LanguageAndInputObject.ObjectType.Image:
                EditorGUILayout.PropertyField(objectElement.FindPropertyRelative("uiImageTarget"));
                EditorGUILayout.PropertyField(objectElement.FindPropertyRelative("spriteRendererTarget"));
                break;

            case LanguageAndInputMediaComponent.LanguageAndInputObject.ObjectType.CustomObjects:
                EditorGUILayout.HelpBox("Asigna en cada variante el GameObject a activar para esa combinacion.", MessageType.Info);
                break;
        }

        DrawVariants(objectElement, objectType);
    }

    private void DrawVariants(
        SerializedProperty objectElement,
        LanguageAndInputMediaComponent.LanguageAndInputObject.ObjectType objectType)
    {
        SerializedProperty variants = objectElement.FindPropertyRelative("variants");
        EditorGUILayout.Space(4);
        DrawVariantsHeader();

        if (variants.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No hay variantes. Pulsa Sync Variants With Enums.", MessageType.Warning);
            return;
        }

        bool anyVisibleVariant = false;

        for (int i = 0; i < variants.arraySize; i++)
        {
            SerializedProperty variant = variants.GetArrayElementAtIndex(i);

            if (!PassesFilters(variant))
            {
                continue;
            }

            anyVisibleVariant = true;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawVariantKey(variant);

                switch (objectType)
                {
                    case LanguageAndInputMediaComponent.LanguageAndInputObject.ObjectType.Text:
                        EditorGUILayout.PropertyField(variant.FindPropertyRelative("textValue"));
                        break;

                    case LanguageAndInputMediaComponent.LanguageAndInputObject.ObjectType.Image:
                        EditorGUILayout.PropertyField(variant.FindPropertyRelative("spriteValue"));
                        break;

                    case LanguageAndInputMediaComponent.LanguageAndInputObject.ObjectType.CustomObjects:
                        EditorGUILayout.PropertyField(variant.FindPropertyRelative("customObjectValue"));
                        break;
                }
            }
        }

        if (!anyVisibleVariant)
        {
            EditorGUILayout.HelpBox("No hay variants que cumplan el filtro seleccionado.", MessageType.Info);
        }
    }

    private static void DrawVariantKey(SerializedProperty variant)
    {
        SerializedProperty language = variant.FindPropertyRelative("language");
        SerializedProperty inputType = variant.FindPropertyRelative("inputType");

        using (new EditorGUI.DisabledScope(true))
        {
            Rect rowRect = EditorGUILayout.GetControlRect();
            float spacing = 6f;
            float leftWidth = (rowRect.width - spacing) * 0.5f;
            Rect leftRect = new Rect(rowRect.x, rowRect.y, leftWidth, rowRect.height);
            Rect rightRect = new Rect(leftRect.xMax + spacing, rowRect.y, leftWidth, rowRect.height);

            DrawLabeledEnumProperty(leftRect, "Languaje", language);
            DrawLabeledEnumProperty(rightRect, "Input Type", inputType);
        }
    }

    private static void DrawLabeledEnumProperty(Rect rect, string label, SerializedProperty property)
    {
        float labelWidth = Mathf.Clamp(rect.width * 0.34f, 58f, 80f);
        Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
        Rect fieldRect = new Rect(labelRect.xMax + 4f, rect.y, rect.width - labelWidth - 4f, rect.height);

        EditorGUI.LabelField(labelRect, label);
        EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
    }

    private void DrawVariantsHeader()
    {
        string[] languageOptions = System.Enum.GetNames(typeof(LanguageAndInputMediaManager.Language));
        string[] inputOptions = System.Enum.GetNames(typeof(LanguageAndInputMediaManager.InputType));

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(
                new GUIContent(
                    "Variants",
                    "Solo uno de estos valores sera visible segun la configuracion actual de idioma/input."),
                EditorStyles.boldLabel,
                GUILayout.Width(60));

            GUILayout.Label("Languaje", GUILayout.Width(62));
            languageFilterMask = EditorGUILayout.MaskField(languageFilterMask, languageOptions, GUILayout.MinWidth(90));

            GUILayout.Label("Input Type", GUILayout.Width(68));
            inputFilterMask = EditorGUILayout.MaskField(inputFilterMask, inputOptions, GUILayout.MinWidth(90));
        }
    }

    private bool PassesFilters(SerializedProperty variant)
    {
        SerializedProperty language = variant.FindPropertyRelative("language");
        SerializedProperty inputType = variant.FindPropertyRelative("inputType");

        int languageBit = 1 << language.enumValueIndex;
        int inputBit = 1 << inputType.enumValueIndex;

        bool languageMatches = (languageFilterMask & languageBit) != 0;
        bool inputMatches = (inputFilterMask & inputBit) != 0;

        return languageMatches && inputMatches;
    }

    private static int GetAllMask(System.Type enumType)
    {
        string[] names = System.Enum.GetNames(enumType);
        int mask = 0;

        for (int i = 0; i < names.Length; i++)
        {
            mask |= 1 << i;
        }

        return mask;
    }

    private void DrawObjectActions(int index)
    {
        if (GUILayout.Button("Remove Object " + index))
        {
            objectsProperty.DeleteArrayElementAtIndex(index);
        }
    }
}
