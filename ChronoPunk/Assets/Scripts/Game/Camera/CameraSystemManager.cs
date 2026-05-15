using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Cronopunk.Movement;

namespace CameraSystem
{
    /// <summary>
    /// Gestor principal del sistema de cámara.
    /// Se encarga de:
    /// - Configurar el CinemachineBrain
    /// - Manejar la cámara por defecto
    /// - Gestionar las transiciones entre regiones
    /// - Integrar con el sistema de pausa
    /// Singleton - debe existir uno en la escena.
    /// </summary>
    public class CameraSystemManager : PausableMonoBehaviour
    {
        public static CameraSystemManager Instance { get; private set; }

        [Header("Referencias")]
        [SerializeField]
        [Tooltip("Referencia a la cámara principal. Si está vacía, se buscará automáticamente.")]
        private Camera mainCamera;

        [SerializeField]
        [Tooltip("Transform del jugador a seguir. Si está vacío, se buscará automáticamente.")]
        private Transform playerTarget;

        [Header("Límites Globales de Cámara (Camera Bounds)")]
        [SerializeField]
        [Tooltip("Si está activo, la cámara nunca saldrá de los límites globales definidos.")]
        private bool useGlobalBounds = false;

        [SerializeField]
        [Tooltip("Collider 2D que define los límites globales de la cámara. Puede ser un PolygonCollider2D o CompositeCollider2D.")]
        private Collider2D globalBoundsCollider;

        [Header("Configuración por Defecto")]
        [SerializeField]
        [Tooltip("Configuración de la cámara cuando el jugador no está en ninguna región")]
        private CameraRegionSettings defaultSettings = new CameraRegionSettings()
        {
            regionName = "Default",
            priority = 5,
            orthographicSize = 10f,
            cameraOffset = Vector2.zero,
            dampingX = 0.5f,
            dampingY = 0.5f,
            blendTimeIn = 1f,
            blendTimeOut = 1f,
            blendStyleIn = CameraBlendStyle.EaseInOut,
            blendStyleOut = CameraBlendStyle.EaseInOut
        };

        [Header("Override Cámara Menú de Sombras")]
        [SerializeField]
        [Tooltip("CinemachineCamera usada como cámara principal mientras el menú de sombras está activo.")]
        private CinemachineCamera shadowMenuVirtualCamera;

        [SerializeField]
        [Tooltip("Prioridad temporal para forzar la cámara del menú por encima de regiones y cámara por defecto.")]
        [Range(101, 2000)]
        private int shadowMenuPriority = 1000;

        [Header("Cooldown de Transiciones")]
        [SerializeField]
        [Tooltip("Tiempo mínimo entre cambios de cámara (en segundos). Evita cambios rápidos consecutivos.")]
        [Range(0f, 5f)]
        private float transitionCooldown = 0.5f;

        [Header("Debug")]
        [SerializeField]
        private bool showDebugInfo = false;

        // Componentes de Cinemachine
        private CinemachineBrain cinemachineBrain;
        private CinemachineCamera defaultVirtualCamera;

        // Estado
        private List<CameraRegion> activeRegions = new List<CameraRegion>();
        private CameraRegion currentActiveRegion;
        private bool isInitialized = false;

        // Estado de override para menú de sombras
        private bool isShadowMenuCameraOverrideActive = false;
        private int shadowMenuOriginalPriority = 0;
        private bool shadowMenuPriorityCaptured = false;
        private CinemachineBlendDefinition cachedBlendBeforeShadowMenuOverride;
        private bool hasCachedBlendBeforeShadowMenuOverride = false;

        // Cooldown state
        private float cooldownTimer = 0f;
        private bool isInCooldown = false;
        private bool pendingRegionUpdate = false;

        // Propiedades públicas
        public Camera MainCamera => mainCamera;
        public Transform PlayerTarget => playerTarget;
        public CameraRegion CurrentActiveRegion => currentActiveRegion;
        public CameraRegionSettings DefaultSettings => defaultSettings;
        public bool IsInitialized => isInitialized;
        public bool UseGlobalBounds => useGlobalBounds;
        public Collider2D GlobalBoundsCollider => globalBoundsCollider;
        public bool IsInCooldown => isInCooldown;
        public float CooldownRemaining => cooldownTimer;
        public bool IsShadowMenuCameraOverrideActive => isShadowMenuCameraOverrideActive;
        public CinemachineCamera ShadowMenuVirtualCamera => shadowMenuVirtualCamera;

        private void Awake()
        {
            // Singleton
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[CameraSystemManager] Ya existe una instancia. Destruyendo duplicado.", this);
                Destroy(gameObject);
                return;
            }

            Initialize();
        }

        private void Update()
        {
            // Gestionar el cooldown
            if (isInCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    isInCooldown = false;
                    cooldownTimer = 0f;

                    // Si hay una actualización pendiente, aplicarla ahora
                    if (pendingRegionUpdate)
                    {
                        pendingRegionUpdate = false;
                        ApplyPendingRegionChange();
                    }
                }
            }
        }

        /// <summary>
        /// Aplica el cambio de región que estaba pendiente durante el cooldown
        /// </summary>
        private void ApplyPendingRegionChange()
        {
            // Recalcular cuál debería ser la región activa basándose en las regiones donde está el jugador
            CameraRegion newActiveRegion = null;

            if (activeRegions.Count > 0)
            {
                // Encontrar la región con mayor prioridad
                newActiveRegion = activeRegions[0];
                foreach (var region in activeRegions)
                {
                    if (region.Settings.priority > newActiveRegion.Settings.priority)
                    {
                        newActiveRegion = region;
                    }
                }
            }

            // Solo cambiar si es diferente a la actual
            if (newActiveRegion != currentActiveRegion)
            {
                // Desactivar la región anterior
                if (currentActiveRegion != null)
                {
                    currentActiveRegion.DeactivateRegion();
                }

                // Activar la nueva región
                if (newActiveRegion != null)
                {
                    newActiveRegion.ActivateRegion();

                    // Configurar el blend
                    if (cinemachineBrain != null)
                    {
                        cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(
                            newActiveRegion.GetCinemachineBlendStyle(newActiveRegion.Settings.blendStyleIn),
                            newActiveRegion.Settings.blendTimeIn
                        );
                    }

                    if (showDebugInfo)
                    {
                        Debug.Log($"[CameraSystemManager] Cooldown terminado - Activando región pendiente: {newActiveRegion.Settings.regionName}", this);
                    }
                }
                else
                {
                    // Restaurar blend por defecto usando los valores de defaultSettings
                    if (cinemachineBrain != null)
                    {
                        cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(
                            GetCinemachineBlendStyle(defaultSettings.blendStyleIn),
                            defaultSettings.blendTimeIn
                        );
                    }

                    if (showDebugInfo)
                    {
                        Debug.Log("[CameraSystemManager] Cooldown terminado - Volviendo a cámara por defecto", this);
                    }
                }

                currentActiveRegion = newActiveRegion;

                // Iniciar nuevo cooldown
                StartCooldown();
            }
        }

        /// <summary>
        /// Inicia el cooldown de transición
        /// </summary>
        private void StartCooldown()
        {
            if (transitionCooldown > 0f)
            {
                isInCooldown = true;
                cooldownTimer = transitionCooldown;
            }
        }

        private void Initialize()
        {
            // Obtener o crear la cámara principal
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("[CameraSystemManager] No se encontró la cámara principal.", this);
                    return;
                }
            }

            // Asegurar que la cámara usa proyección ortográfica
            if (!mainCamera.orthographic)
            {
                Debug.LogWarning("[CameraSystemManager] La cámara estaba en modo perspectiva. Cambiando a ortográfico.", this);
                mainCamera.orthographic = true;
            }

            // Configurar CinemachineBrain
            SetupCinemachineBrain();

            // Configurar cámara virtual por defecto
            SetupDefaultVirtualCamera();

            // Buscar jugador si no está asignado
            if (playerTarget == null)
            {
                FindPlayer();
            }

            // Asignar target a la cámara por defecto
            if (playerTarget != null)
            {
                defaultVirtualCamera.Follow = playerTarget;
                defaultVirtualCamera.LookAt = playerTarget;
            }

            isInitialized = true;

            // Aplicar bounds globales a todas las regiones existentes
            ApplyGlobalBoundsToAllRegions();

            // Aplicar bounds globales a la cámara de menú de sombras si existe
            if (shadowMenuVirtualCamera != null)
            {
                ApplyGlobalBounds(shadowMenuVirtualCamera);
            }

            if (showDebugInfo)
            {
                Debug.Log("[CameraSystemManager] Inicializado correctamente.", this);
            }
        }

        /// <summary>
        /// Aplica los bounds globales a todas las regiones de cámara existentes
        /// </summary>
        private void ApplyGlobalBoundsToAllRegions()
        {
            if (!useGlobalBounds || globalBoundsCollider == null) return;

            var allRegions = FindObjectsByType<CameraRegion>(FindObjectsSortMode.None);
            foreach (var region in allRegions)
            {
                if (region.VirtualCamera != null)
                {
                    ApplyGlobalBounds(region.VirtualCamera);
                }
            }
        }

        private void SetupCinemachineBrain()
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();

            if (cinemachineBrain == null)
            {
                cinemachineBrain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            // Configurar el brain con los valores de defaultSettings
            cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(
                GetCinemachineBlendStyle(defaultSettings.blendStyleIn),
                defaultSettings.blendTimeIn
            );

            cinemachineBrain.UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
        }

        private void SetupDefaultVirtualCamera()
        {
            // Buscar si ya existe una cámara por defecto
            Transform existingDefault = transform.Find("DefaultVirtualCamera");

            if (existingDefault != null)
            {
                defaultVirtualCamera = existingDefault.GetComponent<CinemachineCamera>();
            }

            if (defaultVirtualCamera == null)
            {
                // Crear la cámara virtual por defecto
                GameObject vcamObj = new GameObject("DefaultVirtualCamera");
                vcamObj.transform.SetParent(transform);
                // Posicionar en Z=-10 para que pueda ver objetos 2D en Z=0
                vcamObj.transform.localPosition = new Vector3(0f, 0f, -10f);

                defaultVirtualCamera = vcamObj.AddComponent<CinemachineCamera>();
            }

            // Configurar la cámara por defecto
            ApplySettingsToVirtualCamera(defaultVirtualCamera, defaultSettings);
            defaultVirtualCamera.Priority = defaultSettings.priority;

            // Aplicar límites globales si están configurados
            ApplyGlobalBounds(defaultVirtualCamera);
        }

        /// <summary>
        /// Aplica los límites globales de cámara a una cámara virtual
        /// </summary>
        public void ApplyGlobalBounds(CinemachineCamera vcam)
        {
            if (!useGlobalBounds || globalBoundsCollider == null) return;

            var confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner == null)
            {
                confiner = vcam.gameObject.AddComponent<CinemachineConfiner2D>();
            }
            confiner.BoundingShape2D = globalBoundsCollider;

            if (showDebugInfo)
            {
                Debug.Log($"[CameraSystemManager] Límites globales aplicados a {vcam.name}", this);
            }
        }

        /// <summary>
        /// Configura los límites globales de cámara en tiempo de ejecución
        /// </summary>
        public void SetGlobalBounds(Collider2D boundsCollider, bool enable = true)
        {
            globalBoundsCollider = boundsCollider;
            useGlobalBounds = enable;

            // Aplicar a la cámara por defecto
            ApplyGlobalBounds(defaultVirtualCamera);

            // Aplicar a todas las regiones existentes
            var allRegions = FindObjectsByType<CameraRegion>(FindObjectsSortMode.None);
            foreach (var region in allRegions)
            {
                if (region.VirtualCamera != null)
                {
                    ApplyGlobalBounds(region.VirtualCamera);
                }
            }
        }

        /// <summary>
        /// Desactiva los límites globales de cámara
        /// </summary>
        public void DisableGlobalBounds()
        {
            useGlobalBounds = false;

            // Eliminar confiner de la cámara por defecto
            var confiner = defaultVirtualCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
            {
                Destroy(confiner);
            }
        }

        /// <summary>
        /// Aplica una configuración a una cámara virtual
        /// </summary>
        private void ApplySettingsToVirtualCamera(CinemachineCamera vcam, CameraRegionSettings settings)
        {
            // Configurar el lens (ortográfico)
            var lens = vcam.Lens;
            lens.OrthographicSize = settings.orthographicSize;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            // Configurar clipping planes apropiados para juegos 2D
            // Near debe ser positivo y menor que la distancia de la cámara al plano del juego
            lens.NearClipPlane = 0.3f;  // La cámara está en Z=-10, objetos en Z=0, distancia = 10 unidades
            lens.FarClipPlane = 1000f;  // Suficiente para ver fondos lejanos
            vcam.Lens = lens;

            // Configurar el position composer
            var positionComposer = vcam.GetComponent<CinemachinePositionComposer>();
            if (positionComposer == null)
            {
                positionComposer = vcam.gameObject.AddComponent<CinemachinePositionComposer>();
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

            positionComposer.Lookahead.Time = settings.lookAheadTime;
            positionComposer.Lookahead.Smoothing = settings.lookAheadSmoothing;
        }

        /// <summary>
        /// Busca al jugador en la escena
        /// </summary>
        private void FindPlayer()
        {
            // Intentar por tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
                return;
            }

            // Intentar por componente
            var playerMovement = FindAnyObjectByType<PlayerMovementKinematic>();
            if (playerMovement != null)
            {
                playerTarget = playerMovement.transform;
                return;
            }

            Debug.LogWarning("[CameraSystemManager] No se encontró al jugador. Asigna el target manualmente.", this);
        }

        /// <summary>
        /// Asigna manualmente el target de la cámara
        /// </summary>
        public void SetPlayerTarget(Transform target)
        {
            playerTarget = target;

            // Actualizar la cámara por defecto
            if (defaultVirtualCamera != null)
            {
                defaultVirtualCamera.Follow = target;
                defaultVirtualCamera.LookAt = target;
            }

            // Actualizar todas las regiones activas
            foreach (var region in activeRegions)
            {
                region.SetTarget(target);
            }

            // También actualizar todas las regiones registradas
            var allRegions = FindObjectsByType<CameraRegion>(FindObjectsSortMode.None);
            foreach (var region in allRegions)
            {
                region.SetTarget(target);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[CameraSystemManager] Target asignado: {target.name}", this);
            }
        }

        /// <summary>
        /// Llamado cuando el jugador entra en una región
        /// </summary>
        public void OnPlayerEnteredRegion(CameraRegion region)
        {
            // Siempre registrar la región como activa (el jugador está dentro)
            if (!activeRegions.Contains(region))
            {
                activeRegions.Add(region);
            }

            // Asignar el target a la región
            if (playerTarget != null)
            {
                region.SetTarget(playerTarget);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[CameraSystemManager] Jugador entró en región: {region.Settings.regionName}" +
                    (isInCooldown ? " (en cooldown, cambio pendiente)" : ""), this);
            }

            // Si estamos en cooldown, marcar que hay un cambio pendiente
            if (isInCooldown)
            {
                pendingRegionUpdate = true;
                return;
            }

            // Verificar si esta región debería activarse (tiene mayor prioridad que la actual)
            bool shouldActivate = currentActiveRegion == null ||
                                  region.Settings.priority > currentActiveRegion.Settings.priority;

            if (shouldActivate)
            {
                // Desactivar región anterior si existe
                if (currentActiveRegion != null && currentActiveRegion != region)
                {
                    currentActiveRegion.DeactivateRegion();
                }

                // Activar esta región
                region.ActivateRegion();
                currentActiveRegion = region;

                // Configurar el blend para esta transición
                if (cinemachineBrain != null)
                {
                    cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(
                        region.GetCinemachineBlendStyle(region.Settings.blendStyleIn),
                        region.Settings.blendTimeIn
                    );
                }

                // Iniciar cooldown
                StartCooldown();
            }
        }

        /// <summary>
        /// Llamado cuando el jugador sale de una región
        /// </summary>
        public void OnPlayerExitedRegion(CameraRegion region)
        {
            // Siempre quitar la región de la lista de activas
            activeRegions.Remove(region);

            if (showDebugInfo)
            {
                Debug.Log($"[CameraSystemManager] Jugador salió de región: {region.Settings.regionName}" +
                    (isInCooldown ? " (en cooldown, cambio pendiente)" : ""), this);
            }

            // Si estamos en cooldown, marcar que hay un cambio pendiente
            if (isInCooldown)
            {
                pendingRegionUpdate = true;
                return;
            }

            // Solo procesar si salimos de la región activa actual
            if (region == currentActiveRegion)
            {
                // Desactivar la región
                region.DeactivateRegion();

                // Buscar la siguiente región con mayor prioridad, o null si no hay ninguna
                CameraRegion nextRegion = null;
                if (activeRegions.Count > 0)
                {
                    nextRegion = activeRegions[0];
                    foreach (var r in activeRegions)
                    {
                        if (r.Settings.priority > nextRegion.Settings.priority)
                        {
                            nextRegion = r;
                        }
                    }
                }

                // Configurar el blend para la transición de salida
                if (cinemachineBrain != null)
                {
                    cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(
                        region.GetCinemachineBlendStyle(region.Settings.blendStyleOut),
                        region.Settings.blendTimeOut
                    );
                }

                // Activar la siguiente región o dejar la cámara por defecto
                if (nextRegion != null)
                {
                    nextRegion.ActivateRegion();
                }

                currentActiveRegion = nextRegion;

                // Iniciar cooldown
                StartCooldown();
            }
        }

        /// <summary>
        /// Activa el override de la cámara de menú de sombras.
        /// </summary>
        public bool ActivateShadowMenuCamera(float blendTime, CameraBlendStyle blendStyle)
        {
            if (shadowMenuVirtualCamera == null)
            {
                Debug.LogWarning("[CameraSystemManager] No hay cámara de menú de sombras asignada.", this);
                return false;
            }

            if (!shadowMenuPriorityCaptured)
            {
                shadowMenuOriginalPriority = shadowMenuVirtualCamera.Priority;
                shadowMenuPriorityCaptured = true;
            }

            if (!isShadowMenuCameraOverrideActive && cinemachineBrain != null)
            {
                cachedBlendBeforeShadowMenuOverride = cinemachineBrain.DefaultBlend;
                hasCachedBlendBeforeShadowMenuOverride = true;
            }

            if (cinemachineBrain != null)
            {
                cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(
                    GetCinemachineBlendStyle(blendStyle),
                    blendTime
                );
            }

            shadowMenuVirtualCamera.Priority = shadowMenuPriority;
            isShadowMenuCameraOverrideActive = true;

            if (showDebugInfo)
            {
                Debug.Log("[CameraSystemManager] Override activado: cámara de menú de sombras", this);
            }

            return true;
        }

        /// <summary>
        /// Fuerza una transición hacia la cámara de menú de sombras.
        /// </summary>
        public bool TransitionToShadowMenuCamera(float blendTime, CameraBlendStyle blendStyle = CameraBlendStyle.EaseInOut)
        {
            return ActivateShadowMenuCamera(blendTime, blendStyle);
        }

        /// <summary>
        /// Desactiva el override de cámara de menú de sombras y restaura prioridad/blend previos.
        /// </summary>
        public bool DeactivateShadowMenuCamera(float blendTime = -1f, CameraBlendStyle blendStyle = CameraBlendStyle.EaseInOut)
        {
            if (!isShadowMenuCameraOverrideActive)
            {
                return false;
            }

            if (shadowMenuVirtualCamera == null)
            {
                isShadowMenuCameraOverrideActive = false;
                return false;
            }

            if (cinemachineBrain != null)
            {
                if (blendTime >= 0f)
                {
                    cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(
                        GetCinemachineBlendStyle(blendStyle),
                        blendTime
                    );
                }
                else if (hasCachedBlendBeforeShadowMenuOverride)
                {
                    cinemachineBrain.DefaultBlend = cachedBlendBeforeShadowMenuOverride;
                }
            }

            shadowMenuVirtualCamera.Priority = shadowMenuPriorityCaptured ? shadowMenuOriginalPriority : 0;
            isShadowMenuCameraOverrideActive = false;

            if (showDebugInfo)
            {
                Debug.Log("[CameraSystemManager] Override desactivado: restaurando flujo normal", this);
            }

            return true;
        }

        /// <summary>
        /// Fuerza una transición inmediata a una configuración específica
        /// </summary>
        public void ForceTransitionTo(CameraRegionSettings settings, float blendTime = -1f)
        {
            if (blendTime < 0) blendTime = defaultSettings.blendTimeIn;

            ApplySettingsToVirtualCamera(defaultVirtualCamera, settings);

            // Desactivar todas las regiones y activar la por defecto
            foreach (var region in activeRegions)
            {
                region.DeactivateRegion();
            }
            activeRegions.Clear();

            defaultVirtualCamera.Priority = 100; // Alta prioridad temporal

            if (showDebugInfo)
            {
                Debug.Log($"[CameraSystemManager] Transición forzada a configuración: {settings.regionName}", this);
            }
        }

        /// <summary>
        /// Restaura la cámara al comportamiento normal
        /// </summary>
        public void RestoreNormalBehavior()
        {
            DeactivateShadowMenuCamera();

            // Restaurar prioridad por defecto
            ApplySettingsToVirtualCamera(defaultVirtualCamera, defaultSettings);
            defaultVirtualCamera.Priority = defaultSettings.priority;

            if (showDebugInfo)
            {
                Debug.Log("[CameraSystemManager] Comportamiento normal restaurado", this);
            }
        }

        /// <summary>
        /// Hace un shake de cámara
        /// </summary>
        public void ShakeCamera(float intensity = 1f, float duration = 0.3f)
        {
            // Buscar o añadir un componente de impulse
            var impulseSource = mainCamera.GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null)
            {
                impulseSource = mainCamera.gameObject.AddComponent<CinemachineImpulseSource>();
            }

            impulseSource.GenerateImpulse(intensity);

            if (showDebugInfo)
            {
                Debug.Log($"[CameraSystemManager] Camera shake: intensidad={intensity}, duración={duration}", this);
            }
        }

        /// <summary>
        /// Convierte el estilo de blend personalizado al de Cinemachine
        /// </summary>
        private CinemachineBlendDefinition.Styles GetCinemachineBlendStyle(CameraBlendStyle style)
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

        #region Integración con Sistema de Pausa

        public override void SetPaused(bool isPaused)
        {
            base.SetPaused(isPaused);

            // Pausar/reanudar el CinemachineBrain
            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled = !isPaused;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[CameraSystemManager] Pausa: {isPaused}", this);
            }
        }

        #endregion

        #region Métodos de Utilidad

        /// <summary>
        /// Obtiene la configuración actual de la cámara (de la región activa o la por defecto)
        /// </summary>
        public CameraRegionSettings GetCurrentSettings()
        {
            return currentActiveRegion != null ? currentActiveRegion.Settings : defaultSettings;
        }

        /// <summary>
        /// Obtiene todas las regiones de cámara en la escena
        /// </summary>
        public CameraRegion[] GetAllRegions()
        {
            return FindObjectsByType<CameraRegion>(FindObjectsSortMode.None);
        }

        /// <summary>
        /// Obtiene las regiones actualmente activas (donde está el jugador)
        /// </summary>
        public List<CameraRegion> GetActiveRegions()
        {
            return new List<CameraRegion>(activeRegions);
        }

        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Instance == this)
            {
                Instance = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Dibujar información de debug en el editor
            if (mainCamera != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(mainCamera.transform.position, 0.5f);
            }

            if (playerTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(mainCamera != null ? mainCamera.transform.position : transform.position, playerTarget.position);
            }
        }

        /// <summary>
        /// Llamado cuando se cambia cualquier valor en el Inspector.
        /// Permite actualizar la cámara por defecto en tiempo real durante Play Mode.
        /// </summary>
        private void OnValidate()
        {
            // Solo aplicar cambios si estamos en Play Mode y la cámara virtual existe
            if (Application.isPlaying && defaultVirtualCamera != null)
            {
                // Usar delayCall para evitar warnings de Unity sobre modificar objetos durante OnValidate
                UnityEditor.EditorApplication.delayCall += OnValidateDelayed;
            }
        }

        private void OnValidateDelayed()
        {
            // Verificar que el objeto todavía existe
            if (this == null || defaultVirtualCamera == null) return;

            // Aplicar configuración por defecto a la cámara virtual
            ApplySettingsToVirtualCamera(defaultVirtualCamera, defaultSettings);

            // Actualizar la prioridad
            defaultVirtualCamera.Priority = defaultSettings.priority;

            // Actualizar el blend time del brain si cambió
            if (cinemachineBrain != null)
            {
                cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(
                    GetCinemachineBlendStyle(defaultSettings.blendStyleIn),
                    defaultSettings.blendTimeIn
                );
            }

            // Actualizar global bounds si cambió
            if (useGlobalBounds && globalBoundsCollider != null)
            {
                ApplyGlobalBounds(defaultVirtualCamera);

                if (shadowMenuVirtualCamera != null)
                {
                    ApplyGlobalBounds(shadowMenuVirtualCamera);
                }

                // También actualizar todas las regiones
                var allRegions = FindObjectsByType<CameraRegion>(FindObjectsSortMode.None);
                foreach (var region in allRegions)
                {
                    if (region.VirtualCamera != null)
                    {
                        ApplyGlobalBounds(region.VirtualCamera);
                    }
                }
            }

            if (showDebugInfo)
            {
                Debug.Log("[CameraSystemManager] Configuración por defecto actualizada en runtime", this);
            }
        }
#endif
    }
}
