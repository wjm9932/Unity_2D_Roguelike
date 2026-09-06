using UnityEngine;
using Foundations;
using Pawn.Interface;
using Pawn.Controller;

namespace Pawn
{
    public class Pawn : MonoBehaviour
    {
        private IPawnController pawnController; 

        public IPawnController PawnController => pawnController;

        private void Awake()
        {
            // FIXME: 일단 메뉴얼만. 이후 분기쳐서 controller 할당
            pawnController = new ManualPawnController();
        }

        private void Start()
        {

        }

        private void Update()
        {
            this.OnUpdate(TimeManager.Instance.DeltaTime);
        }
    }

}