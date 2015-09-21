using System.Collections.Generic;

//CURRENTLY UNUSED, WOULD BE A GOOD IDEA FOR MESH GENERATION TO IMPLEMENT
//A FAST ALGORITHM THAT CREATES A REPRESENTATION OF THE DT USING THESE
namespace UDelaunay
{
    public class Triangle
    {

        public List<Site> Sites { get; private set; } 

        public Triangle(Site a, Site b, Site c)
        {
            Sites = new List<Site>() { a, b, c };
        }

        public void Dispose()
        {
            Sites.Clear();
            Sites = null;
        }
    } 
}
