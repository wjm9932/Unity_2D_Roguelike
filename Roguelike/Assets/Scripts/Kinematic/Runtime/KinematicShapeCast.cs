using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    internal static class KinematicShapeCast
    {
        private readonly struct CastResult
        {
            internal float Fraction { get; }
            internal Vector2 Normal { get; }

            internal CastResult(float fraction, Vector2 normal)
            {
                Fraction = fraction;
                Normal = normal;
            }
        }

        private const float Epsilon = 0.000001f;

        internal static bool TryCast(Vector2 movingCenter, in ColliderInfo movingShape, KinematicBody targetBody, Vector2 moveDelta, out ShapeCastHit hit)
        {
            hit = default;

            if (moveDelta.sqrMagnitude <= Epsilon * Epsilon)
            {
                return false;
            }

            // 캐스트 시작 전 충돌 상태인지 체크
            if (KinematicBodyExtension.TryCollide(movingCenter, movingShape, targetBody.Center, targetBody.Shape, out var contact))
            {
                // 겹쳐있더라도 cast 방향이 충돌 solving 방향과 같다면 cast허용한다
                if (Vector2.Dot(moveDelta, contact.SeparationNormal) >= 0f)
                {
                    return false;
                }

                hit = CreateHit(movingCenter, movingShape, targetBody, moveDelta, 0f, contact.SeparationNormal);
                return true;
            }

            CastResult castResult = default;

            var hasHit = (movingShape.Shape, targetBody.Shape.Shape) switch
            {
                (Shape.Circle, Shape.Circle) =>
                    TryCircleCircle(movingCenter, movingShape.Radius, targetBody, moveDelta, out castResult),

                (Shape.Circle, Shape.Box) =>
                    TryCircleBox(movingCenter, movingShape.Radius, targetBody, moveDelta, out castResult),

                (Shape.Box, Shape.Circle) =>
                    TryBoxCircle(movingCenter, movingShape.HalfExtents, targetBody, moveDelta, out castResult),

                (Shape.Box, Shape.Box) =>
                    TryBoxBox(movingCenter, movingShape.HalfExtents, targetBody, moveDelta, out castResult),

                _ => false
            };

            if (!hasHit)
            {
                return false;
            }

            hit = CreateHit(movingCenter, movingShape, targetBody, moveDelta, castResult.Fraction, castResult.Normal);
            return true;
        }

        private static ShapeCastHit CreateHit(Vector2 movingCenter, in ColliderInfo movingShape, KinematicBody targetBody, Vector2 moveDelta, float fraction, Vector2 normal)
        {
            var centerAtHit = movingCenter + moveDelta * fraction;
            var point = GetContactPoint(movingShape, centerAtHit, normal);
            var distance = moveDelta.magnitude * fraction;

            return new ShapeCastHit(targetBody, point, normal, distance, fraction);
        }

        private static Vector2 GetContactPoint(in ColliderInfo shape, Vector2 center, Vector2 normal)
        {
            if (shape.Shape == Shape.Circle)
            {
                return center - normal * shape.Radius;
            }

            var halfExtents = shape.HalfExtents;
            var supportOffset = new Vector2(normal.x == 0f ? 0f : -Mathf.Sign(normal.x) * halfExtents.x, normal.y == 0f ? 0f : -Mathf.Sign(normal.y) * halfExtents.y);

            return center + supportOffset;
        }

        private static bool TryCircleCircle(Vector2 movingCenter, float movingRadius, KinematicBody targetCircle, Vector2 moveDelta, out CastResult result)
        {
            var origin = movingCenter - targetCircle.Center;
            var radius = movingRadius + targetCircle.Shape.Radius;

            return TryPointCircle(origin, moveDelta, radius, out result);
        }

        private static bool TryCircleBox(Vector2 movingCenter, float movingRadius, KinematicBody targetBox, Vector2 moveDelta, out CastResult result)
        {
            var origin = movingCenter - targetBox.Center;

            return TryPointRoundedBox(origin, moveDelta, targetBox.Shape.HalfExtents, movingRadius, out result);
        }

        private static bool TryBoxCircle(Vector2 movingCenter, Vector2 movingHalfExtents, KinematicBody targetCircle, Vector2 moveDelta, out CastResult result)
        {
            var origin = movingCenter - targetCircle.Center;

            return TryPointRoundedBox(origin, moveDelta, movingHalfExtents, targetCircle.Shape.Radius, out result);
        }

        private static bool TryBoxBox(Vector2 movingCenter, Vector2 movingHalfExtents, KinematicBody targetBox, Vector2 moveDelta, out CastResult result)
        {
            var origin = movingCenter - targetBox.Center;
            var expandedHalfExtents = movingHalfExtents + targetBox.Shape.HalfExtents;

            return TryPointBox(origin, moveDelta, expandedHalfExtents, out result);
        }

        private static bool TryPointCircle(Vector2 origin, Vector2 moveDelta, float radius, out CastResult result)
        {
            result = default;

            var a = Vector2.Dot(moveDelta, moveDelta);
            var b = Vector2.Dot(origin, moveDelta);
            var c = Vector2.Dot(origin, origin) - radius * radius;
            var discriminant = b * b - a * c;

            if (discriminant < 0f)
            {
                return false;
            }

            var squareRoot = Mathf.Sqrt(discriminant);
            var fraction = (-b - squareRoot) / a;

            if (fraction < -Epsilon || fraction > 1f + Epsilon)
            {
                return false;
            }

            fraction = Mathf.Clamp01(fraction);
            var normal = (origin + moveDelta * fraction).normalized;

            if (normal.sqrMagnitude <= Epsilon * Epsilon || Vector2.Dot(moveDelta, normal) >= -Epsilon)
            {
                return false;
            }

            result = new CastResult(fraction, normal);
            return true;
        }

        private static bool TryPointBox(Vector2 origin, Vector2 moveDelta, Vector2 halfExtents, out CastResult result)
        {
            result = default;

            var enterFraction = float.NegativeInfinity;
            var exitFraction = float.PositiveInfinity;
            var enterNormal = Vector2.zero;

            if (!UpdateSlab(origin.x, moveDelta.x, -halfExtents.x, halfExtents.x, Vector2.left, Vector2.right, ref enterFraction, ref exitFraction, ref enterNormal))
            {
                return false;
            }

            if (!UpdateSlab(origin.y, moveDelta.y, -halfExtents.y, halfExtents.y, Vector2.down, Vector2.up, ref enterFraction, ref exitFraction, ref enterNormal))
            {
                return false;
            }

            if (enterFraction < -Epsilon || enterFraction > 1f + Epsilon || enterFraction > exitFraction)
            {
                return false;
            }

            enterFraction = Mathf.Clamp01(enterFraction);

            if (enterNormal == Vector2.zero || Vector2.Dot(moveDelta, enterNormal) >= -Epsilon)
            {
                return false;
            }

            result = new CastResult(enterFraction, enterNormal);
            return true;
        }

        private static bool UpdateSlab(float origin, float moveDelta, float min, float max, Vector2 minNormal, Vector2 maxNormal, ref float enterFraction, ref float exitFraction, ref Vector2 enterNormal)
        {
            if (Mathf.Abs(moveDelta) <= Epsilon)
            {
                return origin >= min && origin <= max;
            }

            var inverseDisplacement = 1f / moveDelta;
            var firstFraction = (min - origin) * inverseDisplacement;
            var secondFraction = (max - origin) * inverseDisplacement;
            var firstNormal = minNormal;

            if (firstFraction > secondFraction)
            {
                (firstFraction, secondFraction) = (secondFraction, firstFraction);
                firstNormal = maxNormal;
            }

            if (firstFraction > enterFraction)
            {
                enterFraction = firstFraction;
                enterNormal = firstNormal;
            }

            exitFraction = Mathf.Min(exitFraction, secondFraction);
            return enterFraction <= exitFraction;
        }

        private static bool TryPointRoundedBox(Vector2 origin, Vector2 moveDelta, Vector2 coreHalfExtents, float radius, out CastResult result)
        {
            if (radius <= Epsilon)
            {
                return TryPointBox(origin, moveDelta, coreHalfExtents, out result);
            }

            result = default;
            var closestFraction = float.PositiveInfinity;
            var closestNormal = Vector2.zero;

            TryRoundedBoxVerticalSide(origin, moveDelta, coreHalfExtents, radius, -1f, ref closestFraction, ref closestNormal);
            TryRoundedBoxVerticalSide(origin, moveDelta, coreHalfExtents, radius, 1f, ref closestFraction, ref closestNormal);
            TryRoundedBoxHorizontalSide(origin, moveDelta, coreHalfExtents, radius, -1f, ref closestFraction, ref closestNormal);
            TryRoundedBoxHorizontalSide(origin, moveDelta, coreHalfExtents, radius, 1f, ref closestFraction, ref closestNormal);

            for (var xSign = -1f; xSign <= 1f; xSign += 2f)
            {
                for (var ySign = -1f; ySign <= 1f; ySign += 2f)
                {
                    var corner = new Vector2(coreHalfExtents.x * xSign, coreHalfExtents.y * ySign);

                    if (!TryPointCircle(origin - corner, moveDelta, radius, out var cornerResult))
                    {
                        continue;
                    }

                    var pointAtHit = origin + moveDelta * cornerResult.Fraction;
                    var cornerOffset = pointAtHit - corner;

                    if (cornerOffset.x * xSign < -Epsilon || cornerOffset.y * ySign < -Epsilon)
                    {
                        continue;
                    }

                    SetClosestResult(cornerResult, ref closestFraction, ref closestNormal);
                }
            }

            if (float.IsPositiveInfinity(closestFraction))
            {
                return false;
            }

            result = new CastResult(closestFraction, closestNormal);
            return true;
        }

        private static void TryRoundedBoxVerticalSide(Vector2 origin, Vector2 moveDelta, Vector2 coreHalfExtents, float radius, float sideSign, ref float closestFraction, ref Vector2 closestNormal)
        {
            var normal = new Vector2(sideSign, 0f);

            if (Vector2.Dot(moveDelta, normal) >= -Epsilon)
            {
                return;
            }

            var sideX = sideSign * (coreHalfExtents.x + radius);
            var fraction = (sideX - origin.x) / moveDelta.x;

            if (fraction < -Epsilon || fraction > 1f + Epsilon)
            {
                return;
            }

            fraction = Mathf.Clamp01(fraction);
            var hitY = origin.y + moveDelta.y * fraction;

            if (hitY < -coreHalfExtents.y - Epsilon || hitY > coreHalfExtents.y + Epsilon)
            {
                return;
            }

            SetClosestResult(new CastResult(fraction, normal), ref closestFraction, ref closestNormal);
        }

        private static void TryRoundedBoxHorizontalSide(Vector2 origin, Vector2 moveDelta, Vector2 coreHalfExtents, float radius, float sideSign, ref float closestFraction, ref Vector2 closestNormal)
        {
            var normal = new Vector2(0f, sideSign);

            if (Vector2.Dot(moveDelta, normal) >= -Epsilon)
            {
                return;
            }

            var sideY = sideSign * (coreHalfExtents.y + radius);
            var fraction = (sideY - origin.y) / moveDelta.y;

            if (fraction < -Epsilon || fraction > 1f + Epsilon)
            {
                return;
            }

            fraction = Mathf.Clamp01(fraction);
            var hitX = origin.x + moveDelta.x * fraction;

            if (hitX < -coreHalfExtents.x - Epsilon || hitX > coreHalfExtents.x + Epsilon)
            {
                return;
            }

            SetClosestResult(new CastResult(fraction, normal), ref closestFraction, ref closestNormal);
        }

        private static void SetClosestResult(in CastResult candidate, ref float closestFraction, ref Vector2 closestNormal)
        {
            if (candidate.Fraction >= closestFraction)
            {
                return;
            }

            closestFraction = candidate.Fraction;
            closestNormal = candidate.Normal;
        }
    }
}
