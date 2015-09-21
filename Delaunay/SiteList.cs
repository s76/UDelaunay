using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UDelaunay
{
    public class SiteList
    {
        private List<Site> sites;
        private int currentIndex;

        private bool sorted;

        public int Count { get { return sites.Count; } }

        public SiteList()
        {
            sites = new List<Site>();
            sorted = false;
        }

        public void Dispose()
        {
            if (sites == null) return;

            foreach (var site in sites)
            {
                site.Dispose();
            }

            sites.Clear();
            sites = null;
        }

        public int Add(Site site)
        {
            sorted = false;
            sites.Add(site);
            return sites.Count;
        }

        public Site Next()
        {
            if (!sorted)
                Debug.LogError("SiteList::next():  sites have not been sorted");

            return currentIndex < sites.Count ? sites[currentIndex++] : null;
        }

        public Rect GetSiteBounds()
        {
            if (sorted == false)
            {
                Site.SortSites(sites);
                currentIndex = 0;
                sorted = true;
            }

            if (sites.Count == 0)
            {
                return new Rect(0, 0, 0, 0);
            }

            var xmin = float.MaxValue;
            var xmax = float.MinValue;

            foreach (var site in sites)
            {
                if (site.X < xmin)
                {
                    xmin = site.X;
                }
                if (site.X > xmax)
                {
                    xmax = site.X;
                }
            }
            // here's where we assume that the sites have been sorted on y:
            var ymin = sites[0].Y;
            var ymax = sites[sites.Count - 1].Y;

            return new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
        }

        public List<Vector2> SiteCoordinates()
        {
            return sites.Select(site => site.Coordinate).ToList();
        }

        public List<Circle> Circles()
        {
            var circles = new List<Circle>();

            foreach (var site in sites)
            {
                var radius = 0f;
                var nearestEdge = site.NearestEdge();

                if (!nearestEdge.IsPartOfConvexHull())
                {
                    radius = nearestEdge.SitesDistance() * 0.5f;
                }

                circles.Add(new Circle(site.X, site.Y, radius));
            }

            return circles;
        }

        public List<List<Vector2>> Regions(Rect plotBounds)
        {
            return sites.Select(site => site.Region(plotBounds)).ToList();
        }

        //todo: might not be needed

        public void ResetListIndex()
        {
            currentIndex = 0;
        }

        public void SortList()
        {
            Site.SortSites(sites);
            sorted = true;
        }
    } 
}
