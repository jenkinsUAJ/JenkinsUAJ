using UnityEngine;
using Unity.Cinemachine;
using Cronopunk.Movement;
using Unity.VisualScripting;

namespace CameraSystem
{
    /// <summary>
    /// Define una región en el mapa que modifica el comportamiento de la cámara.
    /// Cuando el jugador entra en esta región, se aplica la configuración definida.
    /// Requiere un Collider2D configurado como trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CameraRegion : MonoBehaviour
    {
        [Header("Configuración de la Región")]
        [SerializeField]
        private CameraRegionSettings settings = new CameraRegionSettings();

        [Header("Visualización en Editor")]
        [SerializeField]
        [Tooltip("Color del gizmo para visualizar la región")]
        private Color gizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);

        [SerializeField]
        [Tooltip("Mostrar el área en el editor incluso cuando no está seleccionado")]
        private bool alwaysShowGizmo = true;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugInfo = false;

        // Referencia a la cámara virtual de Cinemachine que controla esta región
        private CinemachineCamera virtualCamera;

        // Collider de la región
        private Collider2D regionCollider;

        // Estado
        private bool isPlayerInside = false;

        // Propiedades públicas
        public CameraRegionSettings Settings => settings;
        public bool IsPlayerInside => isPlayerInside;
        public Collider2D RegionCollider => regionCollider;
        public CinemachineCamera VirtualCamera => virtualCamera;

        private void Awake()
        {
            regionCollider = GetComponent<Collider2D>();

            // Asegurarse de que el collider es trigger
            if (!regionCollider.isTrigger)
            {
                Debug.LogWarning($"[CameraRegion] El collider de '{settings.regionName}' no es trigger. Se cambiará automáticamente.", this);
                regionCollider.isTrigger = true;
            }

            // Crear o obtener la cámara virtual para esta región
            SetupVirtualCamera();
        }

        private void Start()
        {
            // Desactivar la cámara virtual inicialmente (baja prioridad)
            if (virtualCamera != null)
            {
                virtualCamera.Priority = 0;
            }
        }

        /// <summary>
        /// Configura la cámara virtual de Cinemachine para esta región
        /// </summary>
        private void SetupVirtualCamera()
        {
            // Buscar si ya existe una cámara virtual como hijo
            virtualCamera = GetComponentInChildren<CinemachineCamera>();

            if (virtualCamera == null)
            {
                // Crear una nueva cámara virtual
                GameObject vcamObj = new GameObject($"VCam_{settings.regionName}");
                vcamObj.transform.SetParent(transform);
                // Posicionar en Z=-10 para que pueda ver objetos 2D en Z=0
                vcamObj.transform.localPosition = new Vector3(0f, 0f, -10f);

                virtualCamera = vcamObj.AddComponent<CinemachineCamera>();
            }

            // Configurar la cámara con los settings actuales
            ApplySettingsToVirtualCamera();

            // Aplicar límites globales si están configurados en el CameraSystemManager
            ApplyGlobalBoundsFromManager();
        }

        /// <summary>
        /// Aplica los límites globales del CameraSystemManager a esta cámara virtual
        /// </summary>
        public void ApplyGlobalBoundsFromManager()
        {
            if (virtualCamera == null) return;

            // Verificar si el CameraSystemManager existe y tiene bounds globales configurados
            if (CameraSystemManager.Instance != null && CameraSystemManager.Instance.UseGlobalBounds)
            {
                CameraSystemManager.Instance.ApplyGlobalBounds(virtualCamera);
            }
        }

        /// <summary>
        /// Aplica la configuración actual a la cámara virtual
        /// </summary>
        public void ApplySettingsToVirtualCamera()
        {
            if (virtualCamera == null) return;

            // Configurar el objetivo (se asignará dinámicamente por el CameraSystemManager)
            // Por ahora dejamos el Follow y LookAt vacíos

            // Configurar el lens (Orthographic Size)
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = settings.orthographicSize;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            // Configurar clipping planes apropiados para juegos 2D
            // Near debe ser positivo y menor que la distancia de la cámara al plano del juego
            lens.NearClipPlane = 0.3f;  // La cámara está en Z=-10, objetos en Z=0, distancia = 10 unidades
            lens.FarClipPlane = 1000f;  // Suficiente para ver fondos lejanos
            virtualCamera.Lens = lens;

            // Configurar el seguimiento usando CinemachinePositionComposer
            var positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();
            if (positionComposer == null)
            {
                positionComposer = virtualCamera.gameObject.AddComponent<CinemachinePositionComposer>();
            }

            positionComposer.TargetOffset = new Vector3(settings.cameraOffset.x, settings.cameraOffset.y, 0f);
            positionComposer.Damping = new Vector3(settings.dampingX, settings.dampingY, 0f);

            // Configurar composición
            var composition = positionComposer.Composition;
            composition.DeadZone.Enabled = settings.deadZoneWidth > 0 || settings.deadZoneHeight > 0;
            composition.DeadZone.Size = new Vector2(settings.deadZoneWidth, settings.deadZoneHeight);
            composition.HardLimits.Enabled = settings.useHardLimits;
            composition.HardLimits.Size = new Vector2(settings.hardLimitWidth, settings.hardLimitHeight);
            composition.ScreenPosition = new Vector2(0.0f, 0.0f);
            positionComposer.Composition = composition;

            // Configurar lookahead
            positionComposer.Lookahead.Time = settings.lookAheadTime;
            positionComposer.Lookahead.Smoothing = settings.lookAheadSmoothing;

            // Configurar el confiner si es necesario
            if (settings.confineToRegion && regionCollider != null)
            {
                var confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
                if (confiner == null)
                {
                    confiner = virtualCamera.gameObject.AddComponent<CinemachineConfiner2D>();
                }
                confiner.BoundingShape2D = regionCollider;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[CameraRegion] Configuración aplicada a '{settings.regionName}'", this);
            }
        }

        /// <summary>
        /// Activa esta región de cámara
        /// </summary>
        public void ActivateRegion()
        {
            if (virtualCamera != null)
            {
                virtualCamera.Priority = settings.priority;

                if (showDebugInfo)
                {
                    Debug.Log($"[CameraRegion] Región '{settings.regionName}' ACTIVADA con prioridad {settings.priority}", this);
                }
            }
        }

        /// <summary>
        /// Desactiva esta región de cámara
        /// </summary>
        public void DeactivateRegion()
        {
            if (virtualCamera != null)
            {
                virtualCamera.Priority = 0;

                if (showDebugInfo)
                {
                    Debug.Log($"[CameraRegion] Región '{settings.regionName}' DESACTIVADA", this);
                }
            }
        }

        /// <summary>
        /// Asigna el target (jugador) a la cámara virtual
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (virtualCamera != null)
            {
                if (settings.useFixedPosition)
                {
                    // Para posición fija, no seguimos al jugador
                    virtualCamera.Follow = null;
                    // Establecer Z en -10 para que la cámara enfoque correctamente el juego 2D en Z=0
                    virtualCamera.transform.position = new Vector3(settings.fixedPosition.x, settings.fixedPosition.y, -10f);
                }
                else
                {
                    virtualCamera.Follow = target;
                }
                virtualCamera.LookAt = target;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Verificar si es el jugador
            if (IsPlayer(other))
            {
                isPlayerInside = true;

                // Notificar al CameraSystemManager (él decidirá si activar o poner en cola)
                CameraSystemManager.Instance?.OnPlayerEnteredRegion(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // Verificar si es el jugador
            if (IsPlayer(other))
            {
                isPlayerInside = false;

                // Notificar al CameraSystemManager (él decidirá si desactivar o poner en cola)
                CameraSystemManager.Instance?.OnPlayerExitedRegion(this);
            }
        }

        /// <summary>
        /// Verifica si el collider pertenece al jugador
        /// </summary>
        private bool IsPlayer(Collider2D other)
        {
            return CameraSystemManager.Instance?.PlayerTarget == other.transform;
        }

        /// <summary>
        /// Obtiene el estilo de blend de Cinemachine correspondiente
        /// </summary>
        public CinemachineBlendDefinition.Styles GetCinemachineBlendStyle(CameraBlendStyle style)
        {
            return style switch
            {
                CameraBlendStyle.Cut => CinemachineBlendDefinition.Styles.Cut,
                CameraBlendStyle.Linear => CinemachineBlendDefinition.Styles.Linear,
                CameraBlendStyle.EaseIn => CinemachineBlendDefinition.Styles.EaseIn,
                CameraBlendStyle.EaseOut => CinemachineBlendDefinition.Styles.EaseOut,
                CameraBlendStyle.EaseInOut => CinemachineBlendDefinition.Styles.EaseInOut,
                CameraBlendStyle.HardIn => CinemachineBlendDefinition.Styles.HardIn,
                CameraBlendStyle.HardOut => CinemachineBlendDefinition.Styles.HardOut,
                _ => CinemachineBlendDefinition.Styles.EaseInOut
            };
        }

#if UNITY_EDITOR
        private static GUIStyle _nameStyle;
        private static GUIStyle _detailsStyle;
        private static GUIStyle _fixedPosStyle;

        private static GUIStyle GetNameStyle()
        {
            if (_nameStyle == null)
            {
                _nameStyle = new GUIStyle()
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }
            return _nameStyle;
        }

        private static GUIStyle GetDetailsStyle()
        {
            if (_detailsStyle == null)
            {
                _detailsStyle = new GUIStyle()
                {
                    fontSize = 8,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 0.8f) }
                };
            }
            return _detailsStyle;
        }

        private static GUIStyle GetFixedPosStyle()
        {
            if (_fixedPosStyle == null)
            {
                _fixedPosStyle = new GUIStyle()
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.yellow }
                };
            }
            return _fixedPosStyle;
        }

        private void OnDrawGizmos()
        {
            if (alwaysShowGizmo)
            {
                DrawRegionGizmo(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawRegionGizmo(true);
        }

        private void DrawRegionGizmo(bool isSelected)
        {
            // Protección null para settings.
            if (settings == null)
            {
                settings = new CameraRegionSettings();
            }
            Collider2D col = regionCollider != null ? regionCollider : GetComponent<Collider2D>();
            if (col == null) return;
            Color color = isSelected ? new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f) : gizmoColor;
            Gizmos.color = color;
            if (col is BoxCollider2D box)
            {
                Vector3 center = transform.position + (Vector3)box.offset;
                Vector3 size = new Vector3(box.size.x * transform.lossyScale.x, box.size.y * transform.lossyScale.y, 0.1f);
                Gizmos.DrawCube(center, size);
                // Dibujar borde
                Gizmos.color = new Color(color.r, color.g, color.b, 1f);
                Gizmos.DrawWireCube(center, size);
            }
            else if (col is CircleCollider2D circle)
            {
                Vector3 center = transform.position + (Vector3)circle.offset;
                float radius = circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
                Gizmos.DrawSphere(center, radius);
            }
            else if (col is PolygonCollider2D polygon)
            {
                // Para polígonos, dibujamos las líneas
                Gizmos.color = new Color(color.r, color.g, color.b, 1f);
                Vector2[] points = polygon.points;
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 p1 = transform.TransformPoint(points[i]);
                    Vector3 p2 = transform.TransformPoint(points[(i + 1) % points.Length]);
                    Gizmos.DrawLine(p1, p2);
                }
            }
            // Visualización de la cámara si usa posición fija
            if (settings.useFixedPosition)
            {
                Vector3 fixedPos3D = new Vector3(settings.fixedPosition.x, settings.fixedPosition.y, 0f);
                UnityEditor.Handles.color = Color.yellow;
                UnityEditor.Handles.DrawWireDisc(fixedPos3D, Vector3.forward, 0.5f);
                UnityEditor.Handles.Label(fixedPos3D + Vector3.up, "Fixed Position", GetFixedPosStyle());
                // Dibujar rectángulo ortográfico
                DrawOrthographicView(fixedPos3D);
            }
            // Dibujar etiqueta con información
            Vector3 namePosition = transform.position + Vector3.up;
            Vector3 detailsPosition = transform.position + Vector3.down;
            // Dibujar nombre
            UnityEditor.Handles.Label(namePosition, settings.regionName, GetNameStyle());
            // Dibujar detalles debajo
            string details = $"Ortho Size: {settings.orthographicSize:F1}";
            if (!settings.useFixedPosition)
            {
                details += $"\nOffset: ({settings.cameraOffset.x:F1}; {settings.cameraOffset.y:F1})";
            }
            UnityEditor.Handles.Label(detailsPosition, details, GetDetailsStyle());
        }

        private void DrawOrthographicView(Vector3 cameraPos)
        {
            // Calcular el rectángulo visible con tamaño ortográfico
            float halfHeight = settings.orthographicSize;
            float halfWidth = halfHeight * (16f / 9f); // Asumiendo 16:9

            Vector3 topLeft = cameraPos + new Vector3(-halfWidth, halfHeight, 0f);
            Vector3 topRight = cameraPos + new Vector3(halfWidth, halfHeight, 0f);
            Vector3 bottomLeft = cameraPos + new Vector3(-halfWidth, -halfHeight, 0f);
            Vector3 bottomRight = cameraPos + new Vector3(halfWidth, -halfHeight, 0f);

            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.3f);

            // Dibujar el rectángulo de la vista ortográfica
            UnityEditor.Handles.DrawLine(topLeft, topRight);
            UnityEditor.Handles.DrawLine(topRight, bottomRight);
            UnityEditor.Handles.DrawLine(bottomRight, bottomLeft);
            UnityEditor.Handles.DrawLine(bottomLeft, topLeft);

            // Dibujar líneas hacia el centro para indicar la posición de la cámara
            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.15f);
            UnityEditor.Handles.DrawLine(cameraPos, topLeft);
            UnityEditor.Handles.DrawLine(cameraPos, topRight);
            UnityEditor.Handles.DrawLine(cameraPos, bottomLeft);
            UnityEditor.Handles.DrawLine(cameraPos, bottomRight);
        }

        /// <summary>
        /// Llamado cuando se cambia cualquier valor en el Inspector.
        /// Permite actualizar la cámara virtual en tiempo real durante Play Mode.
        /// </summary>
        private void OnValidate()
        {
            // Solo aplicar cambios si estamos en Play Mode y la cámara virtual existe
            if (Application.isPlaying && virtualCamera != null)
            {
                // Usar delayCall para evitar warnings de Unity sobre modificar objetos durante OnValidate
                UnityEditor.EditorApplication.delayCall += OnValidateDelayed;
            }
        }

        private void OnValidateDelayed()
        {
            // Verificar que el objeto todavía existe (podría haber sido destruido)
            if (this == null || virtualCamera == null) return;

            // Aplicar todos los settings a la cámara virtual
            ApplySettingsToVirtualCamera();

            // Actualizar la prioridad si el jugador está dentro
            if (isPlayerInside)
            {
                virtualCamera.Priority = settings.priority;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[CameraRegion] Configuración actualizada en runtime para '{settings.regionName}'", this);
            }
        }
#endif
    }
}
