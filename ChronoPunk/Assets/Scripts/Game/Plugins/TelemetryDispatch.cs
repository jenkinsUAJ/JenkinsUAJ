using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fachada de telemetría para gameplay.
/// Unifica el envío de eventos al EventManager y evita duplicar lógica de contexto en cada callsite.
/// </summary>
namespace Telemetry
{
    public static class TelemetryDispatch
    {

        public static void SendLevelStart()
        {
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendLevelStartEvent(levelId);
        }

        public static void SendLevelEnd()
        {
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendLevelEndEvent(levelId);
        }

        public static void SendDeath(Vector2 position)
        {
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendDeathEvent(
                levelId,
                position.x,
                position.y
            );
        }

        public static void SendEndIteration(Vector2 position, int shadowId)
        {
            if (shadowId < 0) return;
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendEndIterationEvent(
                levelId,
                position.x,
                position.y,
                shadowId
            );
        }

        public static void SendDetFailure(Vector2 position, int shadowId)
        {
            if (shadowId < 0) return;
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendDetFailureEvent(
                levelId,
                position.x,
                position.y,
                shadowId
            );
        }

        public static void SendLeftGame()
        {
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendLeftGameEvent(levelId);
        }

        public static void SendLeftLevel()
        {
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendLeftLevelEvent(levelId);
        }

        public static void SendShadowSelect(int shadowId)
        {
            if (shadowId < 0) return;
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendShadowSelectEvent(levelId, shadowId);
        }

        public static void SendButtonPress(int buttonId)
        {
            if (buttonId < 0) return;
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendButtonPressEvent(levelId, buttonId);
        }

        public static void SendLeverAction(int leverId)
        {
            if (leverId < 0) return;
            if (!TryGetContext(out EventManager manager, out int levelId)) return;

            manager.sendLeverActionEvent(levelId, leverId);
        }

        public static Vector2 ResolvePosition(Transform source)
        {
            if (source == null) return Vector2.zero;

            Vector3 position = source.position;
            return new Vector2(position.x, position.y);
        }

        // Resuelve el contexto mínimo necesario para enviar telemetría.
        private static bool TryGetContext(out EventManager manager, out int levelId)
        {
            manager = UnityEngine.Object.FindAnyObjectByType<EventManager>();

            if (manager == null)
            {
                GameObject eventManagerObject = new GameObject("EventManager_Auto");
                manager = eventManagerObject.AddComponent<EventManager>();
            }

            if (manager == null)
            {
                levelId = -1;
                return false;
            }

            if (SceneManager.GetActiveScene().buildIndex < 1)
            {
                levelId = PlayerPrefs.GetInt("LAST_LEVEL");
            }
            else
            {
                levelId = SceneManager.GetActiveScene().buildIndex;
            }
            
            return true;
        }
    }

}
