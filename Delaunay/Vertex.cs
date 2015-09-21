using System.Collections.Generic;
using UnityEngine;

namespace UDelaunay
{
    public class Vertex : ICoordinate
    {
        public Vector2 Coordinate { get; set; }

        public float X { get { return Coordinate.x; } }
        public float Y { get { return Coordinate.y; } }

        public int VertexIndex { get; private set; }

        private static readonly Stack<Vertex> pool = new Stack<Vertex>();
        private static int nVertices;

        public static readonly Vertex VertexAtInfiniy= new Vertex(float.NaN, float.NaN);

        private static Vertex Create(float x, float y)
        {
            if (float.IsNaN(x) || float.IsNaN(y))
                return VertexAtInfiniy;

            return pool.Count > 0 ? pool.Pop().Init(x, y) : new Vertex(x, y);
        }

        public Vertex(float x, float y)
        {
            Init(x, y);
        }

        public Vertex Init(float x, float y)
        {
            Coordinate = new Vector2(x, y);
            return this;
        }

        public void Dispose()
        {
            pool.Push(this);
        }

        public void SetIndex()
        {
            VertexIndex = nVertices++;
        }

        public override string ToString()
        {
            return "Vertex (" + VertexIndex + ")";
        }

        public static Vertex Intersect(HalfEdge halfEdge0, HalfEdge halfEdge1)
        {
            Edge edge;
            HalfEdge halfEdge;

            var edge0 = halfEdge0.Edge;
            var edge1 = halfEdge1.Edge;

            if (edge0 == null || edge1 == null)
            {
                return null;
            }

            if (edge0.RightSite == edge1.RightSite)
            {
                return null;
            }

            var determinant = edge0.A * edge1.B - edge0.B * edge1.A;

            if (-1.0e-10 < determinant && determinant < 1.0e-10)
            {
                // the edges are parallel
                return null;
            }

            var intersectionX = (edge0.C * edge1.B - edge1.C * edge0.B) / determinant;
            var intersectionY = (edge1.C * edge0.A - edge0.C * edge1.A) / determinant;

            if (Voronoi.CompareByYThenX(edge0.RightSite, edge1.RightSite) < 0)
            {
                halfEdge = halfEdge0;
                edge = edge0;
            }

            else
            {
                halfEdge = halfEdge1;
                edge = edge1;
            }

            var rightOfSite = intersectionX >= edge.RightSite.X;

            if ((rightOfSite && halfEdge.LeftOrRight == Side.Left) || (!rightOfSite && halfEdge.LeftOrRight == Side.Right))
                return null;


            return Create(intersectionX, intersectionY);
        }

    } 
}
