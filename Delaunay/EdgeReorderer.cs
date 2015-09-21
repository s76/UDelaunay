using System.Collections.Generic;

namespace UDelaunay
{
    public enum SiteOrVertex
    {
        Site,
        Vertex
    }

    public class EdgeReorderer
    {
        public List<Edge> Edges { get; private set; }
        public List<Side> EdgeOrientations { get; private set; }

        public EdgeReorderer(List<Edge> edges, SiteOrVertex criterion)
        {
            Edges = new List<Edge>();
            EdgeOrientations = new List<Side>();
            if (edges.Count > 0)
            {
                Edges = ReorderEdges(edges, criterion);
            }
        }

        public void Dispose()
        {
            Edges = null;
            EdgeOrientations = null;
        }

        private List<Edge> ReorderEdges(List<Edge> edges, SiteOrVertex criterion)
        {
            var n = edges.Count;

            // we're going to reorder the edges in order of traversal
            var done = new bool[n];
            var nDone = 0;

            for (var j = 0; j < n; j++)
            {
                done[j] = false;
            }

            var newEdges = new List<Edge>();

            var i = 0;
            var edge = edges[i];
            newEdges.Add(edge);
            EdgeOrientations.Add(Side.Left);
            var firstPoint = (criterion == SiteOrVertex.Vertex) ? (ICoordinate)edge.LeftVertex : edge.LeftSite;
            var lastPoint = (criterion == SiteOrVertex.Vertex) ? (ICoordinate)edge.RightVertex :edge.RightSite;

            if (firstPoint == Vertex.VertexAtInfiniy || lastPoint == Vertex.VertexAtInfiniy)
            {
                return new List<Edge>();
            }

            done[i] = true;
            ++nDone;

            while (nDone < n)
            {
                for (i = 1; i < n; ++i)
                {
                    if (done[i])
                    {
                        continue;
                    }

                    edge = edges[i];
                    var leftPoint = (criterion == SiteOrVertex.Vertex) ? (ICoordinate)edge.LeftVertex : edge.LeftSite;
                    var rightPoint = (criterion == SiteOrVertex.Vertex) ? (ICoordinate)edge.RightVertex : edge.RightSite;

                    if (leftPoint == Vertex.VertexAtInfiniy || rightPoint == Vertex.VertexAtInfiniy)
                    {
                        return new List<Edge>();
                    }

                    if (leftPoint == lastPoint)
                    {
                        lastPoint = rightPoint;
                        EdgeOrientations.Add(Side.Left);
                        newEdges.Add(edge);
                        done[i] = true;
                    }

                    else if (rightPoint == firstPoint)
                    {
                        firstPoint = leftPoint;
                        EdgeOrientations.Insert(0, Side.Left);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    }

                    else if (leftPoint == firstPoint)
                    {
                        firstPoint = rightPoint;
                        EdgeOrientations.Insert(0, Side.Right);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    }

                    else if (rightPoint == lastPoint)
                    {
                        lastPoint = leftPoint;
                        EdgeOrientations.Add(Side.Right);
                        newEdges.Add(edge);
                        done[i] = true;
                    }

                    if (done[i])
                    {
                        ++nDone;
                    }
                }
            }

            return newEdges;
        }

    }
}
