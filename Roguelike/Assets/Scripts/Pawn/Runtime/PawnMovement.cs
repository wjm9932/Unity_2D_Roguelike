using Pawn.Interface;
using System.Collections.Generic;
using UnityEngine;

namespace Pawn.Runtime
{
    internal class PawnMovement : IPawnMovement
    {
        private Queue<MoveRequest> moveRequests = new();

        private AnimationCurve accelerationCurve;

        private Rigidbody2D rb;

        private float accelElapsedTime;

        internal PawnMovement(Rigidbody2D rb, AnimationCurve accelerationCurve)
        {
            this.rb = rb;
            this.accelerationCurve = accelerationCurve;
        }

        public void Enqueue(MoveRequest moveRequest) => moveRequests.Enqueue(moveRequest);

        public void OnMove(float dt)
        {
            Vector2 activeVelocity = Vector2.zero;
            Vector2 passiveVelocity = Vector2.zero;
            Vector2? teleportPosition = null;

            // 이번 프레임에 들어온 이동 요청 순회하면서 일괄 처리
            while (moveRequests.TryDequeue(out var request))
            {
                switch (request.MoveType)
                {
                    // Active 타입은 여러 요청이 있더라도 마지막 요청으로 덮어쓴다.
                    case MoveType.Active:
                        activeVelocity = request.Vector;
                        break;
                    // Passive는 모든 요청을 누적한다.
                    // 음 passive는 속도가 점점 줄어야되니까 추가적인 처리가 필요할 것 같고 activeVelocity가 있다면 passiveVelocity가 threshold 값 이상 존재한다면 무시/Clear해서 적용 안해야 할 것 같은데
                    case MoveType.Passive:
                        passiveVelocity += request.Vector;
                        break;
                    // 마지막 Teleport 요청을 적용하며 다른 이동 요청보다 우선한다.
                    case MoveType.Teleport:
                        teleportPosition = request.Vector;
                        break;
                }
            }

            if (teleportPosition.HasValue)
            {
                rb.MovePosition(teleportPosition.Value);
                return;
            }

            // Active 요청이 없으면 0으로 초기화된 값이 연산 없이 그대로 유지되므로, 부동소수점 오차 없이 Vector2.zero와 직접 비교할 수 있다.
            // 애초에 Vector2 ==은 오버로딩 되어 있어 두 벡터의 차이가 매우 작은지도 고려한다.
            accelElapsedTime = activeVelocity == Vector2.zero ? 0f : accelElapsedTime + dt;

            var speedMultiplier = accelerationCurve.Evaluate(accelElapsedTime);
            speedMultiplier = Mathf.Clamp01(speedMultiplier);

            rb.linearVelocity = activeVelocity * speedMultiplier;
        }
    }
}
