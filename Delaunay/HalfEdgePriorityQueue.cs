using UnityEngine;
using System.Collections;

namespace UDelaunay
{
    public class HalfEdgePriorityQueue
    {
        private HalfEdge[] hash;
        private int count;
        private int minBucket;
        private readonly int hashSize;

        private readonly float yMin;
        private readonly float deltaY;

        public HalfEdgePriorityQueue(float yMin, float deltaY, int sqrtSiteCount)
        {
            this.yMin = yMin;
            this.deltaY = deltaY;
            hashSize = 4 * sqrtSiteCount;
            Init();
        }

        public void Dispose()
        {
            // get rid of dummies
            for (var i = 0; i < hashSize; ++i)
            {
                hash[i].Dispose();
                hash[i] = null;
            }
            hash = null;
        }

        private void Init()
        {
            int i;

            count = 0;
            minBucket = 0;
            hash = new HalfEdge[hashSize];
            // dummy Halfedge at the top of each hash
            for (i = 0; i < hashSize; ++i)
            {
                hash[i] = HalfEdge.CreateDummy();
                hash[i].NextInPriorityQueue = null;
            }
        }

        public void Insert(HalfEdge halfEdge)
        {
            HalfEdge next;
            var insertionBucket = Bucket(halfEdge);

            if (insertionBucket < minBucket)
            {
                minBucket = insertionBucket;
            }

            var previous = hash[insertionBucket];

            while ((next = previous.NextInPriorityQueue) != null
            && (halfEdge.YStar > next.YStar || (halfEdge.YStar == next.YStar && halfEdge.Vertex.X > next.Vertex.X)))
            {
                previous = next;
            }

            halfEdge.NextInPriorityQueue = previous.NextInPriorityQueue;
            previous.NextInPriorityQueue = halfEdge;
            count++;
        }

        public void Remove(HalfEdge halfEdge)
        {
            HalfEdge previous;
            var removalBucket = Bucket(halfEdge);

            if (halfEdge.Vertex == null) return;

            previous = hash[removalBucket];

            while (previous.NextInPriorityQueue != halfEdge)
            {
                previous = previous.NextInPriorityQueue;
            }

            previous.NextInPriorityQueue = halfEdge.NextInPriorityQueue;
            count--;
            halfEdge.Vertex = null;
            halfEdge.NextInPriorityQueue = null;
            halfEdge.Dispose();
        }

        private int Bucket(HalfEdge halfEdge)
        {
            var theBucket = (int)((halfEdge.YStar - yMin) / deltaY * hashSize);
            if (theBucket < 0)
                theBucket = 0;
            if (theBucket >= hashSize)
                theBucket = hashSize - 1;
            return theBucket;
        }

        private bool IsEmpy(int bucket)
        {
            return (hash[bucket].NextInPriorityQueue == null);
        }

        private void AdjustMinBucket()
        {
            while (minBucket < hashSize - 1 && IsEmpy(minBucket))
            {
                minBucket++;
            }
        }

        public bool Empty()
        {
            return count == 0;
        }

        public Vector2 Min()
        {
            AdjustMinBucket();
            var min = hash[minBucket].NextInPriorityQueue;
            return new Vector2(min.Vertex.X, min.YStar);
        }

        public HalfEdge ExtractMin()
        {
            // get the first real Halfedge in _minBucket
            var answer = hash[minBucket].NextInPriorityQueue;

            hash[minBucket].NextInPriorityQueue = answer.NextInPriorityQueue;
            count--;
            answer.NextInPriorityQueue = null;

            return answer;
        }
    } 
}
