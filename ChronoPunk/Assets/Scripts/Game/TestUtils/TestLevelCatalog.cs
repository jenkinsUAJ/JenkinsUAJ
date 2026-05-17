using System.IO;
using System;
using UnityEngine;

/// <summary>
/// Catálogo compartido para los niveles de test y utilidades de rutas de guardado.
/// Lo consumen TestRecordManager, la ventana de editor y los tests de PlayMode.
/// Esta clase guarda las carpetas donde se van a buscar las escenas a testear y tiene metodos
/// de utilidad para la gestion de rutas
/// </summary>
public static class TestLevelCatalog
{
    public static readonly string[] SceneFolders =
    {
        "Assets/Scenes/FinalNiveles/01_Basico"
    };

    public const string RecordingFolderName = "TestRecordings";

    public static string GetRecordingDirectory()
    {
        return Path.Combine(Application.persistentDataPath, RecordingFolderName);
    }

    public static string GetRecordingFilePath(string sceneName)
    {
        return Path.Combine(GetRecordingDirectory(), sceneName + ".json");
    }

    public static bool IsScenePathInConfiguredFolders(string scenePath)
    {
        string normalizedScenePath = NormalizePath(scenePath);

        for (int i = 0; i < SceneFolders.Length; i++)
        {
            string normalizedFolder = NormalizePath(SceneFolders[i]);
            if (normalizedScenePath.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        string normalized = path.Replace('\\', '/');
        if (!normalized.EndsWith("/", StringComparison.Ordinal))
        {
            normalized += "/";
        }

        return normalized;
    }
}
