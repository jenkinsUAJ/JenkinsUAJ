using UnityEngine;
using UnityEditor;
using CameraSystem;

namespace CameraSystem.Editor
{
    /// <summary>
    /// Menú de herramientas para el sistema de cámara.
    /// Accesible desde Tools > Camera System.
    /// </summary>
    public static class CameraSystemMenu
    {
        private const string MENU_PATH = "Tools/Camera System/";

        [MenuItem(MENU_PATH + "Crear Camera System Manager", false, 0)]
        public static void CreateCameraSystemManager()
        {
            // Verificar si ya existe uno
            CameraSystemManager existing = Object.FindAnyObjectByType<CameraSystemManager>();
            if (existing != null)
            {
                Debug.LogWarning("[CameraSystem] Ya existe un CameraSystemManager en la escena.");
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            // Crear el manager
            GameObject managerObj = new GameObject("CameraSystemManager");
            managerObj.AddComponent<CameraSystemManager>();

            Selection.activeGameObject = managerObj;
            EditorGUIUtility.PingObject(managerObj);

            Undo.RegisterCreatedObjectUndo(managerObj, "Create Camera System Manager");
            Debug.Log("[CameraSystem] CameraSystemManager creado. Configura las referencias en el inspector.");
        }

        [MenuItem(MENU_PATH + "Crear Camera Bounds (Límites Globales)", false, 10)]
        public static void CreateGlobalCameraBounds()
        {
            Vector3 position = GetSceneCenter();

            GameObject boundsObj = new GameObject("CameraBounds_Global");
            boundsObj.transform.position = position;

            // Añadir PolygonCollider2D para máxima flexibilidad
            PolygonCollider2D collider = boundsObj.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true; // No queremos que colisione con nada

            // Crear un rectángulo grande por defecto
            float width = 50f;
            float height = 30f;
            collider.points = new Vector2[]
            {
                new Vector2(-width/2, -height/2),
                new Vector2(width/2, -height/2),
                new Vector2(width/2, height/2),
                new Vector2(-width/2, height/2)
            };

            // Poner en una layer que no colisione con nada (opcional)
            // boundsObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            Selection.activeGameObject = boundsObj;
            EditorGUIUtility.PingObject(boundsObj);
            SceneView.lastActiveSceneView?.FrameSelected();

            Undo.RegisterCreatedObjectUndo(boundsObj, "Create Global Camera Bounds");

            Debug.Log("[CameraSystem] Camera Bounds creado. " +
                "Ajusta la forma del PolygonCollider2D en el editor, " +
                "luego asígnalo al campo 'Global Bounds Collider' del CameraSystemManager.");
        }

        [MenuItem(MENU_PATH + "Crear Región de Cámara/Default", false, 20)]
        public static void CreateEmptyRegion()
        {
            CreateRegion("CameraRegion_Custom", null);
        }

        [MenuItem(MENU_PATH + "Crear Región de Cámara/Zoom In (Close-up)", false, 21)]
        public static void CreateZoomInRegion()
        {
            var settings = new CameraRegionSettings
            {
                regionName = "Zoom In Area",
                priority = 15,
                orthographicSize = 8f,
                cameraOffset = Vector2.zero,
                dampingX = 0.3f,
                dampingY = 0.3f,
                blendTimeIn = 0.8f,
                blendTimeOut = 0.5f
            };
            CreateRegion("CameraRegion_ZoomIn", settings);
        }

        [MenuItem(MENU_PATH + "Crear Región de Cámara/Zoom Out (Vista amplia)", false, 22)]
        public static void CreateZoomOutRegion()
        {
            var settings = new CameraRegionSettings
            {
                regionName = "Zoom Out Area",
                priority = 15,
                orthographicSize = 12f,
                cameraOffset = Vector2.zero,
                dampingX = 0.8f,
                dampingY = 0.8f,
                blendTimeIn = 1.2f,
                blendTimeOut = 0.8f
            };
            CreateRegion("CameraRegion_ZoomOut", settings);
        }

        [MenuItem(MENU_PATH + "Crear Región de Cámara/Cámara Fija", false, 23)]
        public static void CreateFixedCameraRegion()
        {
            var settings = new CameraRegionSettings
            {
                regionName = "Fixed Camera Area",
                priority = 20,
                orthographicSize = 10f,
                useFixedPosition = true,
                fixedPosition = GetSceneCenter(),
                blendTimeIn = 1f,
                blendTimeOut = 0.5f
            };
            CreateRegion("CameraRegion_Fixed", settings);
        }

        [MenuItem(MENU_PATH + "Crear Región de Cámara/Área de Combate", false, 24)]
        public static void CreateCombatAreaRegion()
        {
            var settings = new CameraRegionSettings
            {
                regionName = "Combat Area",
                priority = 25,
                orthographicSize = 12f,
                cameraOffset = Vector2.zero,
                dampingX = 0.2f,
                dampingY = 0.2f,
                deadZoneWidth = 0.15f,
                deadZoneHeight = 0.15f,
                lookAheadTime = 0.3f,
                lookAheadSmoothing = 15f,
                blendTimeIn = 0.5f,
                blendTimeOut = 0.3f
            };
            CreateRegion("CameraRegion_Combat", settings);
        }

        [MenuItem(MENU_PATH + "Crear Región de Cámara/Vertical (Plataformas)", false, 25)]
        public static void CreateVerticalRegion()
        {
            var settings = new CameraRegionSettings
            {
                regionName = "Vertical Section",
                priority = 15,
                orthographicSize = 10f,
                cameraOffset = new Vector2(0f, -1f), // Cámara un poco más abajo, muestra más arriba
                dampingX = 0.6f,
                dampingY = 0.3f, // Más responsive verticalmente
                blendTimeIn = 0.7f,
                blendTimeOut = 0.5f
            };
            CreateRegion("CameraRegion_Vertical", settings);
        }

        [MenuItem(MENU_PATH + "Crear Región de Cámara/Cinemática (Sin seguimiento)", false, 26)]
        public static void CreateCinematicRegion()
        {
            var settings = new CameraRegionSettings
            {
                regionName = "Cinematic Area",
                priority = 30,
                orthographicSize = 10f,
                useFixedPosition = true,
                fixedPosition = GetSceneCenter(),
                blendTimeIn = 1.5f,
                blendTimeOut = 1f,
                blendStyleIn = CameraBlendStyle.EaseInOut,
                blendStyleOut = CameraBlendStyle.EaseOut
            };
            CreateRegion("CameraRegion_Cinematic", settings);
        }

        private static void CreateRegion(string name, CameraRegionSettings settings)
        {
            Vector3 position = GetSceneCenter();

            GameObject regionObj = new GameObject(name);
            regionObj.transform.position = position;

            // Añadir BoxCollider2D
            BoxCollider2D collider = regionObj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(10f, 8f);

            // Añadir CameraRegion
            CameraRegion region = regionObj.AddComponent<CameraRegion>();

            // Aplicar settings si se proporcionaron
            if (settings != null)
            {
                // Usamos SerializedObject para aplicar los settings
                SerializedObject serializedRegion = new SerializedObject(region);
                SerializedProperty settingsProp = serializedRegion.FindProperty("settings");

                settingsProp.FindPropertyRelative("regionName").stringValue = settings.regionName;
                settingsProp.FindPropertyRelative("priority").intValue = settings.priority;
                settingsProp.FindPropertyRelative("orthographicSize").floatValue = settings.orthographicSize;
                settingsProp.FindPropertyRelative("cameraOffset").vector2Value = settings.cameraOffset;
                settingsProp.FindPropertyRelative("useFixedPosition").boolValue = settings.useFixedPosition;
                settingsProp.FindPropertyRelative("fixedPosition").vector3Value = settings.fixedPosition;
                settingsProp.FindPropertyRelative("dampingX").floatValue = settings.dampingX;
                settingsProp.FindPropertyRelative("dampingY").floatValue = settings.dampingY;
                settingsProp.FindPropertyRelative("deadZoneWidth").floatValue = settings.deadZoneWidth;
                settingsProp.FindPropertyRelative("deadZoneHeight").floatValue = settings.deadZoneHeight;
                settingsProp.FindPropertyRelative("confineToRegion").boolValue = settings.confineToRegion;
                settingsProp.FindPropertyRelative("blendTimeIn").floatValue = settings.blendTimeIn;
                settingsProp.FindPropertyRelative("blendTimeOut").floatValue = settings.blendTimeOut;
                settingsProp.FindPropertyRelative("blendStyleIn").enumValueIndex = (int)settings.blendStyleIn;
                settingsProp.FindPropertyRelative("blendStyleOut").enumValueIndex = (int)settings.blendStyleOut;
                settingsProp.FindPropertyRelative("lookAheadTime").floatValue = settings.lookAheadTime;
                settingsProp.FindPropertyRelative("lookAheadSmoothing").floatValue = settings.lookAheadSmoothing;

                serializedRegion.ApplyModifiedPropertiesWithoutUndo();
            }

            Selection.activeGameObject = regionObj;
            EditorGUIUtility.PingObject(regionObj);
            SceneView.lastActiveSceneView?.FrameSelected();

            Undo.RegisterCreatedObjectUndo(regionObj, $"Create Camera Region ({name})");
            Debug.Log($"[CameraSystem] Región de cámara '{name}' creada. Ajusta el tamaño del collider según necesites.");
        }

        private static Vector3 GetSceneCenter()
        {
            return Vector3.zero;
        }

        [MenuItem(MENU_PATH + "Mostrar Todas las Regiones", false, 50)]
        public static void ShowAllRegions()
        {
            CameraRegion[] regions = Object.FindObjectsByType<CameraRegion>(FindObjectsSortMode.None);

            if (regions.Length == 0)
            {
                Debug.Log("[CameraSystem] No hay regiones de cámara en la escena.");
                return;
            }

            Debug.Log($"[CameraSystem] === {regions.Length} Regiones de Cámara ===");
            foreach (var region in regions)
            {
                Debug.Log($"  📷 {region.Settings.regionName} (P:{region.Settings.priority}, Size:{region.Settings.orthographicSize}) - {region.gameObject.name}");
            }
        }

        [MenuItem(MENU_PATH + "Documentación", false, 100)]
        public static void OpenDocumentation()
        {
            EditorUtility.DisplayDialog(
                "Camera System - Guía Rápida",
                "🎥 CAMERA SYSTEM - GUÍA RÁPIDA\n\n" +
                "1. CONFIGURACIÓN INICIAL:\n" +
                "   • Crea un CameraSystemManager en la escena\n" +
                "   • Asigna la cámara principal (o se detectará automáticamente)\n" +
                "   • Asigna el jugador como target\n\n" +
                "2. CREAR REGIONES:\n" +
                "   • Usa Tools > Camera System > Crear Región\n" +
                "   • Ajusta el tamaño del BoxCollider2D\n" +
                "   • Configura los parámetros en el inspector\n\n" +
                "3. PARÁMETROS IMPORTANTES:\n" +
                "   • Prioridad: Mayor = se activa antes cuando hay solapamiento\n" +
                "   • Orthographic Size: Menor = más zoom\n" +
                "   • Damping: Mayor = movimiento más suave/lento\n" +
                "   • Dead Zone: Área donde el jugador puede moverse sin que la cámara reaccione\n\n" +
                "4. TAGS:\n" +
                "   • Asegúrate de que el jugador tenga el tag 'Player'\n" +
                "   • O el componente PlayerMovementKinematic\n\n" +
                "¡El sistema se integra automáticamente con tu PauseManager!",
                "Entendido"
            );
        }
    }
}
