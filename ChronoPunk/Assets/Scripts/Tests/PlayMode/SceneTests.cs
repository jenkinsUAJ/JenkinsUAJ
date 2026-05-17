using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SceneTests
{
    [UnityTest]
    public IEnumerator SceneTestsLevelNavigation()
    {
        TestRecordManager.ResetForTests();

        yield return null;


        LevelsData levelData =
            Resources.Load<LevelsData>("Build playtesting");

        Assert.IsNotNull(levelData, $"No se encuentra una definicion de LevelsData en la carpeta Resources");

        for (int i = 0; i < levelData.levels.Length - 1; ++i)
        {
            string currentLevel = levelData.levels[i].name;

            string nextLevel = levelData.levels[i + 1].name;

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(currentLevel);

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;


            GameObject player = GameObject.FindGameObjectWithTag("Player");

            GameObject goal = GameObject.FindGameObjectWithTag("Finish");

            Assert.IsNotNull(player, $"No se encuentra jugador en el nivel {currentLevel}");
            Assert.IsNotNull(goal, $"No se encuentra meta en el nivel {currentLevel}");

            // Mover player
            player.transform.position =
                goal.transform.position;

            // Esperar cambio de escena
            yield return new WaitForSeconds(1f);

            Assert.IsTrue(SceneManager.GetActiveScene().name == nextLevel,
                $"No se ha conseguido navegar desde {currentLevel} hasta {nextLevel}, solo se ha llegado a {SceneManager.GetActiveScene().name}");
        }

        yield return null;
    }
}
