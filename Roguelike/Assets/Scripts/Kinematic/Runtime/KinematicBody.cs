using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    public sealed class KinematicBody
    {
        private readonly Transform target;
        private Vector2 anchorPosition;

        public ColliderInfo Shape { get; }
        public Vector2 Center => anchorPosition + Shape.Offset;

        internal bool IsStatic { get; }
        internal AABB Bounds
        {
            get
            {
                var extents = Shape.Shape switch
                {
                    Data.Shape.Circle => Vector2.one * Shape.Radius,
                    Data.Shape.Box => Shape.HalfExtents,
                    _ => Vector2.zero
                };

                return AABB.Create(Center, extents);
            }
        }

        public KinematicBody(Transform transform, ColliderInfo shape, bool isStatic)
        {
            target = transform;
            anchorPosition = transform.position;
            Shape = shape;
            IsStatic = isStatic;
        }

        public void Move(Vector2 delta)
        {
            Debug.Assert(!IsStatic, $"Request Move to static body: {target.name}");
            anchorPosition += delta;
        }

        public void Teleport(Vector2 position)
        {
            Debug.Assert(!IsStatic, $"Request Teleport to static body: {target.name}");
            anchorPosition = position;
        }

        internal void ApplyCorrection(Vector2 mtv) => anchorPosition += mtv;

        internal void SyncTransform() => target.position = anchorPosition;
    }
}
