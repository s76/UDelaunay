using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UDelaunay
{
    public class Node
    {
        public static Stack<Node> Pool = new Stack<Node>();

        public Node Parent;
        public int TreeSize;
    }

    public enum KruskalType
    {
        Minimum,
        Maximum
    }

    public static class DelaunayHelpers
    {
        public static List<LineSegment> VisibleLineSegments(List<Edge> edges)
        {
            return (from edge in edges where edge.Visible() let p1 = edge.ClippedEnds[Side.Left] let p2 = edge.ClippedEnds[Side.Right] select new LineSegment(p1, p2)).ToList();
        }

        public static List<Edge> SelectEdgesForSitePoint(Vector2 coord, List<Edge> edgesToTest)
        {
            return edgesToTest.FindAll(edge => ((edge.LeftSite != null && edge.LeftSite.Coordinate == coord)
                                                || (edge.RightSite != null && edge.RightSite.Coordinate == coord)));
        }

        public static List<LineSegment> DelaunayLinesForEdges(List<Edge> edges)
        {
            return edges.Select(edge => edge.DelaunayLine()).ToList();
        }

        /**
		*  Kruskal's spanning tree algorithm with union-find
		 * Skiena: The Algorithm Design Manual, p. 196ff
		 * Note: the sites are implied: they consist of the end points of the line segments
		*/
        public static List<LineSegment> Kruskal(List<LineSegment> lineSegments, KruskalType type = KruskalType.Minimum)
        {
            var nodes = new Dictionary<Vector2?, Node>();
            var mst = new List<LineSegment>();
            var nodePool = Node.Pool;

            switch (type)
            {
                // note that the compare functions are the reverse of what you'd expect
                // because (see below) we traverse the lineSegments in reverse order for speed
                case KruskalType.Maximum:
                    lineSegments.Sort(LineSegment.CompareLengths);
                    break;
                default:
                    lineSegments.Sort(LineSegment.CompareLengths_MAX);
                    break;
            }

            for (var i = lineSegments.Count; --i > -1; )
            {
                var lineSegment = lineSegments[i];

                Node node0;
                Node rootOfSet0;
                if (!nodes.ContainsKey(lineSegment.P0))
                {
                    node0 = nodePool.Count > 0 ? nodePool.Pop() : new Node();
                    // intialize the node:
                    rootOfSet0 = node0.Parent = node0;
                    node0.TreeSize = 1;

                    nodes[lineSegment.P0] = node0;
                }
                else
                {
                    node0 = nodes[lineSegment.P0];
                    rootOfSet0 = Find(node0);
                }

                Node node1;
                Node rootOfSet1;
                if (!nodes.ContainsKey(lineSegment.P1))
                {
                    node1 = nodePool.Count > 0 ? nodePool.Pop() : new Node();
                    // intialize the node:
                    rootOfSet1 = node1.Parent = node1;
                    node1.TreeSize = 1;

                    nodes[lineSegment.P1] = node1;
                }
                else
                {
                    node1 = nodes[lineSegment.P1];
                    rootOfSet1 = Find(node1);
                }

                if (rootOfSet0 == rootOfSet1) continue; // nodes not in same set

                mst.Add(lineSegment);

                // merge the two sets:
                var treeSize0 = rootOfSet0.TreeSize;
                var treeSize1 = rootOfSet1.TreeSize;

                if (treeSize0 >= treeSize1)
                {
                    // set0 absorbs set1:
                    rootOfSet1.Parent = rootOfSet0;
                    rootOfSet0.TreeSize += treeSize1;
                }

                else
                {
                    // set1 absorbs set0:
                    rootOfSet0.Parent = rootOfSet1;
                    rootOfSet1.TreeSize += treeSize0;
                }
            }

            foreach (var node in nodes.Values)
            {
                nodePool.Push(node);
            }

            return mst;
        }

        private static Node Find(Node node)
        {
            if (node.Parent == node)
            {
                return node;
            }

            var root = Find(node.Parent);
            // this line is just to speed up subsequent finds by keeping the tree depth low:
            node.Parent = root;
            return root;
        }
    } 
}
