using UnityEngine;

namespace UDelaunay
{
    public class Circle
    {
        public Vector2 Center;
        public float Radius;

        public Circle(float centerX, float centerY, float radius)
        {
            Center = new Vector2(centerX, centerY);
            Radius = radius;
        }

        public override string ToString()
        {
            return "Circle (center: " + Center + ", radius: " + Radius + ")";
        }
    }
}
