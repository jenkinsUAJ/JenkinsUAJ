using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Clase con métodos estáticos que se encargan de la carga y persistencia de los datos y ajustes.
/// </summary>
public class SaveDataManager : ScriptableObject
{
    /// <summary>
    /// Carga como string json a partir de un nombre de archivo
    /// </summary>
    /// <param name="fileName">El nombre del archivo</param>
    /// <returns></returns>
    public static string Load(string fileName)
    {
        if (File.Exists(Application.persistentDataPath + fileName))
        {
           return File.ReadAllText(Application.persistentDataPath + fileName);
        } else
        {
            return null;
        }
    }
    
    /// <summary>
    /// Guarda un archivo a partir de texto json y el nombre de archivo
    /// </summary>
    /// <typeparam name="T">El tipo de objeto serializable a guardar</typeparam>
    /// <param name="objectToSave">El objeto a guardar</param>
    /// <param name="fileName">El nombre del fichero en el directorio</param>
    public static void Persist<T>(T objectToSave, string fileName)
    {
        string json = JsonUtility.ToJson(objectToSave);

        File.WriteAllText(Application.persistentDataPath + fileName, json);
        Debug.Log("Data/Settings saved to" + saveDataFileName);
    }

    // Datos de guardado

    [Serializable]
    public struct SaveData
    {
        // Los niveles que tiene 
        public int currentUnlockedLevel;
    }
    private static string saveDataFileName = "/saveData.json";
    /// <summary>
    /// Carga los datos de guardado, si no hay los crea y los devuelve
    /// </summary>
    /// <returns>Los datos de guardado cargados. Si no hay los crea</returns>
    public static SaveData LoadSavedData()
    {
        string jsonData = Load(saveDataFileName);
        if (jsonData != null)
        {
           return JsonUtility.FromJson<SaveData>(jsonData);
        } else
        {
            return CreateNewSaveData();
        }
    }

    /// <summary>
    /// Guarda los datos de guardado
    /// </summary>
    /// <param name="saveData">El objeto de datos que guardar</param>
    internal static void PersistSaveData(SaveData saveData)
    {
        Persist(saveData, saveDataFileName);
    }
    
    /// <summary>
    /// Crea un nuevo fichero de datos
    /// </summary>
    /// <returns>El nuevo objeto SaveData con valores predeterminados</returns>
    private static SaveData CreateNewSaveData()
    {
        SaveData data = new SaveData
        {
            currentUnlockedLevel = 0
        };
        return data;
    }


    /// Ajustes de audio

    [Serializable]
    public struct AudioSettings
    {
        public float musicVolume;
        public float sfxVolume;
    } 
    private static string audioSettingsFileName = "/audioSettings.json";

    /// <summary>
    /// Carga los ajustes de audio, si no hay los crea y los devuelve
    /// </summary>
    /// <returns>Los ajustes de audio cargados. Si no hay los crea.</returns>
    public static AudioSettings LoadAudioSettings()
    {
        string jsonData = Load(audioSettingsFileName);
        if (jsonData != null)
        {
           return JsonUtility.FromJson<AudioSettings>(jsonData);
        } else
        {
            return CreateNewAudioSettings();
        }
    }

    /// <summary>
    /// Guarda ajustes de audio
    /// </summary>
    /// <param name="saveData">El AudioSettings a guardar</param>
    internal static void PersistAudioSettings(AudioSettings settings)
    {
        Persist(settings, audioSettingsFileName);
    }

    /// <summary>
    /// Crea un nuevo fichero de ajustes de audio
    /// </summary>
    /// <returns>El nuevo objeto AudioSettings</returns>
    private static AudioSettings CreateNewAudioSettings()
    {
        AudioSettings settings = new AudioSettings()
        {
            musicVolume = 0.6f,
            sfxVolume = 0.7f
        };
        return settings;
    }

}
