using System;
using UnityEngine;
using Assets.Scripts.BehaviourTree.Data.Node;

namespace Assets.Scripts.BehaviourTree.Data
{
    [Serializable]
	public class BehaviourTreeGraphNodeData
	{
		[field: SerializeReference] public NodeData NodeData { get; set; }

		[field: HideInInspector, SerializeField] public string Id { get; set; }
		[field: HideInInspector, SerializeField] public Vector2 Position { get; set; }
	}
}
