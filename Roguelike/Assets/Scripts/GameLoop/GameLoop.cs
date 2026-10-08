using System;
using System.Collections.Generic;
using EventSystem;
using Kinematic.Runtime;
using UnityEngine;
using UnityEngine.LowLevel;
using Pawn.Runtime;

namespace GameLoop
{
    internal static class GameLoop
    {
        private struct MessageSystemUpdate { }
        private struct PawnMovementResolveUpdate { }
        private struct KinematicWorldUpdate { }

        private readonly struct UpdateStep
        {
            public Type Type { get; }
            public PlayerLoopSystem.UpdateFunction Callback { get; }

            public UpdateStep(Type type, PlayerLoopSystem.UpdateFunction callback)
            {
                Type = type;
                Callback = callback;
            }
        }

        private static UpdateStep[] DefineUpdateOrder()
        {
            return new UpdateStep[]
            {
                new(typeof(MessageSystemUpdate), MessageSystem.Instance.Update),
                new(typeof(PawnMovementResolveUpdate), PawnExtensions.ConsumeMoveRequests),
                new(typeof(KinematicWorldUpdate), KinematicWorld.Tick)
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // 현재 PlayerLoop 조회
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            // 업데이트 순서 정의
            var updateOrder = DefineUpdateOrder();
            // 이전에 등록한 항목을 제거해 중복 실행을 방지한다.
            RemoveExistingSystems(ref playerLoop);

            // Update 이후 정의한 업데이트 순서대로 삽입
            if (!InsertAfter(ref playerLoop, typeof(UnityEngine.PlayerLoop.Update), updateOrder))
            {
                Debug.LogError("Failed to insert GameLoop.");
                return;
            }

            // PlayerLoop 세팅
            PlayerLoop.SetPlayerLoop(playerLoop);
#if UNITY_EDITOR
            Application.quitting -= Uninstall;
            Application.quitting += Uninstall;
#endif
        }

        private static bool InsertAfter(ref PlayerLoopSystem playerLoop, Type targetType, UpdateStep[] updateOrder)
        {
            var systems = playerLoop.subSystemList;
            if (systems == null)
            {
                return false;
            }

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i].type == targetType)
                {
                    var newSystems = new PlayerLoopSystem[systems.Length + updateOrder.Length];
                    Array.Copy(systems, 0, newSystems, 0, i + 1);

                    for (int j = 0; j < updateOrder.Length; j++)
                    {
                        newSystems[i + 1 + j] = new PlayerLoopSystem
                        {
                            type = updateOrder[j].Type,
                            updateDelegate = updateOrder[j].Callback
                        };
                    }

                    Array.Copy(systems, i + 1, newSystems, i + 1 + updateOrder.Length, systems.Length - i - 1);
                    playerLoop.subSystemList = newSystems;
                    return true;
                }

                if (InsertAfter(ref systems[i], targetType, updateOrder))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveExistingSystems(ref PlayerLoopSystem playerLoop)
        {
            var systems = playerLoop.subSystemList;
            if (systems == null)
            {
                return;
            }

            var retainedSystems = new List<PlayerLoopSystem>(systems.Length);
            for (int i = 0; i < systems.Length; i++)
            {
                var system = systems[i];
                if (IsGameLoopSystem(system.type))
                {
                    continue;
                }

                RemoveExistingSystems(ref system);
                retainedSystems.Add(system);
            }

            playerLoop.subSystemList = retainedSystems.ToArray();
        }

        private static bool IsGameLoopSystem(Type type)
        {
            return type == typeof(MessageSystemUpdate)
                || type == typeof(PawnMovementResolveUpdate)
                || type == typeof(KinematicWorldUpdate);
        }

#if UNITY_EDITOR
        private static void Uninstall()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            RemoveExistingSystems(ref playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);
            Application.quitting -= Uninstall;
        }

        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            // Edit 모드에서 스크립트가 다시 로드되면 남아 있는 실행 항목을 정리한다.
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Uninstall();
            }
        }
#endif
    }
}
