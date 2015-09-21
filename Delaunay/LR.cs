namespace UDelaunay
{
    public enum Side
    {
        Left,
        Right
    }

    public class SideHelper
    {
        public static Side Other(Side leftRight)
        {
            return leftRight == Side.Left ? Side.Right : Side.Left;
        }
    }
}
