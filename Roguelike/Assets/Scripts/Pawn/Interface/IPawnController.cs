using UnityEngine;

namespace Pawn.Interface
{
    public interface IPawnController
    {
        // AI Controller에선 bt update만 수행할텐데 이게 필요할까
        public bool IsStickNeutral { get; }
        public Vector2 LastStickDirection { get; }

        public void OnUpdate(float dt);
    }
}