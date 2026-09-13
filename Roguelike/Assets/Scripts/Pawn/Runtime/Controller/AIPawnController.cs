using Pawn.Interface;
using UnityEngine;

namespace Pawn.Runtime.Controller
{
    public class AIPawnController : IPawnController
    {
        // 이게 필요하려나...
        public bool IsStickNeutral => throw new System.NotImplementedException();

        public Vector2 LastStickDirection => throw new System.NotImplementedException();

        // 여기서 BT 업데이트
        // 근데 그럼 BT 데이터는 어디서 주입시켜주지...
        public void OnUpdate(float dt)
        {
            throw new System.NotImplementedException();
        }
    }
}
