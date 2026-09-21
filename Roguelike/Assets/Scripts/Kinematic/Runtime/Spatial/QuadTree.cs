using Kinematic.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Kinematic.Runtime.Spatial
{
    internal sealed class QuadTree
    {

        private sealed class Node
        {
            internal AABB Bounds { get; }
            internal readonly List<KinematicBody> bodies = new();
            internal Node[] children;

            internal bool IsLeaf => children == null;

            internal Node(AABB bounds)
            {
                Bounds = bounds;
            }
        }

        private const int maxPerNode = 8;
        private const int maxDepth = 6;
        private Node root;

        internal QuadTree(AABB bounds)
        {
            root = new Node(bounds);
        }

        internal void Query(in AABB queryBounds, List<KinematicBody> results) => Query(root, queryBounds, results);

        private static void Query(Node node, in AABB queryBounds, List<KinematicBody> results)
        {
            // 검색 영역과 노드 영역이 겹치지 않으면 탐색하지 않는다.
            if (node.Bounds.Overlaps(queryBounds) == false) return;

            // 현재 노드에 직접 저장된 바디를 검사한다.
            foreach (var body in node.bodies)
            {
                if (body.Bounds.Overlaps(queryBounds))
                {
                    results.Add(body);
                }
            }
             
            if (node.IsLeaf) return;

            // 검색 영역과 겹칠 가능성이 있는 자식들을 탐색한다.
            foreach (var child in node.children)
            {
                Query(child, queryBounds, results);
            }
        }

        internal void Insert(KinematicBody body)
        {
            var bodyBounds = body.Bounds;

            if (root.Bounds.Contains(bodyBounds) == false)
            {
                return;
            }

            Insert(root, body, bodyBounds, 0);
        }

        private static void Insert(Node node, KinematicBody body, in AABB bodyBounds, int depth)
        {
            if (!node.IsLeaf)
            {
                var childIndex = GetContainingChildIndex(node, bodyBounds);
                // 바디가 특정 자식 영역에 완전히 포함되는 경우 해당 자식에 삽입한다.
                if (childIndex >= 0)
                {
                    Insert(node.children[childIndex], body, bodyBounds, depth + 1);
                    return;
                }
            }

            // Leaf 노드이거나 어느 자식에도 완전히 포함되지 않는 경우 현재 노드에 저장한다.
            node.bodies.Add(body);

            // 이미 분할된 노드라면, 어느 자식에도 완전히 포함되지 않는 Body는 현재 노드에 유지한다.
            if (node.IsLeaf == false) return;

            // 최대 깊이에 도달한 경우 더 이상 분할하지 않는다.
            if (depth >= maxDepth) return;

            // 아직 threshold를 넘지 않았다면 아직 분할하지 않는다.
            if (node.bodies.Count <= maxPerNode) return;

            // 임계점을 넘었을 경우 노드를 분할한다.
            Subdivide(node);
            // 현재 노드의 바디들을 새로 생성된 자식 노드에 재분배한다.
            Redistribute(node, depth);
        }

        private static int GetContainingChildIndex(Node node, in AABB bounds)
        {
            for (var childIndex = 0; childIndex < node.children.Length; childIndex++)
            {
                if (node.children[childIndex].Bounds.Contains(bounds))
                {
                    return childIndex;
                }
            }

            return -1;
        }

        private static void Subdivide(Node node)
        {
            var min = node.Bounds.Min;
            var max = node.Bounds.Max;
            var center = (min + max) * 0.5f;

            // 자식 생성
            node.children = new Node[4];
            // 1 사분면
            node.children[0] = new Node(new AABB(new Vector2(center.x, center.y), new Vector2(max.x, max.y)));
            // 2 사분면
            node.children[1] = new Node(new AABB(new Vector2(min.x, center.y), new Vector2(center.x, max.y)));
            // 3 사분면
            node.children[2] = new Node(new AABB(new Vector2(min.x, min.y), new Vector2(center.x, center.y)));
            // 4 사분면
            node.children[3] = new Node(new AABB(new Vector2(center.x, min.y), new Vector2(max.x, center.y)));
        }
        private static void Redistribute(Node node, int depth)
        {
            for (var bodyIndex = 0; bodyIndex < node.bodies.Count; bodyIndex++)
            {
                var body = node.bodies[bodyIndex];
                var bodyBounds = body.Bounds;
                // 들어갈 수 있는 영역을 탐색한다.
                var childIndex = GetContainingChildIndex(node, bodyBounds);

                // 맞는 영역이 없는 경우 분배하지 않고 유지한다.
                if (childIndex < 0) continue;

                // 포함되는 영역이 존재하는 경우 부모 노드에서 제거한 후 자식으로 이동한다.
                node.bodies.RemoveAt(bodyIndex--);
                Insert(node.children[childIndex], body, bodyBounds, depth + 1);
            }
        }

        internal bool Remove(KinematicBody body)
        {
            return Remove(root, body, body.Bounds);
        }

        // 삭제는 빈번하게 일어나지 않을거라 노드가 비어 있어도 Query 결과의 정확성에는 문제가 없기 때문에 Merge는 일단 구현 안함.
        private static bool Remove(Node node, KinematicBody body, in AABB bodyBounds)
        {
            // 자식 경계를 걸치는 바디는 현재 노드에 저장되므로 먼저 확인한다.
            if (node.bodies.Remove(body))
            {
                return true;
            }

            if (node.IsLeaf)
            {
                return false;
            }

            var childIndex = GetContainingChildIndex(node, bodyBounds);

            if (childIndex < 0)
            {
                return false;
            }

            return Remove(node.children[childIndex], body, bodyBounds);
        }
    }
}
