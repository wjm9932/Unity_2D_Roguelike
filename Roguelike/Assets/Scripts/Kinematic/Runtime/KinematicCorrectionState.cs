using Kinematic.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime
{
    // 이번 Solve에서 바디가 밀림을 통해 상속받은 우선순위를 관리한다.
    internal sealed class KinematicCorrectionState
    {
        // 바디 자체의 우선순위는 바꾸지 않고, 이번 Solve에서 전달받은 밀림의 우선순위만 저장한다.
        // 바디 자체의 우선순위보다 작아질 수 없다.
        internal MovePriority PushPriority { get; private set; }

        internal void InheritPushPriority(KinematicCorrectionState source, Vector2 sourceMoveDelta, Vector2 correction)
        {
            // 상대의 접근 이동이 만든 밀림만 전달한다. 정지한 상대에 부딪쳐 자기 이동을 취소한 경우는 제외한다.
            if ((int)source.PushPriority > (int)PushPriority && Vector2.Dot(sourceMoveDelta, correction) > 0f)
            {
                PushPriority = source.PushPriority;
            }
        }

        internal static void Reset(List<KinematicCorrectionState> states, IReadOnlyList<KinematicBody> bodies)
        {
            while (states.Count < bodies.Count)
            {
                states.Add(new KinematicCorrectionState());
            }

            for (var bodyIndex = 0; bodyIndex < bodies.Count; bodyIndex++)
            {
                states[bodyIndex].PushPriority = bodies[bodyIndex].Priority;
            }
        }
    }
}
