using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UDelaunay
{
    public class Site : ICoordinate, IComparable
    {
        public Vector2 Coordinate { get; set; }
        public float X { get { return Coordinate.x; } }
        public float Y { get { return Coordinate.y; } }

        public int SiteIndex { get; set; }

        public float Weigth { get; private set; }

        public List<Edge> Edges { get; private set; }
        public List<Side> EdgeOrientations { get; private set; }

        private List<Vector2> region; 

        private static readonly Stack<Site> pool = new Stack<Site>();

        public static Site Create(Vector2 p, int index, float weight)
        {
            return pool.Count > 0 ? pool.Pop().Init(p, index, weight) : new Site(p, index, weight);
        }

        public static void SortSites(List<Site> sites)
        {
            sites.Sort();
        }

        public int CompareTo(Site s1)
        {
            var returnValue = Voronoi.CompareByYThenX(this, s1);

            int tempIndex;

            switch (returnValue)
            {
                case -1:
                    if (SiteIndex > s1.SiteIndex)
                    {
                        tempIndex = SiteIndex;
                        SiteIndex = s1.SiteIndex;
                        s1.SiteIndex = tempIndex;
                    }
                    break;
                case 1:
                    if (s1.SiteIndex > SiteIndex)
                    {
                        tempIndex = s1.SiteIndex;
                        s1.SiteIndex = SiteIndex;
                        SiteIndex = tempIndex;
                    }
                    break;
            }

            return returnValue;
        }

        private const float Epsilon = 0.005f;

        public static bool CloseEnough(Vector2 p0, Vector2 p1)
        {
            return Vector2.Distance(p0, p1) < Epsilon;
        }

        private Site(Vector2 p, int index, float weight)
        {
            Init(p, index, weight);
        }

        private Site Init(Vector2 p, int index, float weight)
        {
            Coordinate = p;
            SiteIndex = index;
            Weigth = weight;
            Edges = new List<Edge>();
            region = null;
            return this;
        }

        public override string ToString()
        {
            return "Site " + SiteIndex + ": " + Coordinate;
        }

        public int CompareTo(object obj)
        {
            var s2 = (Site)obj;

            var returnValue = Voronoi.CompareByYThenX(this, s2);

            // swap _siteIndex values if necessary to match new ordering:
            int tempIndex;
            switch (returnValue)
            {
                case -1:
                    if (SiteIndex > s2.SiteIndex)
                    {
                        tempIndex = SiteIndex;
                        SiteIndex = s2.SiteIndex;
                        s2.SiteIndex = tempIndex;
                    }
                    break;
                case 1:
                    if (s2.SiteIndex > SiteIndex)
                    {
                        tempIndex = s2.SiteIndex;
                        s2.SiteIndex = SiteIndex;
                        SiteIndex = tempIndex;
                    }
                    break;
            }

            return returnValue;
        }

        private void Move(Vector2 p)
        {
            Clear();
            Coordinate = p;
        }

        public void Dispose()
        {
            Clear();
            pool.Push(this);
        }

        private void Clear()
        {
            if (Edges != null)
            {
                Edges.Clear();
                Edges = null;
            }

            if (EdgeOrientations != null)
            {
                EdgeOrientations.Clear();
                EdgeOrientations = null;
            }

            if (region == null) return;

            region.Clear();
            region = null;
        }

        public void AddEdge(Edge edge)
        {
            Edges.Add(edge);
        }

        public Edge NearestEdge()
        {
            Edges.Sort(Edge.CompareSiteDistances);
            return Edges[0];
        }

        public List<Site> NeighborSites()
        {
            if (Edges == null || Edges.Count == 0)
                return new List<Site>();

            if (EdgeOrientations == null)
            {
                ReorderEdges();
            }

            //for some reason unity won't accept just the method group
            return Edges.Select(edge => NeighborSite(edge)).ToList();
        }

        public Site NeighborSite(Edge edge)
        {
            return this == edge.LeftSite ? edge.RightSite : this == edge.RightSite ? edge.LeftSite : null;
        }

        public List<Vector2> Region(Rect clippingBounds)
        {
            if (Edges == null || Edges.Count == 0)
            {
                return new List<Vector2>();
            }

            if (EdgeOrientations != null) return region;

            ReorderEdges();
            region = ClipToBounds(clippingBounds);

            if ((new Polygon(region)).PolyWinding() == Winding.Clockwise)
            {
                region.Reverse();
            }
            return region;
        }

        private void ReorderEdges()
        {
            var edgeReorderer = new EdgeReorderer(Edges, SiteOrVertex.Vertex);
            Edges = edgeReorderer.Edges;
            EdgeOrientations = edgeReorderer.EdgeOrientations;
            edgeReorderer.Dispose();
        }

        private List<Vector2> ClipToBounds(Rect bounds)
        {
            var points = new List<Vector2>();
            var n = Edges.Count;
            var i = 0;

            while (i < n && !Edges[i].Visible())
            {
                i++;
            }

            if (i == n)
                return new List<Vector2>();

            var edge = Edges[i];
            var orientation = EdgeOrientations[i];

            points.Add(edge.ClippedEnds[orientation]);
            points.Add(edge.ClippedEnds[SideHelper.Other(orientation)]);

            for (var j = i + 1; j < n; j++)
            {
                edge = Edges[j];
                if (!edge.Visible())
                    continue;

                Connect(points, j, bounds);
            }

            Connect(points, i, bounds, true);

            return points;
        }

        private void Connect(List<Vector2> points, int j, Rect bounds, bool closingUp = false)
        {
            var rightPoint = points[points.Count - 1];
            var newEdge = Edges[j];
            var newOrientation = EdgeOrientations[j];

            var newPoint = newEdge.ClippedEnds[newOrientation];

            if (!CloseEnough(rightPoint, newPoint))
            {
                //todo: be sure this is ok to do but it seems more logical considering the last line
                if (Math.Abs(rightPoint.x - newPoint.x) > Epsilon
					&& Math.Abs(rightPoint.y - newPoint.y) > Epsilon) {

                    var rightCheck = BoundsCheck.Check(rightPoint, bounds);
                    var newCheck = BoundsCheck.Check(newPoint, bounds);

					float px, py;
					if ((rightCheck & BoundsCheck.Right) != 0) 
                    {
						px = bounds.xMax;

						if ((newCheck & BoundsCheck.Bottom) != 0) 
                        {
							py = bounds.yMax;
							points.Add (new Vector2 (px, py));
						} 
                        
                        else if ((newCheck & BoundsCheck.Top) != 0) 
                        {
							py = bounds.yMin;
							points.Add (new Vector2 (px, py));
						} 
                        
                        else if ((newCheck & BoundsCheck.Left) != 0) 
                        {
							py = rightPoint.y - bounds.y + newPoint.y - bounds.y < bounds.height ? bounds.yMin : bounds.yMax;
							points.Add (new Vector2 (px, py));
							points.Add (new Vector2 (bounds.xMin, py));
						}
					} 
                    
                    else if ((rightCheck & BoundsCheck.Left) != 0) 
                    {
						px = bounds.xMin;

						if ((newCheck & BoundsCheck.Bottom) != 0) 
                        {
							py = bounds.yMax;
							points.Add (new Vector2 (px, py));
						} 
                        
                        else if ((newCheck & BoundsCheck.Top) != 0) 
                        {
							py = bounds.yMin;
							points.Add (new Vector2 (px, py));
						}
                        
                        else if ((newCheck & BoundsCheck.Right) != 0) 
                        {
							py = rightPoint.y - bounds.y + newPoint.y - bounds.y < bounds.height ? bounds.yMin : bounds.yMax;
							points.Add (new Vector2 (px, py));
							points.Add (new Vector2 (bounds.xMax, py));
						}
					} 
                    
                    else if ((rightCheck & BoundsCheck.Top) != 0) 
                    {
						py = bounds.yMin;
						if ((newCheck & BoundsCheck.Right) != 0) 
                        {
							px = bounds.xMax;
							points.Add (new Vector2 (px, py));
						} 
                        
                        else if ((newCheck & BoundsCheck.Left) != 0) 
                        {
							px = bounds.xMin;
							points.Add (new Vector2 (px, py));
						} 
                        
                        else if ((newCheck & BoundsCheck.Bottom) != 0) 
                        {
							px = rightPoint.x - bounds.x + newPoint.x - bounds.x < bounds.width ? bounds.xMin : bounds.xMax;
							points.Add (new Vector2 (px, py));
							points.Add (new Vector2 (px, bounds.yMax));
						}
					} 
                    
                    else if ((rightCheck & BoundsCheck.Bottom) != 0) 
                    {
						py = bounds.yMax;
						if ((newCheck & BoundsCheck.Right) != 0) {
							px = bounds.xMax;
							points.Add (new Vector2 (px, py));
						} 
                        
                        else if ((newCheck & BoundsCheck.Left) != 0) 
                        {
							px = bounds.xMin;
							points.Add (new Vector2 (px, py));
						} 
                        
                        else if ((newCheck & BoundsCheck.Top) != 0) 
                        {
							px = rightPoint.x - bounds.x + newPoint.x - bounds.x < bounds.width ? bounds.xMin : bounds.xMax;
							points.Add (new Vector2 (px, py));
							points.Add (new Vector2 (px, bounds.yMin));
						}
					}
				}

				if (closingUp) {
					// newEdge's ends have already been added
					return;
				}
				points.Add (newPoint);
			}

			var newRightPoint = newEdge.ClippedEnds[SideHelper.Other(newOrientation)];

			if (!CloseEnough (points [0], newRightPoint)) 
				points.Add (newRightPoint);	          
        }

        public float Distance(ICoordinate p)
        {
            return Vector2.Distance(p.Coordinate, Coordinate);
        }

    }

    public static class BoundsCheck
    {
        public static readonly int Top = 1;
        public static readonly int Bottom = 2;
        public static readonly int Left = 4;
        public static readonly int Right = 8;

        /**
             * 
             * @param point
             * @param bounds
             * @return an int with the appropriate bits set if the Point lies on the corresponding bounds lines
             * 
             */
        public static int Check(Vector2 point, Rect bounds)
        {
            var value = 0;

            if (point.x == bounds.xMin)
            {
                value |= Left;
            }

            if (point.x == bounds.xMax)
            {
                value |= Right;
            }

            if (point.y == bounds.yMin)
            {
                value |= Top;
            }

            if (point.y == bounds.yMax)
            {
                value |= Bottom;
            }

            return value;
        }
    }
}
