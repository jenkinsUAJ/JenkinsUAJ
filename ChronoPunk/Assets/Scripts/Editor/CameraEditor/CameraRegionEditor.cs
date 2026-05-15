using UnityEngine;
using UnityEditor;
using CameraSystem;

namespace CameraSystem.Editor
{
    /// <summary>
    /// Custom editor para CameraRegion.
    /// Proporciona herramientas visuales para configurar las regiones de cámara.
    /// </summary>
    [CustomEditor(typeof(CameraRegion))]
    [CanEditMultipleObjects]
    public class CameraRegionEditor : UnityEditor.Editor
    {
        private CameraRegion region;
        private SerializedProperty settingsProperty;
        private SerializedProperty gizmoColorProperty;
        private SerializedProperty alwaysShowGizmoProperty;
        private SerializedProperty showDebugInfoProperty;

        private bool showPositionSettings = true;
        private bool showDampingSettings = true;
        private bool showZoneSettings = true;
        private bool showTransitionSettings = true;
        private bool showAdvancedSettings = false;

        private void OnEnable()
        {
            region = (CameraRegion)target;
            settingsProperty = serializedObject.FindProperty("settings");
            gizmoColorProperty = serializedObject.FindProperty("gizmoColor");
            alwaysShowGizmoProperty = serializedObject.FindProperty("alwaysShowGizmo");
            showDebugInfoProperty = serializedObject.FindProperty("showDebugInfo");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            DrawRegionHeader();
            EditorGUILayout.Space(10);

            // Configuración básica
            DrawBasicSettings();
            EditorGUILayout.Space(5);

            // Configuración de posición y FOV
            DrawPositionFoldout();
            EditorGUILayout.Space(5);

            // Configuración de damping
            DrawDampingFoldout();
            EditorGUILayout.Space(5);

            // Configuración de zonas
            DrawZoneFoldout();
            EditorGUILayout.Space(5);

            // Configuración de transiciones
            DrawTransitionFoldout();
            EditorGUILayout.Space(5);

            // Configuración avanzada
            DrawAdvancedFoldout();
            EditorGUILayout.Space(5);

            // Visualización
            DrawVisualizationSettings();
            EditorGUILayout.Space(10);

            // Botones de acción
            DrawActionButtons();

            serializedObject.ApplyModifiedProperties();

            // Repintar la escena si hay cambios
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawRegionHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            string regionName = settingsProperty.FindPropertyRelative("regionName").stringValue;
            EditorGUILayout.LabelField($"Región de Cámara: {regionName}", headerStyle);

            // Estado actual
            if (Application.isPlaying)
            {
                string status = region.IsPlayerInside ? "Jugador DENTRO" : "Jugador fuera";
                Color statusColor = region.IsPlayerInside ? Color.green : Color.gray;

                GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = statusColor }
                };
                EditorGUILayout.LabelField(status, statusStyle);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Configuración Básica", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("regionName"),
                new GUIContent("Nombre de la Región"));
            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("priority"),
                new GUIContent("Prioridad", "Mayor valor = mayor prioridad cuando hay solapamiento"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawPositionFoldout()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showPositionSettings = EditorGUILayout.Foldout(showPositionSettings, "Posición y Zoom", true, EditorStyles.foldoutHeader);

            if (showPositionSettings)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("orthographicSize"),
                    new GUIContent("Orthographic Size"));

                // Indicador visual del zoom
                float zoomLevel = Mathf.InverseLerp(30f, 1f, settingsProperty.FindPropertyRelative("orthographicSize").floatValue);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 16), zoomLevel, $"Zoom: {zoomLevel:P0}");

                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("cameraOffset"),
                    new GUIContent("Camera Offset", "Offset de la cámara respecto al jugador en unidades del mundo (X, Y)"));

                EditorGUILayout.Space(5);

                // Posición fija
                var useFixedProp = settingsProperty.FindPropertyRelative("useFixedPosition");
                EditorGUILayout.PropertyField(useFixedProp,
                    new GUIContent("Usar Posición Fija", "La cámara no sigue al jugador"));

                if (useFixedProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("fixedPosition"),
                        new GUIContent("Posición Fija"));
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawDampingFoldout()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showDampingSettings = EditorGUILayout.Foldout(showDampingSettings, "Suavizado (Damping)", true, EditorStyles.foldoutHeader);

            if (showDampingSettings)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("dampingX"),
                    new GUIContent("Damping X", "Suavizado horizontal (mayor = más lento)"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("dampingY"),
                    new GUIContent("Damping Y", "Suavizado vertical (mayor = más lento)"));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawZoneFoldout()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showZoneSettings = EditorGUILayout.Foldout(showZoneSettings, "Dead Zone y Hard Limits", true, EditorStyles.foldoutHeader);

            if (showZoneSettings)
            {
                EditorGUI.indentLevel++;

                // Dead Zone
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("deadZoneWidth"),
                    new GUIContent("Ancho", "El jugador puede moverse dentro de esta zona sin que la cámara reaccione"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("deadZoneHeight"),
                    new GUIContent("Alto"));

                EditorGUILayout.Space(5);

                // Hard Limits
                var useHardLimitsProp = settingsProperty.FindPropertyRelative("useHardLimits");
                EditorGUILayout.PropertyField(useHardLimitsProp,
                    new GUIContent("Usar Hard Limits", "El target no podrá salir de estos límites en pantalla"));

                if (useHardLimitsProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("hardLimitWidth"),
                        new GUIContent("Ancho", "Límite horizontal máximo del target en pantalla (0-1)"));
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("hardLimitHeight"),
                        new GUIContent("Alto", "Límite vertical máximo del target en pantalla (0-1)"));
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawTransitionFoldout()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showTransitionSettings = EditorGUILayout.Foldout(showTransitionSettings, "Transiciones", true, EditorStyles.foldoutHeader);

            if (showTransitionSettings)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("blendTimeIn"),
                    new GUIContent("Tiempo de Transición"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("blendStyleIn"),
                    new GUIContent("Estilo de Transición"));

                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("blendTimeOut"),
                    new GUIContent("Tiempo de Transición"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("blendStyleOut"),
                    new GUIContent("Estilo de Transición"));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedFoldout()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Configuración Avanzada", true, EditorStyles.foldoutHeader);

            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("lookAheadTime"),
                    new GUIContent("Tiempo de Anticipación"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("lookAheadSmoothing"),
                    new GUIContent("Suavizado de Anticipación"));

                EditorGUILayout.Space(5);

                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("confineToRegion"),
                    new GUIContent("Confinar a Región", "La cámara no saldrá de los límites del collider"));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawVisualizationSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Visualización en Editor", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(gizmoColorProperty, new GUIContent("Color del Gizmo"));
            EditorGUILayout.PropertyField(alwaysShowGizmoProperty, new GUIContent("Mostrar Siempre"));
            EditorGUILayout.PropertyField(showDebugInfoProperty, new GUIContent("Mostrar Info Debug"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Copiar Configuración", GUILayout.Height(25)))
            {
                CopySettingsToClipboard();
            }

            if (GUILayout.Button("Pegar Configuración", GUILayout.Height(25)))
            {
                PasteSettingsFromClipboard();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Aplicar a VCam", GUILayout.Height(25)))
            {
                region.ApplySettingsToVirtualCamera();
                EditorUtility.SetDirty(region);
            }

            if (Application.isPlaying && GUILayout.Button("Vista Previa", GUILayout.Height(25)))
            {
                region.ActivateRegion();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static string copiedSettings = "";

        private void CopySettingsToClipboard()
        {
            copiedSettings = JsonUtility.ToJson(region.Settings);
            EditorGUIUtility.systemCopyBuffer = copiedSettings;
            Debug.Log("[CameraRegionEditor] Configuración copiada al portapapeles");
        }

        private void PasteSettingsFromClipboard()
        {
            if (!string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer))
            {
                try
                {
                    // Crear nueva configuración desde JSON
                    CameraRegionSettings newSettings = JsonUtility.FromJson<CameraRegionSettings>(EditorGUIUtility.systemCopyBuffer);

                    // Aplicar propiedades una por una
                    var settingsObj = settingsProperty;
                    settingsObj.FindPropertyRelative("regionName").stringValue = newSettings.regionName;
                    settingsObj.FindPropertyRelative("priority").intValue = newSettings.priority;
                    settingsObj.FindPropertyRelative("orthographicSize").floatValue = newSettings.orthographicSize;
                    settingsObj.FindPropertyRelative("cameraOffset").vector2Value = newSettings.cameraOffset;
                    settingsObj.FindPropertyRelative("useFixedPosition").boolValue = newSettings.useFixedPosition;
                    settingsObj.FindPropertyRelative("fixedPosition").vector3Value = newSettings.fixedPosition;
                    settingsObj.FindPropertyRelative("dampingX").floatValue = newSettings.dampingX;
                    settingsObj.FindPropertyRelative("dampingY").floatValue = newSettings.dampingY;
                    settingsObj.FindPropertyRelative("deadZoneWidth").floatValue = newSettings.deadZoneWidth;
                    settingsObj.FindPropertyRelative("deadZoneHeight").floatValue = newSettings.deadZoneHeight;
                    settingsObj.FindPropertyRelative("useHardLimits").boolValue = newSettings.useHardLimits;
                    settingsObj.FindPropertyRelative("hardLimitWidth").floatValue = newSettings.hardLimitWidth;
                    settingsObj.FindPropertyRelative("hardLimitHeight").floatValue = newSettings.hardLimitHeight;
                    settingsObj.FindPropertyRelative("confineToRegion").boolValue = newSettings.confineToRegion;
                    settingsObj.FindPropertyRelative("blendTimeIn").floatValue = newSettings.blendTimeIn;
                    settingsObj.FindPropertyRelative("blendTimeOut").floatValue = newSettings.blendTimeOut;
                    settingsObj.FindPropertyRelative("lookAheadTime").floatValue = newSettings.lookAheadTime;
                    settingsObj.FindPropertyRelative("lookAheadSmoothing").floatValue = newSettings.lookAheadSmoothing;

                    serializedObject.ApplyModifiedProperties();
                    Debug.Log("[CameraRegionEditor] Configuración pegada desde el portapapeles");
                }
                catch
                {
                    Debug.LogWarning("[CameraRegionEditor] El contenido del portapapeles no es una configuración válida");
                }
            }
        }

        private void OnSceneGUI()
        {
            if (region == null) return;

            var settings = region.Settings;

            // Solo mostrar handle interactivo si la cámara está en modo fixed
            if (settings.useFixedPosition)
            {
                // Handle para mover la posición fija (usar target directamente, no serializedObject)
                EditorGUI.BeginChangeCheck();
                Vector3 newFixedPos = Handles.PositionHandle(settings.fixedPosition, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(region, "Move Fixed Camera Position");
                    region.Settings.fixedPosition = newFixedPos;
                    EditorUtility.SetDirty(region);
                }
            }
        }
    }
}
