using Kinematic.Data;
using UnityEngine;

namespace Kinematic.Runtime
{
    public sealed class KinematicBody
    {
        private readonly Transform target;
        internal bool IsStatic { get; }
        public ColliderInfo Shape { get; }
        public Vector2 Center => anchorPosition + Shape.Offset;
        private Vector2 anchorPosition;

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
