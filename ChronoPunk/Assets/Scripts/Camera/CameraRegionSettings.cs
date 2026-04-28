using System;
using UnityEngine;

namespace CameraSystem
{
    /// <summary>
    /// Configuración de una región de cámara.
    /// Define cómo se comportará la cámara cuando el jugador entre en esta región.
    /// </summary>
    [Serializable]
    public class CameraRegionSettings
    {
        [Header("Identificación")]
        [Tooltip("Nombre descriptivo para identificar esta región en el editor")]
        public string regionName = "Nueva Región";

        [Header("Prioridad")]
        [Tooltip("Prioridad de esta región. Mayor valor = mayor prioridad cuando hay solapamiento")]
        [Range(0, 100)]
        public int priority = 10;

        [Header("Tamaño Ortográfico (Zoom)")]
        [Tooltip("Tamaño ortográfico de la cámara en esta región. Menor valor = más zoom")]
        [Range(1f, 30f)]
        public float orthographicSize = 10f;

        [Header("Posicionamiento")]
        [Tooltip("Offset de la cámara respecto al jugador en unidades del mundo (X = horizontal, Y = vertical). Ej: (0, 2) centra la cámara 2 unidades arriba del jugador.")]
        public Vector2 cameraOffset = Vector2.zero;

        [Tooltip("Si está activo, la cámara se centra en un punto fijo en lugar de seguir al jugador")]
        public bool useFixedPosition = false;

        [Tooltip("Posición fija de la cámara en unidades del mundo (X = horizontal, Y = vertical).")]
        public Vector2 fixedPosition = Vector2.zero;

        [Header("Suavizado del Seguimiento")]
        [Tooltip("Tiempo de suavizado horizontal (mayor = más lento/suave)")]
        [Range(0f, 5f)]
        public float dampingX = 0.5f;

        [Tooltip("Tiempo de suavizado vertical (mayor = más lento/suave)")]
        [Range(0f, 5f)]
        public float dampingY = 0.5f;

        [Header("Dead Zone (Zona Muerta)")]
        [Tooltip("Ancho de la zona muerta (el jugador puede moverse sin que la cámara reaccione)")]
        [Range(0f, 1f)]
        public float deadZoneWidth = 0.1f;

        [Tooltip("Alto de la zona muerta")]
        [Range(0f, 1f)]
        public float deadZoneHeight = 0.1f;

        [Header("Hard Limits (Límites Duros)")]
        [Tooltip("Si está activo, el target no podrá salir de los límites duros en pantalla")]
        public bool useHardLimits = false;

        [Tooltip("Ancho de los límites duros (el target no saldrá de este área en pantalla)")]
        [Range(0f, 1f)]
        public float hardLimitWidth = 0.6f;

        [Tooltip("Alto de los límites duros")]
        [Range(0f, 1f)]
        public float hardLimitHeight = 0.6f;

        [Header("Límites de la Cámara")]
        [Tooltip("Si está activo, la cámara no saldrá de los límites definidos por el collider de la región")]
        public bool confineToRegion = false;

        [Header("Transiciones")]
        [Tooltip("Tiempo de transición al entrar a esta región (en segundos)")]
        [Range(0f, 5f)]
        public float blendTimeIn = 1f;

        [Tooltip("Tiempo de transición al salir de esta región (en segundos)")]
        [Range(0f, 5f)]
        public float blendTimeOut = 1f;

        [Tooltip("Curva de la transición de entrada")]
        public CameraBlendStyle blendStyleIn = CameraBlendStyle.EaseInOut;

        [Tooltip("Curva de la transición de salida")]
        public CameraBlendStyle blendStyleOut = CameraBlendStyle.EaseInOut;

        [Header("Look Ahead (Anticipación)")]
        [Tooltip("Tiempo de anticipación - la cámara mira hacia donde se dirige el jugador")]
        [Range(0f, 2f)]
        public float lookAheadTime = 0f;

        [Tooltip("Suavizado de la anticipación")]
        [Range(0f, 30f)]
        public float lookAheadSmoothing = 10f;

        /// <summary>
        /// Crea una copia de esta configuración
        /// </summary>
        public CameraRegionSettings Clone()
        {
            return (CameraRegionSettings)this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Estilos de blend para las transiciones de cámara
    /// </summary>
    public enum CameraBlendStyle
    {
        Cut,
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        HardIn,
        HardOut
    }
}
