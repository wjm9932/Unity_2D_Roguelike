using Pawn.Interface;

namespace Pawn.Controller
{
    public class AIPawnController : IPawnController
    {
        // 여기서 BT 업데이트
        // 근데 그럼 BT 데이터는 어디서 주입시켜주지...
        public void ThrowUpdate(float dt)
        {
            throw new System.NotImplementedException();
        }
    }
}
