using System;
using UnityEngine;
using BehaviourTree.Data.Node;

namespace BehaviourTree.Data
{
    [Serializable]
	public class BehaviourTreeGraphNodeData
	{
		[field: SerializeReference] public NodeData NodeData { get; set; }

		[field: HideInInspector, SerializeField] public string Id { get; set; }
		[field: HideInInspector, SerializeField] public Vector2 Position { get; set; }
	}
}
