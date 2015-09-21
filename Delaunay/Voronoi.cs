using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UDelaunay
{
    public class Voronoi
    {
        private SiteList sites;
        private Dictionary<Vector2, Site> sitesIndexedByLocation;

        public List<Edge> Edges { get; private set; }

        public Rect Bounds { get; private set; }

        public Voronoi(List<Vector2> points, Rect plotBounds)
        {
            Init(points, plotBounds);
        }

        public Voronoi(List<Vector2> points, Rect plotBounds, int lloydRelaxationIterations)
        {
            Init(points, plotBounds);
            LloydRelaxation(lloydRelaxationIterations);
        }

        public void Init(List<Vector2> points, Rect plotBounds)
        {
            sites = new SiteList();
            sitesIndexedByLocation = new Dictionary<Vector2, Site>();
            AddSites(points);
            Bounds = plotBounds;
            Edges = new List<Edge>();
            FortunesAlgorithm();
        }

        public void Dispose()
        {
            int i, n;
            if (sites != null)
            {
                sites.Dispose();
                sites = null;
            }

            if (Edges != null)
            {
                n = Edges.Count;
                for (i = 0; i < n; ++i)
                {
                    Edges[i].Dispose();
                }
                Edges.Clear();
                Edges = null;
            }

            sitesIndexedByLocation = null;
        }

        public void AddSites(List<Vector2> points )
        {
            for (var i = 0; i < points.Count; ++i)
            {
                AddSite(points[i], i);
            }
        }

        public void AddSite(Vector2 p, int index)
        {
            if (sitesIndexedByLocation.ContainsKey(p))
            {
                Debug.LogWarning("UDelaunay.Voronoi Warning - duplicate site detected, skipping");
            return;
            }

            var weight = Random.value * 100f;
            var site = Site.Create(p, index, weight);
            sites.Add(site);
            sitesIndexedByLocation[p] = site;
        }

        public List<Vector2> Region(Vector2 p)
        {
            Site site;
            return sitesIndexedByLocation.TryGetValue(p, out site) ? site.Region(Bounds) : new List<Vector2>();
        }

        public List<Vector2> NeighborSitesForSite(Vector2 p)
        {
            var points = new List<Vector2>();
            Site site;
            if (!sitesIndexedByLocation.TryGetValue(p, out site)) return points;

            var neighborSites = site.NeighborSites();
            points.AddRange(neighborSites.Select(neighbor => neighbor.Coordinate));

            return points;
        }

        public List<Circle> Circles()
        {
            return sites.Circles();
        }

        public List<LineSegment> VoronoiBoundaryForSite(Vector2 p)
        {
            return DelaunayHelpers.VisibleLineSegments(DelaunayHelpers.SelectEdgesForSitePoint(p, Edges));
        }

        public List<LineSegment> DelaunayLinesForSite(Vector2 p)
        {
            return DelaunayHelpers.DelaunayLinesForEdges(DelaunayHelpers.SelectEdgesForSitePoint(p, Edges));
        }

        public List<LineSegment> VoronoiDiagram()
        {
            return DelaunayHelpers.VisibleLineSegments(Edges);
        }

        public List<LineSegment> DelaunayTriangulation()
        {
            return DelaunayHelpers.DelaunayLinesForEdges(Edges);
        }

        public List<LineSegment> Hull()
        {
            return DelaunayHelpers.DelaunayLinesForEdges(HullEdges());
        }

        public List<Edge> HullEdges()
        {
            return Edges.FindAll(edge => (edge.IsPartOfConvexHull()));
        }

        public List<Vector2> HullPointsInOrder()
        {
            var hullEdges = HullEdges();

            var points = new List<Vector2>();

            if (hullEdges.Count == 0)
                return points;

            var edgeReorderer = new EdgeReorderer(hullEdges, SiteOrVertex.Site);

            hullEdges = edgeReorderer.Edges;
            var orientations = edgeReorderer.EdgeOrientations;
            edgeReorderer.Dispose();

            var n = hullEdges.Count;

            for (var i = 0; i < n; ++i)
            {
                var edge = hullEdges[i];
                var orientation = orientations[i];
                points.Add(edge.Site(orientation).Coordinate);
            }

            return points;
        }

        public List<LineSegment> SpanningTree(KruskalType type = KruskalType.Minimum)
        {
            return DelaunayHelpers.Kruskal(DelaunayHelpers.DelaunayLinesForEdges(Edges), type);
        }

        public List<List<Vector2>> Regions()
        {
            return sites.Regions(Bounds);
        }

        public List<Vector2> SiteCoordinates()
        {
            return sites.SiteCoordinates();
        }

        private void FortunesAlgorithm()
        {
            var newIntStar = Vector2.zero;

            var dataBounds = sites.GetSiteBounds();

            var sqrtSiteCount = (int)(Mathf.Sqrt(sites.Count + 4));

            var heap = new HalfEdgePriorityQueue(dataBounds.y, dataBounds.height, sqrtSiteCount);

            var edgeList = new EdgeList(dataBounds.x, dataBounds.width, sqrtSiteCount);

            var halfEdges = new List<HalfEdge>();

            var vertices = new List<Vertex>();

            var bottomMostSite = sites.Next();

            var newSite = sites.Next();

            for (;;)
            {
                if (heap.Empty() == false)
                {
                    newIntStar = heap.Min();
                }

                Site bottomSite;
                Vertex vertex;
                HalfEdge lbnd;
                HalfEdge rbnd;
                HalfEdge bisector;
                Edge edge;
                
                if (newSite != null
                    && (heap.Empty() || CompareByYThenX(newSite, newIntStar) < 0))
                {
                    /* new site is smallest */
                    //trace("smallest: new site " + newSite);

                    // Step 8:
                    lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coordinate);	// the Halfedge just to the left of newSite
                    //trace("lbnd: " + lbnd);
                    rbnd = lbnd.EdgeListRightNeighbor;		// the Halfedge just to the right
                    //trace("rbnd: " + rbnd);
                    bottomSite = RightRegion(lbnd, bottomMostSite);		// this is the same as leftRegion(rbnd)
                    // this Site determines the region containing the new site
                    //trace("new Site is in region of existing site: " + bottomSite);

                    // Step 9:
                    edge = Edge.CreateBisectingEdge(bottomSite, newSite);
                    //trace("new edge: " + edge);
                    Edges.Add(edge);

                    bisector = HalfEdge.Create(edge, Side.Left);
                    halfEdges.Add(bisector);
                    // inserting two Halfedges into edgeList constitutes Step 10:
                    // insert bisector to the right of lbnd:
                    edgeList.Insert(lbnd, bisector);

                    // first half of Step 11:
                    if ((vertex = Vertex.Intersect(lbnd, bisector)) != null)
                    {
                        vertices.Add(vertex);
                        heap.Remove(lbnd);
                        lbnd.Vertex = vertex;
                        lbnd.YStar = vertex.Y + newSite.Distance(vertex);
                        heap.Insert(lbnd);
                    }

                    lbnd = bisector;
                    bisector = HalfEdge.Create(edge, Side.Right);
                    halfEdges.Add(bisector);
                    // second Halfedge for Step 10:
                    // insert bisector to the right of lbnd:
                    edgeList.Insert(lbnd, bisector);

                    // second half of Step 11:
                    if ((vertex = Vertex.Intersect(bisector, rbnd)) != null)
                    {
                        vertices.Add(vertex);
                        bisector.Vertex = vertex;
                        bisector.YStar = vertex.Y + newSite.Distance(vertex);
                        heap.Insert(bisector);
                    }

                    newSite = sites.Next();
                }
                else if (heap.Empty() == false)
                {
                    /* intersection is smallest */
                    lbnd = heap.ExtractMin();
                    var llbnd = lbnd.EdgeListLeftNeighbor;
                    rbnd = lbnd.EdgeListRightNeighbor;
                    var rrbnd = rbnd.EdgeListRightNeighbor;
                    bottomSite = LeftRegion(lbnd, bottomMostSite);
                    var topSite = RightRegion(rbnd, bottomMostSite);
                    // these three sites define a Delaunay triangle
                    // (not actually using these for anything...)
                    //_triangles.push(new Triangle(bottomSite, topSite, rightRegion(lbnd)));

                    var v = lbnd.Vertex;
                    v.SetIndex();
                    lbnd.Edge.SetVertex((Side)lbnd.LeftOrRight, v);
                    rbnd.Edge.SetVertex((Side)rbnd.LeftOrRight, v);
                    edgeList.Remove(lbnd);
                    heap.Remove(rbnd);
                    edgeList.Remove(rbnd);
                    var leftRight = Side.Left;
                    if (bottomSite.Y > topSite.Y)
                    {
                        var tempSite = bottomSite;
                        bottomSite = topSite;
                        topSite = tempSite;
                        leftRight = Side.Right;
                    }
                    edge = Edge.CreateBisectingEdge(bottomSite, topSite);
                    Edges.Add(edge);
                    bisector = HalfEdge.Create(edge, leftRight);
                    halfEdges.Add(bisector);
                    edgeList.Insert(llbnd, bisector);
                    edge.SetVertex(SideHelper.Other(leftRight), v);
                    if ((vertex = Vertex.Intersect(llbnd, bisector)) != null)
                    {
                        vertices.Add(vertex);
                        heap.Remove(llbnd);
                        llbnd.Vertex = vertex;
                        llbnd.YStar = vertex.Y + bottomSite.Distance(vertex);
                        heap.Insert(llbnd);
                    }
                    if ((vertex = Vertex.Intersect(bisector, rrbnd)) != null)
                    {
                        vertices.Add(vertex);
                        bisector.Vertex = vertex;
                        bisector.YStar = vertex.Y + bottomSite.Distance(vertex);
                        heap.Insert(bisector);
                    }
                }
                else
                {
                    break;
                }
            }

            // heap should be empty now
            heap.Dispose();
            edgeList.Dispose();

            foreach (var halfEdge in halfEdges)
            {
                halfEdge.ReallyDispose();
            }
            halfEdges.Clear();

            // we need the vertices to clip the edges
            foreach (var e in Edges)
            {
                e.ClipVertices(Bounds);
            }
            // but we don't actually ever use them again!
            foreach (var v in vertices)
            {
                v.Dispose();
            }
            vertices.Clear();
        }

        private static Site LeftRegion(HalfEdge halfEdge, Site bottomMostSite)
        {
            var edge = halfEdge.Edge;
            return edge == null ? bottomMostSite : edge.Site((Side)halfEdge.LeftOrRight);
        }

        private static Site RightRegion(HalfEdge halfEdge, Site bottomMostSite)
        {
            var edge = halfEdge.Edge;
            return edge == null ? bottomMostSite : edge.Site(SideHelper.Other((Side)halfEdge.LeftOrRight));
        }

        public static int CompareByYThenX(Site s1, Site s2)
        {
            if (s1.Y < s2.Y) return -1;
            if (s1.Y > s2.Y) return 1;
            if (s1.X < s2.X) return -1;
            if (s1.X > s2.X) return 1;
            return 0;
        }

        public static int CompareByYThenX(Site s1, Vector2 s2)
        {
            if (s1.Y < s2.y) return -1;
            if (s1.Y > s2.y) return 1;
            if (s1.X < s2.x) return -1;
            if (s1.X > s2.x) return 1;
            return 0;
        }

        public void LloydRelaxation(int interations)
        {
            // Reapeat the whole process for the number of iterations asked
            for (var i = 0; i < interations; i++)
            {
                var newPoints = new List<Vector2>();
                // Go thourgh all sites
                sites.ResetListIndex();
                var site = sites.Next();

                while (site != null)
                {
                    // Loop all corners of the site to calculate the centroid
                    var region = site.Region(Bounds);
                    var centroid = Vector2.zero;
                    float signedArea = 0;
                    float x0;
                    float y0;
                    float x1;
                    float y1;
                    float a;
                    // For all vertices except last
                    for (var j = 0; j < region.Count - 1; j++)
                    {
                        x0 = region[j].x;
                        y0 = region[j].y;
                        x1 = region[j + 1].x;
                        y1 = region[j + 1].y;
                        a = x0 * y1 - x1 * y0;
                        signedArea += a;
                        centroid.x += (x0 + x1) * a;
                        centroid.y += (y0 + y1) * a;
                    }
                    // Do last vertex
                    x0 = region[region.Count - 1].x;
                    y0 = region[region.Count - 1].y;
                    x1 = region[0].x;
                    y1 = region[0].y;
                    a = x0 * y1 - x1 * y0;
                    signedArea += a;
                    centroid.x += (x0 + x1) * a;
                    centroid.y += (y0 + y1) * a;

                    signedArea *= 0.5f;
                    centroid.x /= (6 * signedArea);
                    centroid.y /= (6 * signedArea);
                    // Move site to the centroid of its Voronoi cell
                    newPoints.Add(centroid);
                    site = sites.Next();
                }

                // Between each replacement of the cendroid of the cell,
                // we need to recompute Voronoi diagram:
                var origPlotBounds = Bounds;
                Dispose();
                Init(newPoints, origPlotBounds);
            }
        }
    }
}
