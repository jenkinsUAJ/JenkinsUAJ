using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.WSA;

public class AssetsTests
{

    /// <summary>
    /// Test de asset enfocado en revisar el correcto formato en los asset de arte del proyecto
    /// </summary>
    [Test]
    public void AssetsTestsArtFormat()
    {
        bool validTest = true;

        //Establecemos la carpeta raiz la cual se va a revisar
        string rootFolder = "Assets/Art/Nuevo/CurrentArt";

        //Sobre esa carpeta sacamos todos y cada uno de los assets en ella
        string[] guids = AssetDatabase.FindAssets("", new[] { rootFolder });

        foreach (string guid in guids)
        {
            //Sacamos el path del elemento para usarlo como referncia de nombre que debe tener
            string path = AssetDatabase.GUIDToAssetPath(guid);

            //En caso de ser una carpeta se ignora
            if (AssetDatabase.IsValidFolder(path))
                continue;

            //Obtenemos el nombre del archivo sin extension
            string fileName = Path.GetFileNameWithoutExtension(path);

            //Sacamos el path sin extension
            string correctFormat = System.IO.Path.ChangeExtension(path, null);

            //Quitamos la ruta de la raiz
            correctFormat = correctFormat.Replace("Assets/Art/Nuevo/CurrentArt/", "");
            //Quitamos el nombre del archivo
            correctFormat = correctFormat.Replace(fileName, "");
            //Cambiamos las barras del path por los guiones bajos que debe seguir el formato
            correctFormat = correctFormat.Replace("/", "_");
            //Ponemos todo en minusculas
            correctFormat = correctFormat.ToLower();

            //Si el archivo no comienza por el nombre de la ruta desde la raiz con el formato especificado no es valido
            if (!fileName.StartsWith(correctFormat))
            {
                Debug.Log($"El archivo {fileName}, con ruta {path}, no coincide con el formato correcto de {correctFormat}");
                validTest = false;
            }

        }

        Assert.IsTrue(validTest);

    }

    /// <summary>
    /// Test de asset enfocado en revisar el correcto formato en las escenas de juego del proyecto
    /// </summary>
    [Test]
    public void AssetsTestsSceneFormat()
    {
        //bool que marca si el test a sido correcto o no, se crea para poder almacenar todos los incorrectos en un log
        bool validTest = true;

        //Establecemos la carpeta raiz la cual se va a revisar
        string rootFolder = "Assets/Scenes/FinalNiveles";

        //Sobre esa carpeta sacamos todos y cada uno de los assets en ella
        string[] guids = AssetDatabase.FindAssets(
            "t:Scene",
            new[] { rootFolder }
        );

        foreach (string guid in guids)
        {
            //Sacamos el path del elemento para usarlo como referncia de nombre que debe tener
            string path = AssetDatabase.GUIDToAssetPath(guid);

            //Se saca el nombre del archivo sin la extension
            string fileName = Path.GetFileNameWithoutExtension(path);

            //Sacamos el nombre de la carpeta en la que se encuentra
            string correctFormat = new DirectoryInfo(Path.GetDirectoryName(path)).Name;

            //Obtenemos el tipo de nivel (Basico, Roca, Globo...)
            correctFormat = correctFormat.Split('_')[1];
            correctFormat = correctFormat + "_";

            //Si los archivos de escenas no empiezan por el tipo de nivel esta mal el formato
            if (!fileName.StartsWith(correctFormat))
            {
                Debug.Log($"El archivo {fileName}, con ruta {path}, no coincide con el formato correcto de {correctFormat}");
                validTest = false;
            }

        }

        Assert.IsTrue(validTest);

    }

    /// <summary>
    /// Test de asset enfocado en revisar el correcto formato en las escenas de juego del proyecto
    /// </summary>
    [Test]
    public void AssetsTestsSceneNumeration()
    {
        bool validTest = true;

        //Establecemos la carpeta raiz la cual se va a revisar
        string rootFolder = "Assets/Scenes/FinalNiveles";

        //Sobre esa carpeta sacamos todos y cada uno de los assets en ella
        string[] guids = AssetDatabase.FindAssets("", new[] { rootFolder });

        foreach (string guid in guids)
        {
            //Sacamos el path del elemento para usarlo como referncia de nombre que debe tener
            string path = AssetDatabase.GUIDToAssetPath(guid);

            //Solo nos metemos en las carpetas
            if (AssetDatabase.IsValidFolder(path))
            {
                //Sacamos los archivos de tipo escena
                string[] sceneGuids = AssetDatabase.FindAssets(
                    "t:Scene",
                    new[] { path }
                );

                //Obtenemos la ruta de cada uno de ellos
                string[] scenePaths = sceneGuids
                    .Select(g => AssetDatabase.GUIDToAssetPath(g))
                    .ToArray();

                //Ordenamos las rutas por nombres
                System.Array.Sort(scenePaths);

                //Obtenemos el numero de escenas por cada tipo
                int levelCount = scenePaths.Length;

                //Bucle que comprueba que la enumeracion de los distintos tipos de escena vaya de 1 hasta length
                for(int i = 0; i < levelCount; ++i)
                {
                    string levelNumber = Path.GetFileNameWithoutExtension(scenePaths[i]);

                    levelNumber = levelNumber.Split('_')[1];

                    int num;

                    bool isNumber = int.TryParse(levelNumber, out num);

                    if(!isNumber || num != i + 1)
                    {
                        Debug.Log($"La escena {Path.GetFileNameWithoutExtension(scenePaths[i])} tiene una enumeracion incorrecta");
                        validTest = false;
                    }

                }

            }      

        }

        Assert.IsTrue(validTest);

    }

}
