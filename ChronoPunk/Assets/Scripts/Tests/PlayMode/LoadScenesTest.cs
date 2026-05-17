using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using System.Collections.Generic;

public class LoadScenesTest
{

    [UnityTest, Category("Fast")]
    public IEnumerator AllScenes_Load_Correctly()
    {
        List<LogType> logs = new List<LogType>();

        Application.LogCallback handler =
          (condition, stackTrace, type) =>
          {
              logs.Add(type);
          };

        Application.logMessageReceived += handler;
        try
        {
            int sceneCount =
                SceneManager.sceneCountInBuildSettings;

            for (int i = 0; i < sceneCount; i++)
            {

                logs.Clear();

                string scenePath =
                    SceneUtility.GetScenePathByBuildIndex(i);

                string sceneName =
                    System.IO.Path.GetFileNameWithoutExtension(
                        scenePath
                    );

                Debug.Log("Cheking loading scene : " +sceneName);

                AsyncOperation operation =
                    SceneManager.LoadSceneAsync(sceneName);

                while (!operation.isDone)
                {
                    yield return null;
                }

                //esperamos un par de frames para que se actualize el active scene
                yield return null;
                yield return null;

                Scene activeScene =
                    SceneManager.GetActiveScene();

                Assert.AreEqual(
                    sceneName,
                    activeScene.name
                );

                Assert.IsTrue(
                    activeScene.isLoaded
                );



                //detectamos solo logs de errores
                Assert.IsFalse(logs.Contains(LogType.Error));

                //no lo usamos ya que detecta tambien warnings
                //LogAssert.NoUnexpectedReceived();

                Debug.Log("Scene " + sceneName + " loaded correctly");

                yield return null;
            }
        }
        finally
        {
            Application.logMessageReceived -= handler;

            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.SetPause(false);
            }

            if (ReplayManager.Instance != null)
            {
                ReplayManager.Instance.ClearExistingReplicants();
            }

            TestRecordManager.ResetForTests();
        }
        
        yield return null;

    }




}
