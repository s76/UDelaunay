# UDelaunay

A delaunay triangulation implementation for unity, ported from:
as3delaunay: https://github.com/nodename/as3delaunay

Using fortunes algorithm provides triangulation, voronoi, convex hull and minimum spanning tree. Lloyds relaxation algorithm is also implemented.

## Usage

Simply drop into your project, there is currently no demo included, but it is simple enough to use, create a new Voronoi object by providing a list of points as Vector2 and a Rect to represent the bounds. The voronoi object contains helpers to retrieve diagrams as lists of type LineSegment. The voronoi diagram, delaunay triangulation etc can be be inferered from the segments.

## License

(from as3delaunay)

MIT License

Copyright (c) 2009 Alan Shaw 

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
