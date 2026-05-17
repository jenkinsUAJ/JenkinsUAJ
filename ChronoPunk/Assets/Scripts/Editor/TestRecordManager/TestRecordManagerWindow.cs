#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ventana de editor para lanzar niveles de test y revisar las grabaciones ya guardadas.
/// Se apoya en TestLevelCatalog para listar escenas y en TestRecordManager para activar el modo de captura.
/// </summary>
public class TestRecordManagerWindow : EditorWindow
{
    private readonly List<string> _scenePaths = new List<string>();
    private Vector2 _savedFilesScroll;
    private int _selectedSceneIndex;

    [MenuItem("Tools/TestRecordManager")]
    public static void OpenRoot()
    {
        Open();
    }

    public static void Open()
    {
        GetWindow<TestRecordManagerWindow>("TestRecordManager");
    }

    private void OnEnable()
    {
        RefreshScenes();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Play recorded levels", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUILayout.HelpBox(
            "La lista de niveles se construye desde las carpetas definidas en TestLevelCatalog.SceneFolders.",
            MessageType.Info
        );

        DrawSavedFilesSection();

        if (_scenePaths.Count == 0)
        {
            EditorGUILayout.HelpBox("No se han encontrado escenas en las carpetas configuradas.", MessageType.Warning);
        }
        else
        {
            _selectedSceneIndex = Mathf.Clamp(_selectedSceneIndex, 0, _scenePaths.Count - 1);
            string[] labels = BuildSceneLabels();
            _selectedSceneIndex = EditorGUILayout.Popup("Nivel", _selectedSceneIndex, labels);

            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Jugar nivel"))
            {
                PlaySelectedLevel();
            }
        }

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Actualizar lista"))
        {
            RefreshScenes();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Carpetas configuradas", EditorStyles.boldLabel);

        for (int i = 0; i < TestLevelCatalog.SceneFolders.Length; i++)
        {
            EditorGUILayout.LabelField(TestLevelCatalog.SceneFolders[i]);
        }
    }

    private void DrawSavedFilesSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Grabaciones guardadas", EditorStyles.boldLabel);

        string directory = TestLevelCatalog.GetRecordingDirectory();
        if (!Directory.Exists(directory))
        {
            EditorGUILayout.LabelField("No existe aún el directorio de grabaciones.");
            return;
        }

        string[] files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            EditorGUILayout.LabelField("No hay archivos guardados.");
            return;
        }

        _savedFilesScroll = EditorGUILayout.BeginScrollView(_savedFilesScroll, GUILayout.MinHeight(120f));

        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileName(files[i]);
            string levelName = Path.GetFileNameWithoutExtension(files[i]);
            TestRecordingFileData recording = TestRecordManager.LoadRecordingFromFile(files[i]);
            int slotCount = recording != null && recording.slots != null ? recording.slots.Count : 0;
            int inputCount = 0;

            if (recording != null && recording.slots != null)
            {
                for (int slotIndex = 0; slotIndex < recording.slots.Count; slotIndex++)
                {
                    TestRecordingSlotData slotData = recording.slots[slotIndex];
                    if (slotData?.inputs != null)
                    {
                        inputCount += slotData.inputs.Count;
                    }
                }
            }

            bool matchesConfiguredLevel = _scenePaths.Contains(GetScenePathForLevelName(levelName));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(fileName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Nivel: {levelName}");
            EditorGUILayout.LabelField($"Estado: {(matchesConfiguredLevel ? "nivel configurado" : "archivo suelto")}");
            EditorGUILayout.LabelField($"Slots grabados: {slotCount}");
            EditorGUILayout.LabelField($"Inputs grabados: {inputCount}");
            EditorGUILayout.LabelField(files[i]);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    private string GetScenePathForLevelName(string levelName)
    {
        for (int i = 0; i < _scenePaths.Count; i++)
        {
            string sceneName = Path.GetFileNameWithoutExtension(_scenePaths[i]);
            if (string.Equals(sceneName, levelName, System.StringComparison.OrdinalIgnoreCase))
            {
                return _scenePaths[i];
            }
        }

        return string.Empty;
    }

    private void RefreshScenes()
    {
        _scenePaths.Clear();

        HashSet<string> uniquePaths = new HashSet<string>();

        for (int i = 0; i < TestLevelCatalog.SceneFolders.Length; i++)
        {
            string folder = TestLevelCatalog.SceneFolders[i];
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { folder });

            for (int j = 0; j < guids.Length; j++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guids[j]);
                if (string.IsNullOrEmpty(scenePath) || !scenePath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!uniquePaths.Add(scenePath))
                {
                    continue;
                }

                _scenePaths.Add(scenePath);
            }
        }

        _scenePaths.Sort();
        _selectedSceneIndex = Mathf.Clamp(_selectedSceneIndex, 0, Mathf.Max(0, _scenePaths.Count - 1));
    }

    private string[] BuildSceneLabels()
    {
        string[] labels = new string[_scenePaths.Count];

        for (int i = 0; i < _scenePaths.Count; i++)
        {
            labels[i] = Path.GetFileNameWithoutExtension(_scenePaths[i]);
        }

        return labels;
    }

    private void PlaySelectedLevel()
    {
        if (_scenePaths.Count == 0)
        {
            return;
        }

        string scenePath = _scenePaths[_selectedSceneIndex];
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);

        EditorPrefs.SetBool(TestRecordManager.EditorPrefsEnabledKey, true);

        if (EditorApplication.isPlaying)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            return;
        }

        EditorSceneManager.OpenScene(scenePath);
        EditorApplication.EnterPlaymode();
    }
}
#endif