#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
#endif

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

namespace GameFlow
{
    /// <summary>
    /// Gestor del paso entre niveles.
    /// Singleton. Se autogestiona para asegurarse de que sólo existe una instancia.
    /// Debe crearse en la primera escena de juego. Automáticamente persistirá al resto.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Tooltip("Los niveles que se van a utilizar")]
        [FormerlySerializedAs("levelsData")]
        public LevelsData levelsDataSerial;
        private LevelsData levelsData;
        private string mainMenuSceneName = "MainMenu";
        private string levelSelectionSceneName = "LevelSelection";
#if UNITY_EDITOR
        [Tooltip("La escena mainMenu")]
        public SceneAsset mainSceneAsset;
        [Tooltip("La escena levelSelection")]
        public SceneAsset levelSelectionSceneAsset;
        [Tooltip("Activar para que NO se apliquen los datos guardados para progresión sino lo seleccionado en los data")]
        [SerializeField] public bool debugMode;
#endif
        private int _currentLevel = 0;
        // El nivel desbloqueado actual
        public int _currentUnlockedLevel;
        public static LevelManager Instance { get; private set; }

        // ejecute solo una vez la primera cancion
        private bool _playStartSong = true;

        //El awake asegura que la instancia es única y que no se el objeto no se destruye al cambiar de escena
        private void Awake()
        {
            if (Instance == null)
            {
                // Crea una nueva instancia de los datos de niveles (para que pueda ser modificada sin problemas)
                levelsData = Instantiate(levelsDataSerial);
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!debugMode)
            {
#endif
                ApplySavedData();
#if UNITY_EDITOR
            }
#endif
            // Detectar en qué nivel estamos actualmente
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            if (_playStartSong && AudioManager.Instance != null)
            {
                _playStartSong = false;
            }

            //descomentando esta linea no pasa el test de carga de escenas
            //Debug.LogError("error prueba tests");
        }

        private void OnDestroy()
        {
            // Desuscribirse del evento para evitar memory leaks
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Actualizar el nivel actual cada vez que se carga una escena
            UpdateCurrentLevelFromScene();

            if (IsGameplayScene(scene.name))
            {
                Telemetry.TelemetryDispatch.SendLevelStart();
            }
        }

        /// <summary>
        /// Carga y aplica los datos de guardado sobre los niveles.
        /// </summary>
        private void ApplySavedData()
        {
            int l = SaveDataManager.LoadSavedData().currentUnlockedLevel;
            for (int i = 0; i < l; i++)
            {
                levelsData.levels[i].state = Level.LevelProgressState.COMPLETED;
            }
            if (l < levelsData.levels.Length)
                levelsData.levels[l].state = Level.LevelProgressState.UNLOCKED;
            _currentUnlockedLevel = l;
        }

        /// <summary>
        /// Actualiza _currentLevel basándose en la escena activa actual.
        /// Esto permite que el juego funcione correctamente independientemente del nivel inicial.
        /// </summary>
        private void UpdateCurrentLevelFromScene()
        {
            if (levelsData == null || levelsData.levels == null) return;

            string currentSceneName = SceneManager.GetActiveScene().name;

            // Buscar el índice del nivel actual
            for (int i = 0; i < levelsData.levels.Length; i++)
            {
                if (levelsData.levels[i].sceneName == currentSceneName)
                {
                    _currentLevel = i;
                    Debug.Log($"LevelManager: Nivel actual detectado: {_currentLevel} ({currentSceneName})");
                    return;
                }
            }

            // Si no se encuentra, podría ser el menú principal o selección de niveles
            Debug.Log($"LevelManager: Escena actual '{currentSceneName}' no es un nivel de juego.");
        }

        private bool IsGameplayScene(string sceneName)
        {
            if (levelsData == null || levelsData.levels == null)
            {
                return false;
            }

            for (int i = 0; i < levelsData.levels.Length; i++)
            {
                if (levelsData.levels[i].sceneName == sceneName)
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mainSceneAsset != null)
            {
                //mainMenuSceneName = mainSceneAsset.name;
            }
            else
            {
                Debug.LogWarning("No ha sido asignada una escena de menú principal");
            }


            if (levelSelectionSceneAsset != null)
            {
                //levelSelectionSceneName = levelSelectionSceneAsset.name;
            }
            else
            {
                Debug.LogWarning("No ha sido asignada una escena de selección de niveles");
            }


            //lo quitamos de la cola para evitar acumulacion de llamadas a la funcion (se podria hacer con un bool)
            UnityEditor.EditorApplication.delayCall -= DelayedValidate;
                
            //lo metemos a la cola una unica vez
            UnityEditor.EditorApplication.delayCall += DelayedValidate;
          
        }

        /** OnValidate puede ser llamado antes de que se terminen de settear correctamente referencias del editor, por 
         * lo que para cierto tipo de comprobaciones como verificar si una referencia esta asignada, es mas seguro usar una 
         * validacion en el siguiente tick del editor
         * 
         * IMPORTANTE: esto solo arregla warnigns de editor no de tests
         * 
         */
        private void DelayedValidate()
        {
            if (levelsDataSerial == null)
            {
                Debug.LogWarning("Necesita una LevelsData.");
            }
        }


#endif
        private void LoadLevel()
        {
            //TODO: Lanza una loading Scene
            SceneManager.LoadScene(levelsData.levels[_currentLevel].sceneName);
        }

        public void RestartLevel()
        {
            RecordingSlotManager.Instance.ResetSlots();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Cambia al siguiente nivel y lo marca como desbloqueado.
        /// </summary>
        public void NextLevel()
        {
            RecordingSlotManager.Instance.ResetSlots();
            if (_currentLevel < levelsData.levels.Length - 1)
            {
                if (_currentUnlockedLevel == _currentLevel)
                {
                    //el nivel actual lo ponemos a completado
                    levelsData.levels[_currentLevel].state = Level.LevelProgressState.COMPLETED;
                    SaveDataManager.SaveData saveData = new SaveDataManager.SaveData();
                    _currentUnlockedLevel++;
                    saveData.currentUnlockedLevel = _currentUnlockedLevel;
                    SaveDataManager.PersistSaveData(saveData);
                    //el siguiente nivel lo ponemos a desbloqueado
                    levelsData.levels[_currentUnlockedLevel].state = Level.LevelProgressState.UNLOCKED;
                }
                _currentLevel++;

                LoadLevel();
            }
            else
            {
                _currentUnlockedLevel++;
                SaveDataManager.SaveData saveData = new SaveDataManager.SaveData();
                SaveDataManager.PersistSaveData(saveData);
                levelsData.levels[_currentLevel].state = Level.LevelProgressState.COMPLETED;
                LoadMainMenu();
            }
        }

        /// <summary>
        /// Carga el nivel.
        /// </summary>
        /// <param name="lvl">El número del nivel en el array</param>
        public void SelectLevel(int lvl)
        {
            _currentLevel = lvl;
            LoadLevel();
        }

        public void ExitGame()
        {
            Telemetry.TelemetryDispatch.SendLeftGame();
            Application.Quit();
        }

        public void LoadMainMenu()
        {
            if (RecordingSlotManager.Instance != null)
            {
                RecordingSlotManager.Instance.ResetSlots();
            }
            SceneManager.LoadScene(mainMenuSceneName);

            AudioManager.Instance.StopSong();
        }

        public void LoadLevelSelection()
        {
            SceneManager.LoadScene(levelSelectionSceneName);
        }

        public LevelsData GetLevelsData()
        {
            return levelsData;
        }

        public int GetCurrentLevel()
        {
            return _currentLevel;
        }
    }

    /**
    ** Código antiguo de la jam. Sistema de fade in-out
    **/

    ///Debemos rehacer el sistema de fade-in fade-out

    /// <summary>
    /// Maneja el fade in.
    /// </summary>
    /// <returns></returns>
    /**
    private IEnumerator FadeIn(Color color)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0.1f, 0, elapsedTime / fadeInDuration); // Reduce alpha
            fadeInImageColored.color = color;
            color.a = Mathf.Lerp(1, 0, elapsedTime / fadeInDurationFondo); // Reduce alpha
            fadeInImageFondo.color = color;
            yield return null;
        }
        if (CameraFollow.Instance is not null)
        {
            CameraFollow.Instance.destroyGoalTransform();
        }
    }


    /// <summary>
    /// Procesa el fadeout y una vez termina, lanza la nueva escena.
    /// </summary>
    /// <returns></returns>
    private IEnumerator FadeOut(Color color)
    {
        float elapsedTime = 0f;

        float lDuration = fadeOutDuration;
        float lDurationFondo = fadeOutDurationFondo;
        if (state != FState.Won)
        {
            lDuration /= 5;
            lDurationFondo /= 5;
        }
        while (elapsedTime < lDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0, 0.1f, elapsedTime / lDuration); // Increase alpha
            fadeInImageColored.color = color;
            color.a = Mathf.Lerp(0, 1f, elapsedTime / lDurationFondo); // Increase alpha
            fadeInImageFondo.color = color;
            yield return null;
        }
        //Cutrísimo esto. Lo siento, es una jam.
        if (state == FState.Won)
        {
            NextLevel();
        }
        else
        {
            SceneManager.UnloadSceneAsync(levels[_currentLevel]);
            LoadLevel(_currentLevel);
        }
    }



        /// Esto es un desastre.
        void updateCurrentLevel()
        {
            string name = SceneManager.GetActiveScene().name;

            for (int i = 0; i < levels.Length; i++)
            {

                if (levels[i] == name)
                {
                    _currentLevel = i;
                    break;
                }
            }
        }

    **/
}
