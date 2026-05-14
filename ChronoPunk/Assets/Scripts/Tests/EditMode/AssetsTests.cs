using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class AssetsTests
{

    /// <summary>
    /// Test de asset enfocado en revisar el correcto formato en los asset de arte del proyecto
    /// </summary>
    [Test]
    public void AssetsTestsArtFormat()
    {
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

            string fileName = Path.GetFileNameWithoutExtension(path);

            string correctFormat = System.IO.Path.ChangeExtension(path, null);

            correctFormat = correctFormat.Replace("Assets/Art/Nuevo/CurrentArt/", "");
            correctFormat = correctFormat.Replace(fileName, "");
            correctFormat = correctFormat.Replace("/", "_");
            correctFormat = correctFormat.ToLower();

            Debug.Log($"Comprobando: {fileName} que coincida con {correctFormat} en path {path}");

            Assert.IsTrue(
                        fileName.StartsWith(correctFormat),
                        $"{fileName} en la ruta {path} debería empezar por {correctFormat}"
            );

        }

    }

}
