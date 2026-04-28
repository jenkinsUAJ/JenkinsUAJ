using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de debug aislado para grabar y proveer rastros de posición.
/// Funciona en paralelo al RecordManager sin interferir con él.
/// Utiliza el patrón de diseño Singleton para ser accesible desde cualquier lugar
/// de la aplicación.
/// </summary>
public class DebugTraceManager : MonoBehaviour
{
    // Propiedad estática para el acceso Singleton.
    public static DebugTraceManager Instance;

    // Diccionario para guardar una lista de posiciones (el "rastro") por cada slot.
    private Dictionary<int, List<Vector2>> allPositionTraces = new Dictionary<int, List<Vector2>>();

    /// <summary>
    /// Se llama cuando se instancia el objeto. Implementa la lógica del Singleton.
    /// </summary>
    void Awake() {
        // Si no hay una instancia, esta se convierte en la única instancia.
        if (Instance == null) {
            Instance = this;
            // Se usa DontDestroyOnLoad para que el manager persista a través de
            // los cambios de escena, lo cual es vital para el sistema de replay.
            DontDestroyOnLoad(gameObject);
        } else {
            // Si ya hay una instancia, destruimos este objeto para mantener la unicidad.
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Prepara un nuevo rastro para un slot, borrando el anterior si existe.
    /// </summary>
    /// <param name="slotId">El ID del slot para el que se inicia la traza.</param>
    public void StartTrace(int slotId) {
        // Si el slot no existe en el diccionario, lo creamos con una nueva lista.
        if (!allPositionTraces.ContainsKey(slotId)) {
            allPositionTraces.Add(slotId, new List<Vector2>());
        } else {
            // Si ya existe, simplemente borramos el contenido de la lista para reutilizarla.
            allPositionTraces[slotId].Clear();
        }
        Debug.Log($"[Debug] Iniciado rastro de posición para el Slot {slotId}.");
    }

    /// <summary>
    /// Añade una nueva posición al rastro del slot especificado.
    /// </summary>
    /// <param name="slotId">El ID del slot.</param>
    /// <param name="position">La posición a grabar.</param>
    public void RecordPosition(int slotId, Vector2 position) {
        // Comprobamos si el slot existe antes de añadir la posición.
        if (allPositionTraces.ContainsKey(slotId)) {
            allPositionTraces[slotId].Add(position);
        }
    }

    /// <summary>
    /// Devuelve el rastro de posiciones grabado para un slot.
    /// </summary>
    /// <param name="slotId">El ID del slot del que se quiere obtener el rastro.</param>
    /// <returns>La lista de posiciones grabadas, o null si el slot no existe.</returns>
    public List<Vector2> GetTrace(int slotId) {
        // Usamos TryGetValue para obtener la lista de forma segura.
        if (allPositionTraces.TryGetValue(slotId, out List<Vector2> trace)) {
            return trace;
        }
        return null;
    }
}