using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal readonly struct Contact
    {
        // 침투 깊이
        public float PenetrationDepth { get; }
        // A와B의 충돌 정보를 저장할 때 항상 B->A 방향으로 Normal을 구한다.
        public Vector2 SeparationNormal { get; }
        // 크기와 방향을 가지는 MTV
        public Vector2 SeparationMtv => SeparationNormal * PenetrationDepth;

        public Contact(Vector2 separationNormal, float penetrationDepth)
        {
            SeparationNormal = separationNormal;
            PenetrationDepth = penetrationDepth;
        }
    }

    internal static class KinematicCollision
    {
        private const float Epsilon = 0.00001f;

        internal static void GetPriorityCorrections(in Contact contact, int pushPriorityA, int pushPriorityB, out Vector2 correctionA, out Vector2 correctionB)
        {
            // PushPriority가 낮은 바디를 보정한다.
            var lowerIsA = pushPriorityA < pushPriorityB;

            // 낮은 우선순위의 바디에 MTV 전체를 적용한다.
            // 다른 바디나 벽에 의해 발생하는 겹침은 이후 Iteration에서 해결한다.
            correctionA = lowerIsA ? contact.SeparationMtv : Vector2.zero;
            correctionB = lowerIsA ? Vector2.zero : -contact.SeparationMtv;
        }

        internal static bool TryCollide(Vector2 centerA, in ColliderInfo shapeA, Vector2 centerB, in ColliderInfo shapeB, out Contact contact)
        {
            var hasCollision = TryGetContact(centerA, shapeA, centerB, shapeB, out contact);

            // 부동소수점 정밀도 한계로, 분담한 미세 MTV를 좌표에 더해도 반올림되어 실제 위치가 바뀌지 않을 수 있다.
            // 이로 인해 충돌은 계속 검출되지만 겹침은 해소되지 않는 반복을 막기 위해 허용 오차 이하의 침투는 무시한다.
            return hasCollision && contact.PenetrationDepth > Epsilon;
        }

        // Sweep의 시작 접촉에는 위치 보정용 허용 오차를 적용하지 않는다. 작은 겹침도 접촉으로 유지해야 안쪽으로 파고드는 이동을 막을 수 있다.
        // 물론 한 프레임 접촉 감지를 못해 파고들더라로 다음 프레임에 감지가 되어서 안쪽으로 계속 파고 들 수는 없으나 한 프레임 파고 들고
        // 다음 프레임에 InitialSolver가 감지해서 밖으로 Solving하는 과정에서 떨림이 유발 발생하기 때문에 오차를 여기서는 허용하지 않는다.
        internal static bool TryGetContact(Vector2 centerA, in ColliderInfo shapeA, Vector2 centerB, in ColliderInfo shapeB, out Contact contact)
        {
            contact = default;

            var hasCollision = (shapeA.Shape, shapeB.Shape) switch
            {
                (Shape.Circle, Shape.Circle) =>
                    TryCircleCircle(centerA, shapeA.Radius, centerB, shapeB.Radius, out contact),

                (Shape.Circle, Shape.Box) =>
                    TryCircleBox(centerA, shapeA.Radius, centerB, shapeB.HalfExtents, out contact),

                (Shape.Box, Shape.Circle) =>
                    TryBoxCircle(centerA, shapeA.HalfExtents, centerB, shapeB.Radius, out contact),

                (Shape.Box, Shape.Box) =>
                    TryBoxBox(centerA, shapeA.HalfExtents, centerB, shapeB.HalfExtents, out contact),

                _ => false
            };

            return hasCollision;
        }

        private static bool TryBoxBox(Vector2 centerA, Vector2 halfExtentsA, Vector2 centerB, Vector2 halfExtentsB, out Contact contact)
        {
            contact = default;

            var delta = centerA - centerB;
            var combinedHalfExtents = halfExtentsA + halfExtentsB;
            var overlapX = combinedHalfExtents.x - Mathf.Abs(delta.x);

            if (overlapX <= 0f)
            {
                return false;
            }

            var overlapY = combinedHalfExtents.y - Mathf.Abs(delta.y);

            if (overlapY <= 0f)
            {
                return false;
            }

            if (overlapX < overlapY)
            {
                var normalX = delta.x < 0f ? -1f : 1f;
                contact = new Contact(new Vector2(normalX, 0f), overlapX);
            }
            else
            {
                var normalY = delta.y < 0f ? -1f : 1f;
                contact = new Contact(new Vector2(0f, normalY), overlapY);
            }

            return true;
        }

        private static bool TryCircleCircle(Vector2 centerA, float radiusA, Vector2 centerB, float radiusB, out Contact contact)
        {
            contact = default;

            var delta = centerA - centerB;

            var radiusSum = radiusA + radiusB;
            var distanceSquared = delta.sqrMagnitude;

            if (distanceSquared >= radiusSum * radiusSum)
            {
                return false;
            }

            var distance = Mathf.Sqrt(distanceSquared);
            var normal = distance > 0f ? delta.normalized : Vector2.right;
            var penetrationDepth = radiusSum - distance;

            contact = new Contact(normal, penetrationDepth);

            return true;
        }

        private static bool TryBoxCircle(Vector2 boxCenter, Vector2 boxHalfExtents, Vector2 circleCenter, float circleRadius, out Contact contact)
        {
            contact = default;

            if (TryCircleBox(circleCenter, circleRadius, boxCenter, boxHalfExtents, out var circleContact) == false)
            {
                return false;
            }

            contact = new Contact(-circleContact.SeparationNormal, circleContact.PenetrationDepth);

            return true;
        }

        private static bool TryCircleBox(Vector2 circleCenter, float radius, Vector2 boxCenter, Vector2 halfExtents, out Contact contact)
        {
            contact = default;

            // 경로 검사와 같은 박스 중심 상대 좌표를 사용한다.
            // 월드 좌표에 면 위치를 더했다가 빼면 반올림 때문에 시작 겹침 판정이 달라질 수 있다.
            var circleOffset = circleCenter - boxCenter;
            var boxMin = -halfExtents;
            var boxMax = halfExtents;

            if (IsInsideOnAABB(circleOffset, boxMin, boxMax))
            {
                var distanceToLeft = circleOffset.x - boxMin.x;
                var distanceToRight = boxMax.x - circleOffset.x;
                var distanceToBottom = circleOffset.y - boxMin.y;
                var distanceToTop = boxMax.y - circleOffset.y;

                var nearestFaceDistance = distanceToLeft;
                var separationNormal = Vector2.left;

                if (distanceToRight < nearestFaceDistance)
                {
                    nearestFaceDistance = distanceToRight;
                    separationNormal = Vector2.right;
                }

                if (distanceToBottom < nearestFaceDistance)
                {
                    nearestFaceDistance = distanceToBottom;
                    separationNormal = Vector2.down;
                }

                if (distanceToTop < nearestFaceDistance)
                {
                    nearestFaceDistance = distanceToTop;
                    separationNormal = Vector2.up;
                }

                contact = new Contact(separationNormal, radius + nearestFaceDistance);

                return true;
            }

            var closestPoint = new Vector2(Mathf.Clamp(circleOffset.x, boxMin.x, boxMax.x), Mathf.Clamp(circleOffset.y, boxMin.y, boxMax.y));

            var delta = circleOffset - closestPoint;
            var distanceSquared = delta.sqrMagnitude;

            if (distanceSquared >= radius * radius)
            {
                return false;
            }

            var normal = delta.normalized;
            var penetrationDepth = radius - Mathf.Sqrt(distanceSquared);

            contact = new Contact(normal, penetrationDepth);

            return true;
        }

        private static bool IsInsideOnAABB(Vector2 point, Vector2 boxMin, Vector2 boxMax)
        {
            return point.x >= boxMin.x &&
                   point.x <= boxMax.x &&
                   point.y >= boxMin.y &&
                   point.y <= boxMax.y;
        }
    }
}
