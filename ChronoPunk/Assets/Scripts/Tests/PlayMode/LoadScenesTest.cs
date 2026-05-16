using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;

public class LoadScenesTest
{

    [UnityTest]
    public IEnumerator AllScenes_Load_Correctly()
    {

        int sceneCount =
            SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath =
                SceneUtility.GetScenePathByBuildIndex(i);

            string sceneName =
                System.IO.Path.GetFileNameWithoutExtension(
                    scenePath
                );

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(i);

            while (!operation.isDone)
            {
                yield return null;
            }

            Scene activeScene =
                SceneManager.GetActiveScene();

            Assert.AreEqual(
                sceneName,
                activeScene.name
            );

            Assert.IsTrue(
                activeScene.isLoaded
            );

            yield return null;
        }
    }

  

}
