using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Kinematic.Runtime
{
    internal static class KinematicPlayerLoop
    {
        private struct KinematicWorldStep
        {
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            RemoveExistingSystem(ref playerLoop);

            var worldStepSystem = new PlayerLoopSystem
            {
                type = typeof(KinematicWorldStep),
                updateDelegate = KinematicWorld.Tick
            };

            if (!InsertAfterUpdate(ref playerLoop, worldStepSystem))
            {
                Debug.LogError("Failed to insert Kinematic PlayerLoop.");
                return;
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        private static bool InsertAfterUpdate(ref PlayerLoopSystem playerLoop, PlayerLoopSystem systemToInsert)
        {
            var systems = playerLoop.subSystemList;

            if (systems == null)
            {
                return false;
            }

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i].type != typeof(UnityEngine.PlayerLoop.Update))
                {
                    continue;
                }

                var newSystems = new List<PlayerLoopSystem>(systems);
                // i는 Update 이므로 i + 1은 업데이트 루프 이후에 Kinematic Update를 삽입한다.
                // 즉 Kinematic Upate는 mono update 직후 실행된다.
                newSystems.Insert(i + 1, systemToInsert);
                playerLoop.subSystemList = newSystems.ToArray();

                return true;
            }

            return false;
        }

        private static void RemoveExistingSystem(ref PlayerLoopSystem playerLoop)
        {
            var systems = playerLoop.subSystemList;

            if (systems == null)
            {
                return;
            }

            var existingIndex = -1;

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i].type == typeof(KinematicWorldStep))
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex < 0)
            {
                return;
            }

            var newSystems = new PlayerLoopSystem[systems.Length - 1];

            Array.Copy(systems, 0, newSystems, 0, existingIndex);
            Array.Copy(systems, existingIndex + 1, newSystems, existingIndex, systems.Length - existingIndex - 1);

            playerLoop.subSystemList = newSystems;
        }
    }
}