using System.Collections.Generic;
using UnityEngine;

namespace UDelaunay
{
    public class Edge
    {
        public int EdgeIndex { get; private set; }

        private Dictionary<Side, Site> sites;

        public Site LeftSite
        {
            get { return sites[Side.Left]; }
            set { sites[Side.Left] = value; }

        }
        public Site RightSite
        {
            get { return sites[Side.Right]; }
            set { sites[Side.Right] = value; }
        }

        public Vertex LeftVertex { get; private set; }
        public Vertex RightVertex { get; private set; }

        public Vertex Vertex(Side leftRight)
        {
            return (leftRight == Side.Left) ? LeftVertex : RightVertex;
        }

        public void SetVertex(Side leftRight, Vertex v)
        {
            if (leftRight == Side.Left)
            {
                LeftVertex = v;
            }
            else
            {
                RightVertex = v;
            }
        }

        public bool IsPartOfConvexHull()
        {
            return (LeftVertex == null || RightVertex == null);
        }

        public Dictionary<Side, Vector2> ClippedEnds { get; private set; }

        public float A, B, C;

        private static readonly Stack<Edge> pool = new Stack<Edge>();

        private static int nEdges;

        public static readonly Edge Deleted = new Edge();

        public static Edge CreateBisectingEdge(Site site0, Site site1)
        {
            float a, b;

            var dx = site1.X - site0.X;
            var dy = site1.Y - site0.Y;
            var absdx = dx > 0 ? dx : -dx;
            var absdy = dy > 0 ? dy : -dy;
            var c = site0.X * dx + site0.Y * dy + (dx * dx + dy * dy) * 0.5f;
            if (absdx > absdy)
            {
                a = 1.0f;
                b = dy / dx;
                c /= dx;
            }
            else
            {
                b = 1.0f;
                a = dx / dy;
                c /= dy;
            }

            var edge = Create();

            edge.LeftSite = site0;
            edge.RightSite = site1;
            site0.AddEdge(edge);
            site1.AddEdge(edge);

            edge.LeftVertex = null;
            edge.RightVertex = null;

            edge.A = a;
            edge.B = b;
            edge.C = c;

            return edge;
        }

        public static Edge Create()
        {
            Edge edge;
            if (pool.Count > 0)
            {
                edge = pool.Pop();
                edge.Init();
            }
            else
            {
                edge = new Edge();
            }
            return edge;
        }

        private Edge()
        {
            EdgeIndex = nEdges++;
            Init();
        }

        private void Init()
        {
            sites = new Dictionary<Side, Site>();
        }

        public override string ToString()
        {
            return "Edge " + EdgeIndex + "; sites " + sites[Side.Left] + ", " + sites[Side.Right]
                + "; endVertices " + ((LeftVertex != null) ? LeftVertex.VertexIndex.ToString() : "null") + ", "
                + ((RightVertex != null) ? RightVertex.VertexIndex.ToString() : "null") + "::";
        }

        public void Dispose()
        {
            LeftVertex = null;
            RightVertex = null;

            if (ClippedEnds != null)
            {
                ClippedEnds.Clear();
                ClippedEnds = null;
            }

            sites.Clear();
            sites = null;

            pool.Push(this);
        }

        public float SitesDistance()
        {
            return Vector2.Distance(LeftSite.Coordinate, RightSite.Coordinate);
        }

        public static int CompareSitesDistances_MAX(Edge edge0, Edge edge1)
        {
            var length0 = edge0.SitesDistance();
            var length1 = edge1.SitesDistance();

            if (length0 < length1)
            {
                return 1;
            }

            if (length0 > length1)
            {
                return -1;
            }

            return 0;
        }

        public static int CompareSiteDistances(Edge edge0, Edge edge1)
        {
            return -CompareSitesDistances_MAX(edge0, edge1);
        }

        public bool Visible()
        {
            return ClippedEnds != null;
        }

        public Site Site(Side leftRight)
        {
            return sites[leftRight];
        }

        public void ClipVertices(Rect bounds)
        {
            float xmin = bounds.xMin;
            float ymin = bounds.yMin;
            float xmax = bounds.xMax;
            float ymax = bounds.yMax;

            Vertex vertex0, vertex1;
            float x0, x1, y0, y1;

            if (A == 1.0 && B >= 0.0)
            {
                vertex0 = RightVertex;
                vertex1 = LeftVertex;
            }
            else
            {
                vertex0 = LeftVertex;
                vertex1 = RightVertex;
            }

            if (A == 1.0)
            {
                y0 = ymin;
                if (vertex0 != null && vertex0.Y > ymin)
                {
                    y0 = vertex0.Y;
                }
                if (y0 > ymax)
                {
                    return;
                }
                x0 = C - B * y0;

                y1 = ymax;
                if (vertex1 != null && vertex1.Y < ymax)
                {
                    y1 = vertex1.Y;
                }
                if (y1 < ymin)
                {
                    return;
                }
                x1 = C - B * y1;

                if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin))
                {
                    return;
                }

                if (x0 > xmax)
                {
                    x0 = xmax;
                    y0 = (C - x0) / B;
                }
                else if (x0 < xmin)
                {
                    x0 = xmin;
                    y0 = (C - x0) / B;
                }

                if (x1 > xmax)
                {
                    x1 = xmax;
                    y1 = (C - x1) / B;
                }
                else if (x1 < xmin)
                {
                    x1 = xmin;
                    y1 = (C - x1) / B;
                }
            }
            else
            {
                x0 = xmin;
                if (vertex0 != null && vertex0.X > xmin)
                {
                    x0 = vertex0.X;
                }
                if (x0 > xmax)
                {
                    return;
                }
                y0 = C - A * x0;

                x1 = xmax;
                if (vertex1 != null && vertex1.X < xmax)
                {
                    x1 = vertex1.X;
                }
                if (x1 < xmin)
                {
                    return;
                }
                y1 = C - A * x1;

                if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin))
                {
                    return;
                }

                if (y0 > ymax)
                {
                    y0 = ymax;
                    x0 = (C - y0) / A;
                }
                else if (y0 < ymin)
                {
                    y0 = ymin;
                    x0 = (C - y0) / A;
                }

                if (y1 > ymax)
                {
                    y1 = ymax;
                    x1 = (C - y1) / A;
                }
                else if (y1 < ymin)
                {
                    y1 = ymin;
                    x1 = (C - y1) / A;
                }
            }

            ClippedEnds = new Dictionary<Side, Vector2>();
            if (vertex0 == LeftVertex)
            {
                ClippedEnds[Side.Left] = new Vector2(x0, y0);
                ClippedEnds[Side.Right] = new Vector2(x1, y1);
            }
            else
            {
                ClippedEnds[Side.Right] = new Vector2(x0, y0);
                ClippedEnds[Side.Left] = new Vector2(x1, y1);
            }
        }

        public LineSegment DelaunayLine()
        {
            return new LineSegment(LeftSite.Coordinate, RightSite.Coordinate);
        }

        public LineSegment VoronoiEdge()
        {
            return !Visible() ? null : new LineSegment(ClippedEnds[Side.Left], ClippedEnds[Side.Right]);
        }
    }
}

