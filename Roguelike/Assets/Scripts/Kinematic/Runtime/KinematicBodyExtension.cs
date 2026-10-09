using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal static class KinematicBodyExtension
    {
        internal static bool CanCollideWith(this KinematicBody bodyA, KinematicBody bodyB)
        {
            // 고스트 바디는 Pawn만 통과하며, 상대 바디의 충돌 반응도 함께 끈다.
            if ((bodyA.Ghost.IsGhost && bodyA.Ghost.CanGhostThrough(bodyB)) || (bodyB.Ghost.IsGhost && bodyB.Ghost.CanGhostThrough(bodyA)))
            {
                return false;
            }

            // 2D Layer Collision Matrix에서 충돌이 허용된 레이어 쌍만 양쪽 모두 반응한다.
            return !Physics2D.GetIgnoreLayerCollision(bodyA.Layer, bodyB.Layer);
        }

        internal static bool TryCollide(this KinematicBody bodyA, KinematicBody bodyB, out Contact contact)
        {
            return TryCollide(bodyA.Center, bodyA.Shape, bodyB.Center, bodyB.Shape, out contact);
        }

        internal static bool TryCollide(Vector2 centerA, in ColliderInfo shapeA, Vector2 centerB, in ColliderInfo shapeB, out Contact contact)
        {
            contact = default;

            return (shapeA.Shape, shapeB.Shape) switch
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

            if (!TryCircleBox(circleCenter, circleRadius, boxCenter, boxHalfExtents, out var circleContact))
            {
                return false;
            }

            contact = new Contact(-circleContact.SeparationNormal, circleContact.PenetrationDepth);

            return true;
        }

        private static bool TryCircleBox(Vector2 circleCenter, float radius, Vector2 boxCenter, Vector2 halfExtents, out Contact contact)
        {
            contact = default;

            var boxMin = boxCenter - halfExtents;
            var boxMax = boxCenter + halfExtents;

            if (IsInsideOnAABB(circleCenter, boxMin, boxMax))
            {
                var distanceToLeft = circleCenter.x - boxMin.x;
                var distanceToRight = boxMax.x - circleCenter.x;
                var distanceToBottom = circleCenter.y - boxMin.y;
                var distanceToTop = boxMax.y - circleCenter.y;

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

            var closestPoint = new Vector2(Mathf.Clamp(circleCenter.x, boxMin.x, boxMax.x), Mathf.Clamp(circleCenter.y, boxMin.y, boxMax.y));

            var delta = circleCenter - closestPoint;
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
