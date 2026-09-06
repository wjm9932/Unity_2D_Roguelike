using System;
using UnityEngine;

namespace BehaviourTree.Data.Node
{
    [Serializable]
    public class InterrupterNodeData : NodeData
    {
        public override NodeType Type => NodeType.Interrupter;

        [field: Tooltip("체크 시 조건 결과를 반전합니다.")]
        [field: SerializeField] public bool IsInverted { get; private set; }

        [field: SerializeReference] public NodeData ConditionNodeData { get; private set; }
    }

    [Serializable]
    public class InverterNodeData : NodeData
    {
        public override NodeType Type => NodeType.Inverter;
    }

    [Serializable]
    public class RepeaterNodeData : NodeData
    {
        public override NodeType Type => NodeType.Repeater;

        [field: Tooltip("체크 시 Repeat Count를 무시하고 무한 반복합니다.")]
        [field: SerializeField] public bool IsInfinite { get; private set; }
        [field: SerializeField, Min(1)] public int Count { get; private set; } = 1;
    }

    [Serializable]
    public class SucceederData : NodeData
    {
        public override NodeType Type => NodeType.Succeeder;

        public override string Tooltip => "자식이 Running 상태가 아닌 경우 항상 Success를 반환합니다.";
    }
}
