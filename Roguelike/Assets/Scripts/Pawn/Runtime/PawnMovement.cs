using Kinematic.Data;
using Kinematic.Runtime;
using Pawn.Interface;
using System.Collections.Generic;
using UnityEngine;

namespace Pawn.Runtime
{
    internal class PawnMovement : IPawnMovement
    {
        private readonly Queue<MoveRequest> moveRequests = new();
        private KinematicBody kinematicBody;
        private AnimationCurve accelerationCurve;
        private float accelElapsedTime;

        internal PawnMovement(Transform owner, ColliderInfo colldierInfo, AnimationCurve accelCurve)
        {
            kinematicBody = new KinematicBody(owner, colldierInfo, false);
            accelerationCurve = accelCurve;

            KinematicWorld.Register(kinematicBody);
        }

        public void Enqueue(MoveRequest moveRequest)
        {
            moveRequests.Enqueue(moveRequest);
        }

        void IPawnMover.OnMove(float dt)
        {
            var hasActive = false;
            var activeVelocity = Vector2.zero;
            var hasPassive = false;
            var passiveImpulse = Vector2.zero;
            var hasTeleport = false;
            var teleportPosition = Vector2.zero;

            // 이번 프레임에 들어온 이동 요청 순회하면서 일괄 처리
            while (moveRequests.TryDequeue(out var request))
            {
                switch (request.MoveType)
                {
                    // Active 타입은 여러 요청이 있더라도 마지막 요청으로 덮어쓴다.
                    case MoveType.Active:
                        activeVelocity = request.Velocity;
                        hasActive = true;
                        break;
                    // Passive는 모든 요청을 누적한다.
                    // 음 passive는 속도가 점점 줄어야되니까 추가적인 처리가 필요할 것 같고 activeVelocity가 있다면 passiveVelocity가 threshold 값 이상 존재한다면 무시/Clear해서 적용 안해야 할 것 같은데
                    case MoveType.Passive:
                        passiveImpulse += request.Velocity;
                        hasPassive = true;
                        break;
                    // 마지막 Teleport 요청을 적용하며 다른 이동 요청보다 우선한다.
                    case MoveType.Teleport:
                        teleportPosition = request.Velocity;
                        hasTeleport = true;
                        break;
                }
            }

            if (hasTeleport == true)
            {
                kinematicBody.Teleport(teleportPosition);
                return;
            }

            accelElapsedTime = hasActive ? accelElapsedTime + dt : 0f;

            if (hasActive == false && hasPassive == false)
            {
                return;
            }

            var speedMultiplier = accelerationCurve.Evaluate(accelElapsedTime);
            speedMultiplier = Mathf.Clamp01(speedMultiplier);
            var moveDelta = activeVelocity * (speedMultiplier * dt);

            kinematicBody.Move(moveDelta);
        }

        void IPawnMovement.Dispose()
        {
            moveRequests.Clear();
            KinematicWorld.Unregister(kinematicBody);
        }
    }
}
