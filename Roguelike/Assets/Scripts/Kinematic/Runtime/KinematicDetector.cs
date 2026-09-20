using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal static class KinematicDetector
    {
        internal static bool TryCollide(KinematicBody bodyA, KinematicBody bodyB, out Contact contact)
        {
            contact = default;

            return (bodyA.Shape.Shape, bodyB.Shape.Shape) switch
            {
                (Shape.Circle, Shape.Circle) =>
                    TryCircleCircle(bodyA, bodyB, out contact),

                (Shape.Circle, Shape.Box) =>
                    TryCircleBox(bodyA, bodyB, out contact),

                (Shape.Box, Shape.Circle) =>
                    TryBoxCircle(bodyA, bodyB, out contact),

                _ => false
            };
        }

        private static bool TryCircleCircle(in KinematicBody circleA, in KinematicBody circleB, out Contact contact)
        {
            contact = default;

            var delta = circleA.Center - circleB.Center;

            var radiusSum = circleA.Shape.Radius + circleB.Shape.Radius;
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

        private static bool TryBoxCircle(KinematicBody box, KinematicBody circle, out Contact contact)
        {
            contact = default;

            if (!TryCircleBox(circle, box, out Contact circleContact))
            {
                return false;
            }

            contact = new Contact(-circleContact.SeparationNormal, circleContact.PenetrationDepth);

            return true;
        }

        private static bool TryCircleBox(in KinematicBody circle, in KinematicBody box, out Contact contact)
        {
            contact = default;

            var circleCenter = circle.Center;
            var boxCenter = box.Center;
            var halfExtents = box.Shape.HalfExtents;
            var radius = circle.Shape.Radius;

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
