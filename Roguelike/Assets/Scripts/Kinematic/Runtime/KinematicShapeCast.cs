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
            internal bool IsGrazing { get; }

            internal CastResult(float fraction, Vector2 normal, bool isGrazing = false)
            {
                Fraction = fraction;
                Normal = normal;
                IsGrazing = isGrazing;
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

            // Cast 하기 전 이미 겹쳐있다면 겹쳐있는 물체 반환
            if (KinematicBodyExtension.TryCollide(movingCenter, movingShape, targetBody.Center, targetBody.Shape, out var contact))
            {
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

            if (hasHit == false)
            {
                return false;
            }

            hit = CreateHit(movingCenter, movingShape, targetBody, moveDelta, castResult.Fraction, castResult.Normal, castResult.IsGrazing);

            return true;
        }

        private static ShapeCastHit CreateHit(Vector2 movingCenter, in ColliderInfo movingShape, KinematicBody targetBody, Vector2 moveDelta, float fraction, Vector2 normal, bool isGrazing = false)
        {
            var centerAtHit = movingCenter + moveDelta * fraction;
            var point = GetContactPoint(centerAtHit, movingShape, targetBody, normal);
            var distance = moveDelta.magnitude * fraction;

            return new ShapeCastHit(targetBody, point, normal, distance, fraction, isGrazing);
        }

        private static Vector2 GetContactPoint(Vector2 movingCenter, in ColliderInfo movingShape, KinematicBody targetBody, Vector2 normal)
        {
            if (movingShape.Shape == Shape.Circle)
            {
                return movingCenter - normal * movingShape.Radius;
            }

            if (targetBody.Shape.Shape == Shape.Circle)
            {
                return targetBody.Center + normal * targetBody.Shape.Radius;
            }

            var movingHalfExtents = movingShape.HalfExtents;
            var targetHalfExtents = targetBody.Shape.HalfExtents;

            // 좌/우 세로면이 충돌 법선인 경우
            if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
            {
                // max of min
                var overlapMinY = Mathf.Max(movingCenter.y - movingHalfExtents.y, targetBody.Center.y - targetHalfExtents.y);
                // min of max
                var overlapMaxY = Mathf.Min(movingCenter.y + movingHalfExtents.y, targetBody.Center.y + targetHalfExtents.y);

                // 겹치는 구간 중 중앙을 충돌 point로 지정
                return new Vector2(targetBody.Center.x + normal.x * targetHalfExtents.x, (overlapMinY + overlapMaxY) * 0.5f);
            }

            // 위/아래 가로면이 충돌 법선인 경우
            var overlapMinX = Mathf.Max(movingCenter.x - movingHalfExtents.x, targetBody.Center.x - targetHalfExtents.x);
            var overlapMaxX = Mathf.Min(movingCenter.x + movingHalfExtents.x, targetBody.Center.x + targetHalfExtents.x);

            // 겹치는 구간 중 중앙을 충돌 point로 지정
            return new Vector2((overlapMinX + overlapMaxX) * 0.5f, targetBody.Center.y + normal.y * targetHalfExtents.y);
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

            if (normal.sqrMagnitude <= Epsilon * Epsilon)
            {
                return false;
            }

            result = new CastResult(fraction, normal, discriminant <= 0f);
            return true;
        }

        private static bool TryPointBox(Vector2 origin, Vector2 moveDelta, Vector2 halfExtents, out CastResult result)
        {
            result = default;

            if (!TryGetAxisInterval(origin.x, moveDelta.x, -halfExtents.x, halfExtents.x, Vector2.left, Vector2.right, out var enterX, out var exitX, out var normalX) ||
                !TryGetAxisInterval(origin.y, moveDelta.y, -halfExtents.y, halfExtents.y, Vector2.down, Vector2.up, out var enterY, out var exitY, out var normalY))
            {
                return false;
            }

            var enterFraction = Mathf.Max(enterX, enterY);
            var exitFraction = Mathf.Min(exitX, exitY);

            if (enterFraction < -Epsilon || enterFraction > 1f + Epsilon || enterFraction > exitFraction)
            {
                return false;
            }

            var isGrazing = false;
            var enterNormal = enterX >= enterY ? normalX : normalY;

            // 면 스침은 해당 면의 법선을 사용하고, 모서리 스침은 진입 축의 법선을 유지한다.
            if (Mathf.Abs(moveDelta.x) <= Epsilon && Mathf.Abs(Mathf.Abs(origin.x) - halfExtents.x) <= Epsilon)
            {
                isGrazing = true;
                enterNormal = origin.x < 0f ? Vector2.left : Vector2.right;
            }
            else if (Mathf.Abs(moveDelta.y) <= Epsilon && Mathf.Abs(Mathf.Abs(origin.y) - halfExtents.y) <= Epsilon)
            {
                isGrazing = true;
                enterNormal = origin.y < 0f ? Vector2.down : Vector2.up;
            }
            // 모서리 한 점을 스치는 경우
            else if (Mathf.Abs(enterFraction - exitFraction) <= Epsilon)
            {
                isGrazing = true;
            }

            if (enterNormal == Vector2.zero)
            {
                return false;
            }

            result = new CastResult(Mathf.Clamp01(enterFraction), enterNormal, isGrazing);

            return true;
        }

        private static bool TryGetAxisInterval(float origin, float moveDelta, float min, float max, Vector2 minNormal, Vector2 maxNormal, out float enter, out float exit, out Vector2 enterNormal)
        {
            enter = float.NegativeInfinity;
            exit = float.PositiveInfinity;
            enterNormal = Vector2.zero;

            if (Mathf.Abs(moveDelta) <= Epsilon)
            {
                return origin >= min && origin <= max;
            }

            var inverse = 1 / moveDelta;
            enter = (min - origin)  * inverse;
            exit = (max - origin) * inverse;
            enterNormal = minNormal;

            if (enter > exit)
            {
                (enter, exit) = (exit, enter);
                enterNormal = maxNormal;
            }

            return true;
        }

        private static bool TryPointRoundedBox(Vector2 origin, Vector2 moveDelta, Vector2 coreHalfExtents, float radius, out CastResult result)
        {
            // radius가 충분히 작다면 그냥 point vs box로 처리한다.
            if (radius <= Epsilon)
            {
                return TryPointBox(origin, moveDelta, coreHalfExtents, out result);
            }

            result = default;

            // X/Y 방향으로 확장한 두 박스를 Slab 검사하고, 각 확장 방향의 바깥 면만 사용한다.
            var horizontalHalfExtents = new Vector2(coreHalfExtents.x + radius, coreHalfExtents.y);
            if (TryPointBox(origin, moveDelta, horizontalHalfExtents, out var horizontalResult) && Mathf.Abs(horizontalResult.Normal.x) > Mathf.Abs(horizontalResult.Normal.y))
            {
                result = horizontalResult;
                return true;
            }

            var verticalHalfExtents = new Vector2(coreHalfExtents.x, coreHalfExtents.y + radius);
            if (TryPointBox(origin, moveDelta, verticalHalfExtents, out var verticalResult) && Mathf.Abs(verticalResult.Normal.y) > Mathf.Abs(verticalResult.Normal.x))
            {
                result = verticalResult;
                return true;
            }

            var closestFraction = float.PositiveInfinity;
            var isHit = false;

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

                    // 부호가 반대이면 곱이 음수가 되므로 부호가 반대라는 것은 접촉점의 방향이 해당 코너의 바깥 방향과 맞지 않는다는 뜻
                    if (cornerOffset.x * xSign < -Epsilon || cornerOffset.y * ySign < -Epsilon)
                    {
                        continue;
                    }

                    // 표면을 따라 스치는 경로는 여러 모서리에 닿을 수 있으므로 최초 접촉을 선택한다.
                    if (cornerResult.Fraction < closestFraction)
                    {
                        closestFraction = cornerResult.Fraction;
                        result = cornerResult;
                    }

                    isHit = true;
                }
            }

            return isHit;
        }
    }
}
