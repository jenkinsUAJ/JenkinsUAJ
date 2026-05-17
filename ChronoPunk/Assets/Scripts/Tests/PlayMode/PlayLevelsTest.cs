using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cronopunk.Movement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Test de PlayMode para cargar cada nivel configurado y ejecutarlo con las partidas guardadas.
/// Reutiliza RecordManager y ReplayManager para sombras, y aplica un slot de entrada al Player para validar que completa el nivel.
/// </summary>
public class PlayLevelsTest
{
    private const float MaxPlaybackSecondsPerLevel = 40f;

    [UnityTest]
    public IEnumerator PlayConfiguredLevels_WithSavedRecordings()
    {
        TestRecordManager.EnableReadOnlyModeForTests();

        List<string> scenePaths = GetConfiguredScenePathsFromBuildSettings();
        Assert.IsNotEmpty(scenePaths, "No se han encontrado escenas de test dentro de las carpetas configuradas.");

        try
        {
            for (int i = 0; i < scenePaths.Count; i++)
            {
                string scenePath = scenePaths[i];
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);

                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                while (!loadOperation.isDone)
                {
                    yield return null;
                }

                yield return null;

                Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name, $"No se ha cargado la escena {sceneName}.");

                string recordingFile = TestLevelCatalog.GetRecordingFilePath(sceneName);
                TestRecordingFileData recording = TestRecordManager.LoadRecordingFromFile(recordingFile);
                Assert.IsNotNull(recording, $"No existe fichero de recording para {sceneName} en {recordingFile}.");

                Dictionary<int, List<RecordedInput>> loadedRecordings = recording.ToRecordedInputsBySlot();
                Assert.IsNotEmpty(loadedRecordings, $"El fichero de recording de {sceneName} no contiene slots.");

                int playerDriverSlot = SelectPlayerDriverSlot(loadedRecordings);
                Assert.IsTrue(playerDriverSlot >= 0, $"No se ha podido seleccionar slot driver para {sceneName}.");

                List<RecordedInput> playerInputs = loadedRecordings[playerDriverSlot];
                Assert.IsNotEmpty(playerInputs, $"El slot driver {playerDriverSlot} de {sceneName} está vacío.");

                RecordManager recordManager = RecordManager.Instance;
                ReplayManager replayManager = ReplayManager.Instance;
                Assert.IsNotNull(recordManager, $"No existe RecordManager en la escena {sceneName}.");
                Assert.IsNotNull(replayManager, $"No existe ReplayManager en la escena {sceneName}.");

                PrepareSceneForTestPlayback(loadedRecordings, playerDriverSlot, recordManager, replayManager);

                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Assert.IsNotNull(player, $"No se encuentra Player en {sceneName}.");

                PlayerMovementKinematic movement = player.GetComponent<PlayerMovementKinematic>();
                Shoot shoot = player.GetComponent<Shoot>();
                BalloonUser balloonUser = player.GetComponent<BalloonUser>();
                Solidify solidify = player.GetComponent<Solidify>();
                PlayerPerkController perkController = player.GetComponent<PlayerPerkController>();

                Assert.IsNotNull(movement, $"El Player no tiene PlayerMovementKinematic en {sceneName}.");

                yield return ReplayInputsOnPlayerUntilSceneChanges(
                    sceneName,
                    playerInputs,
                    movement,
                    shoot,
                    balloonUser,
                    solidify,
                    perkController,
                    MaxPlaybackSecondsPerLevel
                );

                Assert.AreNotEqual(
                    sceneName,
                    SceneManager.GetActiveScene().name,
                    $"No se completó el nivel {sceneName} al reproducir la partida guardada."
                );
            }
        }
        finally
        {
            TestRecordManager.DisableReadOnlyModeForTests();
        }
    }

    private static void PrepareSceneForTestPlayback(
        Dictionary<int, List<RecordedInput>> loadedRecordings,
        int playerDriverSlot,
        RecordManager recordManager,
        ReplayManager replayManager)
    {
        if (RecordingSlotManager.Instance != null)
        {
            RecordingSlotManager.Instance.ResetSlots();
        }
        else
        {
            recordManager.allRecordings.Clear();
        }

        foreach (KeyValuePair<int, List<RecordedInput>> entry in loadedRecordings)
        {
            if (entry.Key == playerDriverSlot)
            {
                continue;
            }

            recordManager.allRecordings[entry.Key] = new List<RecordedInput>(entry.Value);
        }

        SelectShadow shadowMenu = Object.FindAnyObjectByType<SelectShadow>();
        if (shadowMenu != null)
        {
            if (shadowMenu.textoEligeTuRama != null)
            {
                shadowMenu.textoEligeTuRama.SetActive(false);
            }

            shadowMenu.gameObject.SetActive(false);
        }

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.SetPause(false);
        }

        replayManager.StartFullReplay();
    }

    private static int SelectPlayerDriverSlot(Dictionary<int, List<RecordedInput>> recordings)
    {
        int selectedSlot = -1;
        int farthestFrame = -1;

        foreach (KeyValuePair<int, List<RecordedInput>> entry in recordings)
        {
            if (entry.Value == null || entry.Value.Count == 0)
            {
                continue;
            }

            int lastFrame = entry.Value[entry.Value.Count - 1].fixedFrameStamp;
            if (lastFrame > farthestFrame)
            {
                farthestFrame = lastFrame;
                selectedSlot = entry.Key;
            }
        }

        return selectedSlot;
    }

    private static IEnumerator ReplayInputsOnPlayerUntilSceneChanges(
        string sourceSceneName,
        List<RecordedInput> playerInputs,
        PlayerMovementKinematic movement,
        Shoot shoot,
        BalloonUser balloonUser,
        Solidify solidify,
        PlayerPerkController perkController,
        float timeoutSeconds)
    {
        int frameCounter = 0;
        int inputIndex = 0;
        float elapsed = 0f;

        while (elapsed < timeoutSeconds && SceneManager.GetActiveScene().name == sourceSceneName)
        {
            yield return new WaitForFixedUpdate();

            elapsed += Time.fixedDeltaTime;
            frameCounter++;

            while (inputIndex < playerInputs.Count && frameCounter >= playerInputs[inputIndex].fixedFrameStamp)
            {
                ExecutePlayerInput(playerInputs[inputIndex], movement, shoot, balloonUser, solidify, perkController);
                inputIndex++;
            }
        }
    }

    private static void ExecutePlayerInput(
        RecordedInput input,
        PlayerMovementKinematic movement,
        Shoot shoot,
        BalloonUser balloonUser,
        Solidify solidify,
        PlayerPerkController perkController)
    {
        if (input is MoveInput moveInput)
        {
            if (balloonUser != null && balloonUser.IsOnBalloon)
            {
                balloonUser.CurrentBalloon.SetHorizontalInput(moveInput.direction.x);
            }
            else
            {
                movement.SetHorizontalMove(moveInput.direction.x);
            }

            if (shoot != null)
            {
                Vector2 aimDirection = Shoot.QuantizeToEightDirections(moveInput.direction);
                shoot.SetAim(aimDirection);
            }
        }
        else if (input is JumpInput jumpInput)
        {
            if (balloonUser != null && balloonUser.IsOnBalloon)
            {
                if (jumpInput.isPressed)
                {
                    balloonUser.EjectFromBalloon(true);
                }
            }
            else
            {
                if (jumpInput.isPressed)
                {
                    movement.TryJump();
                }
                else
                {
                    movement.ApplyJumpCut();
                }
            }
        }
        else if (input is ShootInput)
        {
            if (shoot != null)
            {
                shoot.TryShoot();
            }
        }
        else if (input is StopRecordingInput)
        {
            movement.ForceStopHorizontalImmediately();

            if (solidify != null)
            {
                solidify.ActivateSolidification();
            }

            if (perkController != null)
            {
                perkController.UsePerk();
            }
        }
    }

    private static List<string> GetConfiguredScenePathsFromBuildSettings()
    {
        List<string> configuredScenes = new List<string>();

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (TestLevelCatalog.IsScenePathInConfiguredFolders(scenePath))
            {
                configuredScenes.Add(scenePath);
            }
        }

        return configuredScenes;
    }
}