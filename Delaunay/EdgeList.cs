using UnityEngine;

namespace UDelaunay
{
    public class EdgeList
    {
        private readonly float deltaX;
        private readonly float xMin;

        private readonly int hashSize;
        private HalfEdge[] hash;

        public HalfEdge LeftEnd { get; private set; }
        public HalfEdge RightEnd { get; private set; }

        public void Dispose()
        {
            var halfEdge = LeftEnd;

            while (halfEdge != RightEnd)
            {
                var prevHalfEdge = halfEdge;
                halfEdge = halfEdge.EdgeListRightNeighbor;
                prevHalfEdge.Dispose();
            }

            LeftEnd = null;
            RightEnd.Dispose();
            RightEnd = null;

            int i;
            //todo: might not be needed
            for (i = 0; i < hashSize; ++i)
            {
                hash[i] = null;
            }

            hash = null;
        }

        public EdgeList(float xMin, float deltaX, int sqrtSiteCount)
        {
            this.xMin = xMin;
            this.deltaX = deltaX;
            hashSize = 2*sqrtSiteCount;

            hash = new HalfEdge[hashSize];

            LeftEnd = HalfEdge.CreateDummy();
            RightEnd = HalfEdge.CreateDummy();
            LeftEnd.EdgeListLeftNeighbor = null;
            LeftEnd.EdgeListRightNeighbor = RightEnd;

            RightEnd.EdgeListRightNeighbor = null;
            RightEnd.EdgeListLeftNeighbor = LeftEnd;

            hash[0] = LeftEnd;
            hash[hashSize - 1] = RightEnd;
        }

        public void Insert(HalfEdge lb, HalfEdge newHalfEdge)
        {
            newHalfEdge.EdgeListLeftNeighbor = lb;
            newHalfEdge.EdgeListRightNeighbor = lb.EdgeListRightNeighbor;

            lb.EdgeListRightNeighbor.EdgeListLeftNeighbor = newHalfEdge;
            lb.EdgeListRightNeighbor = newHalfEdge;
        }

        public void Remove(HalfEdge halfEdge)
        {
            halfEdge.EdgeListLeftNeighbor.EdgeListRightNeighbor = halfEdge.EdgeListRightNeighbor;
            halfEdge.EdgeListRightNeighbor.EdgeListLeftNeighbor = halfEdge.EdgeListLeftNeighbor;
            halfEdge.Edge = Edge.Deleted;
            halfEdge.EdgeListLeftNeighbor = halfEdge.EdgeListRightNeighbor = null;
        }

        public HalfEdge EdgeListLeftNeighbor(Vector2 p)
        {
            /* Use hash table to get close to desired halfedge */
            var bucket = (int)((p.x - xMin) / deltaX * hashSize);

            if (bucket < 0)
            {
                bucket = 0;
            }

            if (bucket >= hashSize)
            {
                bucket = hashSize - 1;
            }

            var halfEdge = GetHash(bucket);

            if (halfEdge == null)
            {
                int i;

                for (i = 1;; ++i)
                {
                    if ((halfEdge = GetHash(bucket - i)) != null)
                        break;
                    if ((halfEdge = GetHash(bucket + i)) != null)
                        break;
                }
            }
            /* Now search linear list of halfedges for the correct one */
            if (halfEdge == LeftEnd || (halfEdge != RightEnd && halfEdge.IsLeftOf(p)))
            {
                do
                {
                    halfEdge = halfEdge.EdgeListRightNeighbor;
                } while (halfEdge != RightEnd && halfEdge.IsLeftOf(p));
                halfEdge = halfEdge.EdgeListLeftNeighbor;
            }

            else
            {
                do
                {
                    halfEdge = halfEdge.EdgeListLeftNeighbor;
                } while (halfEdge != LeftEnd && !halfEdge.IsLeftOf(p));
            }

            /* Update hash table and reference counts */
            if (bucket > 0 && bucket < hashSize - 1)
            {
                hash[bucket] = halfEdge;
            }
            return halfEdge;
        }

        private HalfEdge GetHash(int b)
        {
            if (b < 0 || b >= hashSize)
            {
                return null;
            }

            var halfEdge = hash[b];

            if (halfEdge == null || halfEdge.Edge != Edge.Deleted) return halfEdge;

            /* Hash table points to deleted halfedge.  Patch as necessary. */
            hash[b] = null;
            // still can't dispose halfEdge yet!
            return null;
        }

    } 
}
