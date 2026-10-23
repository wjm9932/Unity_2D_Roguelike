using Pawn.Data;
using Pawn.Interface;
using UnityEngine;

namespace Pawn.Runtime
{
    internal class PawnMoveProcessor : IPawnMoveProcessor
    {
        private const float PassiveThreshold = 2f;

        private readonly PawnMotor pawnMotor;
        private readonly IMoveRequestQueue requestQueue;
        private readonly AnimationCurve accelerationCurve;
        private readonly AnimationCurve passiveDecelerationCurve;
        private readonly float PassiveMoveDuration;

        private float activeElapsedTime;

        private Vector2 initialPassiveVelocity;
        private float passiveElapsedTime;
        private bool isPassiveMoving;

        internal PawnMoveProcessor(PawnMotor motor, IMoveRequestQueue queue, AnimationCurve accelCurve, AnimationCurve passiveCurve)
        {
            pawnMotor = motor;
            requestQueue = queue;
            accelerationCurve = accelCurve;
            passiveDecelerationCurve = passiveCurve;
            PassiveMoveDuration = passiveCurve.length > 0 ? Mathf.Max(0f, passiveCurve[passiveCurve.length - 1].time) : 0f;
        }

        void IPawnMoveProcessor.OnMove(float dt)
        {
            var hasActive = false;
            var requestedActiveVelocity = Vector2.zero;

            var hasPassive = false;
            var requestedPassiveVelocity = Vector2.zero;

            var hasTeleport = false;
            var teleportPosition = Vector2.zero;

            // 이번 프레임에 들어온 이동 요청 순회하면서 일괄 처리
            while (requestQueue.TryDequeue(out var request))
            {
                switch (request.MoveType)
                {
                    case MoveType.Active:
                        requestedActiveVelocity = request.Velocity;
                        hasActive = true;
                        break;

                    case MoveType.Passive:
                        requestedPassiveVelocity = request.Velocity;
                        hasPassive = true;
                        break;

                    case MoveType.Teleport:
                        teleportPosition = request.Velocity;
                        hasTeleport = true;
                        break;
                }
            }

            // Teleport는 다른 이동 요청보다 우선한다.
            if (hasTeleport)
            {
                pawnMotor.Teleport(teleportPosition);
                return;
            }

            // 새로운 Passive 요청이 들어오면 기존 Passive 속도는 무시한 후 감속을 다시 시작한다.
            if (hasPassive)
            {
                StartPassiveMovement(requestedPassiveVelocity);
            }

            // 각 이동 속도를 계산하고 최종 속도를 합성한다.
            var passiveVelocity = ResolvePassiveVelocity(dt);
            var activeVelocity = ResolveActiveVelocity(hasActive, requestedActiveVelocity, passiveVelocity, dt);
            var finalVelocity = activeVelocity + passiveVelocity;

            pawnMotor.Move(finalVelocity * dt);
        }

        private void StartPassiveMovement(Vector2 velocity)
        {
            isPassiveMoving = true;
            initialPassiveVelocity = velocity;
            passiveElapsedTime = 0f;
        }

        private Vector2 ResolvePassiveVelocity(float dt)
        {
            if (isPassiveMoving == false)
            {
                return Vector2.zero;
            }

            return UpdatePassiveVelocity(dt);
        }

        private Vector2 UpdatePassiveVelocity(float dt)
        {
            // 시간을 먼저 갱신하면 dt가 지속 시간을 초과할 때 첫 프레임부터 속도가 0이 될 수 있다.
            // 또한 종료 전 마지막 프레임의 속도가 0으로 평가되어 이동량이 발생하지 않을 수 있으므로, dt를 더하기 전의 경과 시간으로 커브를 평가한다.
            var speedMultiplier = Mathf.Clamp01(passiveDecelerationCurve.Evaluate(passiveElapsedTime));

            passiveElapsedTime += dt;

            isPassiveMoving = passiveElapsedTime < PassiveMoveDuration;

            return initialPassiveVelocity * speedMultiplier;
        }

        private Vector2 ResolveActiveVelocity(bool hasActive, Vector2 requestedVelocity, Vector2 passiveVelocity, float dt)
        {
            if (hasActive == false || passiveVelocity.sqrMagnitude > PassiveThreshold * PassiveThreshold)
            {
                activeElapsedTime = 0f;
                return Vector2.zero;
            }

            return UpdateActiveVelocity(requestedVelocity, dt);
        }

        private Vector2 UpdateActiveVelocity(Vector2 activeVelocity, float dt)
        {
            activeElapsedTime += dt;

            var speedMultiplier = Mathf.Clamp01(accelerationCurve.Evaluate(activeElapsedTime));

            return activeVelocity * speedMultiplier;
        }

        public void Dispose()
        {
            requestQueue.Clear();
        }
    }
}