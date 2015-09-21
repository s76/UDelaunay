using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace UDelaunay
{
    public class HalfEdge
    {
        public Edge Edge;
        public Side? LeftOrRight;
        public Vertex Vertex;

        public HalfEdge EdgeListLeftNeighbor, EdgeListRightNeighbor;
        public HalfEdge NextInPriorityQueue;

        // the vertex's y-coordinate in the transformed Voronoi space V*
        public float YStar;

        public override string ToString()
        {
            return "Halfedge (LeftRight: " + LeftOrRight + "; vertex: " + Vertex + ")";
        }

        internal bool IsLeftOf(Vector2 p)
        {
            bool above;

            var topSite = Edge.RightSite;
            var rightOfSite = p.x > topSite.X;

            if (rightOfSite && LeftOrRight == Side.Left)
            {
                return true;
            }

            if (!rightOfSite && LeftOrRight == Side.Right)
            {
                return false;
            }

            if (Edge.A == 1.0)
            {
                var dyp = p.y - topSite.Y;
                var dxp = p.x - topSite.X;
                var fast = false;

                if ((!rightOfSite && Edge.B < 0.0) || (rightOfSite && Edge.B >= 0.0))
                {
                    above = dyp >= Edge.B * dxp;
                    fast = above;
                }

                else
                {
                    above = p.x + p.y * Edge.B > Edge.C;

                    if (Edge.B < 0.0)
                    {
                        above = !above;
                    }

                    if (!above)
                    {
                        fast = true;
                    }
                }

                if (fast) return LeftOrRight == Side.Left ? above : !above;

                var dxs = topSite.X - Edge.LeftSite.X;
                above = Edge.B * (dxp * dxp - dyp * dyp) <
                        dxs * dyp * (1.0 + 2.0 * dxp / dxs + Edge.B * Edge.B);

                if (Edge.B < 0.0)
                {
                    above = !above;
                }
            }

            else
            {  
                var yl = Edge.C - Edge.A * p.x;
                var t1 = p.y - yl;
                var t2 = p.x - topSite.X;
                var t3 = yl - topSite.Y;
                above = t1 * t1 > t2 * t2 + t3 * t3;
            }

            return LeftOrRight == Side.Left ? above : !above;
        }

        #region Pool

        private static readonly Stack<HalfEdge> pool = new Stack<HalfEdge>();

        public static HalfEdge Create(Edge edge, Side? side)
        {
            return pool.Count > 0 ? pool.Pop().Init(edge, side) : new HalfEdge(edge, side);
        }

        public static HalfEdge CreateDummy()
        {
            return new HalfEdge(null, null);
        }

        public HalfEdge(Edge edge, Side? side)
        {
            Init(edge, side);
        }

        public HalfEdge Init(Edge edge, Side? side)
        {
            Edge = edge;
            LeftOrRight = side;
            NextInPriorityQueue = null;
            Vertex = null;

            return this;
        }

        public void Dispose()
        {
            if (EdgeListLeftNeighbor != null || EdgeListRightNeighbor != null)
                return;

            if (NextInPriorityQueue != null)
                return;

            Edge = null;
            LeftOrRight = null;
            Vertex = null;
            pool.Push(this);
        }

        public void ReallyDispose()
        {
            EdgeListLeftNeighbor = null;
            EdgeListRightNeighbor = null;
            NextInPriorityQueue = null;
            Edge = null;
            LeftOrRight = null;
            Vertex = null;
            pool.Push(this);
        }

        #endregion
    } 
}
