using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SceneTests
{
    [UnityTest]
    public IEnumerator SceneTestsLevelNavigation()
    {
        yield return CleanupPersistentRuntimeState();


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

            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.SetPause(false);
            }

            Scene loadedScene = SceneManager.GetActiveScene();

            GameObject player = FindTaggedObjectInScene(loadedScene, "Player");

            GameObject goal = FindTaggedObjectInScene(loadedScene, "Finish");

            Assert.IsNotNull(player, $"No se encuentra jugador en el nivel {currentLevel}");
            Assert.IsNotNull(goal, $"No se encuentra meta en el nivel {currentLevel}");

            // Mover player
            player.transform.position =
                goal.transform.position;

            // Esperar de forma determinista a que cambie de escena.
            float elapsed = 0f;
            const float timeoutSeconds = 5f;

            while (elapsed < timeoutSeconds && SceneManager.GetActiveScene().name == currentLevel)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Assert.IsTrue(SceneManager.GetActiveScene().name == nextLevel,
                $"No se ha conseguido navegar desde {currentLevel} hasta {nextLevel}, solo se ha llegado a {SceneManager.GetActiveScene().name}");
        }

        yield return null;
    }

    private static IEnumerator CleanupPersistentRuntimeState()
    {
        TestRecordManager.ResetForTests();

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.SetPause(false);
        }

        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.ClearExistingReplicants();
            Object.Destroy(ReplayManager.Instance.gameObject);
        }

        if (RecordManager.Instance != null)
        {
            Object.Destroy(RecordManager.Instance.gameObject);
        }

        if (RecordingSlotManager.Instance != null)
        {
            Object.Destroy(RecordingSlotManager.Instance.gameObject);
        }

        if (GameFlow.LevelManager.Instance != null)
        {
            //Object.Destroy(GameFlow.LevelManager.Instance.gameObject);
        }

        if (GameManager.Instance != null)
        {
            Object.Destroy(GameManager.Instance.gameObject);
        }

        if (TransitionManager.Instance != null)
        {
            Object.Destroy(TransitionManager.Instance.gameObject);
        }

        // Dejar al menos un frame para que Unity complete destrucciones pendientes.
        yield return null;
        yield return null;
    }

    private static GameObject FindTaggedObjectInScene(Scene scene, string tag)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        Queue<Transform> queue = new Queue<Transform>();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            queue.Enqueue(rootObjects[i].transform);
        }

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            if (current.CompareTag(tag))
            {
                return current.gameObject;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }

        return null;
    }
}
