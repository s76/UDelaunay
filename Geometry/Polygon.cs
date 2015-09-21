using UnityEngine;
using System.Collections.Generic;

namespace UDelaunay
{
    public class Polygon
    {
        private readonly List<Vector2> vertices;

        public Polygon(List<Vector2> vertices)
        {
            this.vertices = vertices;
        }

        public float Area()
        {
            return Mathf.Abs(SignedDoubleArea() * 0.5f);
        }

        public Winding PolyWinding()
        {
            var signedDoubleArea = SignedDoubleArea();

            return signedDoubleArea < 0 ? Winding.Clockwise : signedDoubleArea > 0 ? Winding.Counterclockwise : Winding.None;
        }

        private float SignedDoubleArea()
        {
            var n = vertices.Count;
            var signedDoubleArea = 0f;

            for (var index = 0; index < n; index++)
            {
                var nextIndex = (index + 1) % n;
                var point = vertices[index];
                var next = vertices[nextIndex];
                signedDoubleArea += point.x * next.y - next.x * point.y;
            }

            return signedDoubleArea;
        }

    } 
}
