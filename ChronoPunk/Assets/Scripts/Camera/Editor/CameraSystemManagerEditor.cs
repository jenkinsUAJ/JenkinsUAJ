using UnityEngine;
using UnityEditor;
using CameraSystem;

namespace CameraSystem.Editor
{
    /// <summary>
    /// Custom editor para CameraSystemManager.
    /// Proporciona herramientas de debug y configuración.
    /// </summary>
    [CustomEditor(typeof(CameraSystemManager))]
    public class CameraSystemManagerEditor : UnityEditor.Editor
    {
        private CameraSystemManager manager;
        private SerializedProperty mainCameraProperty;
        private SerializedProperty playerTargetProperty;
        private SerializedProperty globalBoundsColliderProperty;
        private SerializedProperty useGlobalBoundsProperty;
        private SerializedProperty defaultSettingsProperty;
        private SerializedProperty shadowMenuVirtualCameraProperty;
        private SerializedProperty shadowMenuPriorityProperty;
        private SerializedProperty transitionCooldownProperty;
        private SerializedProperty showDebugInfoProperty;

        private bool showReferences = true;
        private bool showGlobalBounds = true;
        private bool showGlobalSettings = true;
        private bool showShadowMenuCamera = true;
        private bool showDefaultSettings = true;
        private bool showRuntimeInfo = true;

        private void OnEnable()
        {
            manager = (CameraSystemManager)target;
            mainCameraProperty = serializedObject.FindProperty("mainCamera");
            playerTargetProperty = serializedObject.FindProperty("playerTarget");
            globalBoundsColliderProperty = serializedObject.FindProperty("globalBoundsCollider");
            useGlobalBoundsProperty = serializedObject.FindProperty("useGlobalBounds");
            defaultSettingsProperty = serializedObject.FindProperty("defaultSettings");
            shadowMenuVirtualCameraProperty = serializedObject.FindProperty("shadowMenuVirtualCamera");
            shadowMenuPriorityProperty = serializedObject.FindProperty("shadowMenuPriority");
            transitionCooldownProperty = serializedObject.FindProperty("transitionCooldown");
            showDebugInfoProperty = serializedObject.FindProperty("showDebugInfo");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!HasRequiredProperties())
            {
                EditorGUILayout.HelpBox(
                    "No se pudieron resolver todas las propiedades serializadas del CameraSystemManager. Mostrando inspector por defecto.",
                    MessageType.Warning);
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space(10);
            DrawManagerHeader();
            EditorGUILayout.Space(10);

            showReferences = EditorGUILayout.Foldout(showReferences, "Referencias", true, EditorStyles.foldoutHeader);
            if (showReferences)
            {
                DrawReferences();
            }

            EditorGUILayout.Space(5);

            showGlobalBounds = EditorGUILayout.Foldout(showGlobalBounds, "Límites Globales de Cámara", true, EditorStyles.foldoutHeader);
            if (showGlobalBounds)
            {
                DrawGlobalBounds();
            }

            EditorGUILayout.Space(5);

            showGlobalSettings = EditorGUILayout.Foldout(showGlobalSettings, "Configuración Global", true, EditorStyles.foldoutHeader);
            if (showGlobalSettings)
            {
                DrawGlobalSettings();
            }

            EditorGUILayout.Space(5);

            showShadowMenuCamera = EditorGUILayout.Foldout(showShadowMenuCamera, "Cámara Menú de Sombras", true, EditorStyles.foldoutHeader);
            if (showShadowMenuCamera)
            {
                DrawShadowMenuCameraSettings();
            }

            EditorGUILayout.Space(5);

            showDefaultSettings = EditorGUILayout.Foldout(showDefaultSettings, "Configuración por Defecto", true, EditorStyles.foldoutHeader);
            if (showDefaultSettings)
            {
                DrawDefaultSettings();
            }

            EditorGUILayout.Space(5);

            // Runtime info (solo en play mode)
            if (Application.isPlaying)
            {
                showRuntimeInfo = EditorGUILayout.Foldout(showRuntimeInfo, "Información en Tiempo Real", true, EditorStyles.foldoutHeader);
                if (showRuntimeInfo)
                {
                    DrawRuntimeInfo();
                }
                EditorGUILayout.Space(5);
            }

            DrawActionButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private bool HasRequiredProperties()
        {
            return mainCameraProperty != null
                && playerTargetProperty != null
                && globalBoundsColliderProperty != null
                && useGlobalBoundsProperty != null
                && defaultSettingsProperty != null
                && shadowMenuVirtualCameraProperty != null
                && shadowMenuPriorityProperty != null
                && transitionCooldownProperty != null
                && showDebugInfoProperty != null;
        }

        private void DrawManagerHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("Camera System Manager", headerStyle);

            // Estado de inicialización
            if (Application.isPlaying)
            {
                string status = manager.IsInitialized ? "Inicializado" : "No inicializado";
                Color statusColor = manager.IsInitialized ? Color.green : Color.red;

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

        private void DrawReferences()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(mainCameraProperty, new GUIContent("Cámara Principal"));
            EditorGUILayout.PropertyField(playerTargetProperty, new GUIContent("Target (Jugador)"));

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawGlobalBounds()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(useGlobalBoundsProperty, new GUIContent("Usar Límites Globales", "Si está activo, la cámara no saldrá de los límites definidos"));

            if (useGlobalBoundsProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(globalBoundsColliderProperty, new GUIContent("Collider de Límites", "PolygonCollider2D o CompositeCollider2D que define el área"));

                if (globalBoundsColliderProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Asigna un Collider2D que defina los límites del nivel. Puedes crear uno desde Tools > Camera System > Crear Camera Bounds.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawGlobalSettings()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(transitionCooldownProperty, new GUIContent("Cooldown de Transición", "Tiempo mínimo entre cambios de cámara (evita cambios rápidos consecutivos)"));
            EditorGUILayout.PropertyField(showDebugInfoProperty, new GUIContent("Mostrar Info de Debug"));

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawShadowMenuCameraSettings()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(shadowMenuVirtualCameraProperty,
                new GUIContent("Virtual Camera", "Cámara que dominará durante menú de sombras y transición pre-reset"));

            if (shadowMenuVirtualCameraProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Asigna una CinemachineCamera en la escena para habilitar el override del menú de sombras.",
                    MessageType.Info);
            }

            EditorGUILayout.PropertyField(shadowMenuPriorityProperty,
                new GUIContent("Prioridad Override", "Debe ser mayor que las prioridades de regiones"));

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawDefaultSettings()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Dibujar cada propiedad del defaultSettings
            var settings = defaultSettingsProperty;

            EditorGUILayout.PropertyField(settings.FindPropertyRelative("regionName"), new GUIContent("Nombre"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("priority"), new GUIContent("Prioridad"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("orthographicSize"), new GUIContent("Orthographic Size"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("cameraOffset"), new GUIContent("Camera Offset"));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("dampingX"), new GUIContent("Damping X"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("dampingY"), new GUIContent("Damping Y"));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("deadZoneWidth"), new GUIContent("Ancho"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("deadZoneHeight"), new GUIContent("Alto"));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("useHardLimits"), new GUIContent("Usar Hard Limits"));
            if (settings.FindPropertyRelative("useHardLimits").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("hardLimitWidth"), new GUIContent("Ancho"));
                EditorGUILayout.PropertyField(settings.FindPropertyRelative("hardLimitHeight"), new GUIContent("Alto"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("blendTimeIn"), new GUIContent("Tiempo de Entrada"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("blendStyleIn"), new GUIContent("Estilo de Entrada"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("blendTimeOut"), new GUIContent("Tiempo de Salida"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("blendStyleOut"), new GUIContent("Estilo de Salida"));

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawRuntimeInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Región activa actual
            EditorGUILayout.LabelField("Región Activa:", EditorStyles.boldLabel);
            if (manager.CurrentActiveRegion != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Nombre: {manager.CurrentActiveRegion.Settings.regionName}");
                EditorGUILayout.LabelField($"Prioridad: {manager.CurrentActiveRegion.Settings.priority}");
                EditorGUILayout.LabelField($"Ortho Size: {manager.CurrentActiveRegion.Settings.orthographicSize}");
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("(Usando configuración por defecto)");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Regiones activas
            var activeRegions = manager.GetActiveRegions();
            EditorGUILayout.LabelField($"Regiones Activas: {activeRegions.Count}", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var region in activeRegions)
            {
                EditorGUILayout.LabelField($"• {region.Settings.regionName} (P: {region.Settings.priority})");
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);

            // Configuración actual
            var currentSettings = manager.GetCurrentSettings();
            EditorGUILayout.LabelField("Configuración Actual:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"Ortho Size: {currentSettings.orthographicSize}");
            EditorGUILayout.LabelField($"Offset: {currentSettings.cameraOffset}");
            EditorGUILayout.LabelField($"Damping: ({currentSettings.dampingX}, {currentSettings.dampingY})");
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();

            // Forzar repintado
            Repaint();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Acciones", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔍 Buscar Regiones", GUILayout.Height(25)))
            {
                FindAllRegions();
            }

            if (GUILayout.Button("📸 Crear Región", GUILayout.Height(25)))
            {
                CreateNewRegion();
            }

            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("💥 Camera Shake", GUILayout.Height(25)))
                {
                    manager.ShakeCamera(100f, 0.3f);
                }

                if (GUILayout.Button("🔄 Reset", GUILayout.Height(25)))
                {
                    manager.RestoreNormalBehavior();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void FindAllRegions()
        {
            CameraRegion[] regions = FindObjectsByType<CameraRegion>(FindObjectsSortMode.None);
            Debug.Log($"[CameraSystemManager] Encontradas {regions.Length} regiones de cámara:");
            foreach (var region in regions)
            {
                Debug.Log($"  • {region.Settings.regionName} - Prioridad: {region.Settings.priority}, Ortho Size: {region.Settings.orthographicSize}");
            }
        }

        private void CreateNewRegion()
        {
            GameObject regionObj = new GameObject("CameraRegion_New");
            regionObj.transform.position = SceneView.lastActiveSceneView != null ?
                SceneView.lastActiveSceneView.pivot : Vector3.zero;

            // Añadir BoxCollider2D
            BoxCollider2D collider = regionObj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(10f, 10f);

            // Añadir CameraRegion
            CameraRegion region = regionObj.AddComponent<CameraRegion>();

            // Seleccionar el nuevo objeto
            Selection.activeGameObject = regionObj;
            EditorGUIUtility.PingObject(regionObj);

            // Abrir el foco en el objeto
            SceneView.lastActiveSceneView?.FrameSelected();

            Debug.Log("[CameraSystemManager] Nueva región de cámara creada. Configura sus propiedades en el inspector.");

            Undo.RegisterCreatedObjectUndo(regionObj, "Create Camera Region");
        }

        private void OnSceneGUI()
        {
            if (manager == null || !Application.isPlaying) return;

            // Dibujar info de la región activa actual
            var currentRegion = manager.CurrentActiveRegion;
            if (currentRegion != null)
            {
                Handles.color = Color.green;
                Vector3 pos = currentRegion.transform.position;
                Handles.Label(pos + Vector3.up * 3f,
                    $"📷 ACTIVA: {currentRegion.Settings.regionName}",
                    EditorStyles.whiteBoldLabel);
            }
        }
    }
}
