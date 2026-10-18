using CustomAttribute.Runtime;
using CutomAttributes.Runtime;
using System;
using UnityEngine;

namespace Kinematic.Data
{
    public enum MovePriority
    {
        Low,
        Medium,
        High
    }

    public enum Shape
    {
        Circle,
        Box,
    }

    [Serializable]
    public struct ColliderInfo
    {
        // 충돌 레이어
        [SerializeField, Layer] private int layer;
        // 충돌체 타입
        [SerializeField] private Shape shape;
        // 오브젝트와 실제 충돌 중심과의 오프셋
        [SerializeField] private Vector2 offset;
        // Circle일 때 사용할 반지름
        [ShowIf(nameof(shape), (int)Shape.Circle)]
        [SerializeField] private float radius;
        // Box일 때 사용할 크기
        [ShowIf(nameof(shape), (int)Shape.Box)]
        [SerializeField] private Vector2 halfExtents;

        public int Layer => layer;
        public Shape Shape => shape;
        public Vector2 Offset => offset;
        public float Radius => radius;
        public Vector2 HalfExtents => halfExtents;

        public static ColliderInfo CreateCircle(Vector2 offset, float radius)
        {
            return new ColliderInfo
            {
                shape = Shape.Circle,
                offset = offset,
                radius = radius,
            };
        }

        public static ColliderInfo CreateBox(Vector2 offset, Vector2 halfExtents)
        {
            return new ColliderInfo
            {
                shape = Shape.Box,
                offset = offset,
                halfExtents = halfExtents,
            };
        }
    }
}

